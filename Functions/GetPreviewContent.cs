using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Vendanor.Preview.Common;
using Vendanor.Preview.Extensions;

namespace Vendanor.Preview.Functions
{
    public static class GetPreviewContent
    {
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// Main reverse proxy function
        /// HttpContext / requesting url here is vn-preview.azurewebsites.net (azure function internal url)
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetPreviewContent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogDebug("🎈🎈🎈 GetPreviewContent 🎈🎈🎈");

            // Get disguised host, example: vnmfa-278-b2e7537.preview.domain.com/
            var disguisedHost = req.Headers["DISGUISED-HOST"].ToString();
            var host = !string.IsNullOrEmpty(disguisedHost) ? disguisedHost : req.Host.Host;

            var settings = EnvSettings.GetSettings();

            // app-274-ff03
            var subdomain = host.Replace(settings.PreviewBaseUrl, "").TrimEnd('.');
            var hasSubdomain = subdomain.Length > 0;
            var rx = new Regex(@"\w+-{1}\d+-{1}\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var isMatch = rx.IsMatch(subdomain);
            var isSubdomainValid = hasSubdomain && isMatch;

            if (!hasSubdomain)
            {
                return new OkObjectResult(new
                {
                    Error = "Preview not found, please retry preview url",
                    Host = host,
                    subdomain,
                    DisguisedHost = disguisedHost
                });
            }

            if (!isSubdomainValid)
            {
                return new OkObjectResult(new
                {
                    Error = $"Preview not found, {subdomain} is not a valid preview url",
                });
            }

            // NOTE: this needs to be passed by azure functions proxy:
            var restOfPath = req.Query["restOfPath"];

            var previewUrl = subdomain.Replace("-", "/");
            var targetUrl = Url.Combine(settings.StaticAssetsBaseUrl, previewUrl, restOfPath);

            log.LogDebug("restOfPath: " + restOfPath);
            log.LogDebug("Target url: " + targetUrl);

            var uri = new Uri(targetUrl);

            var request = req.HttpContext.CreateProxyHttpRequest(uri);

            // response from azure static storage:
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, req.HttpContext
            .RequestAborted).ConfigureAwait(false);

            log.LogInformation("response from azure static: " + response.StatusCode);

            // Handle deep links.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                log.LogDebug($"==> Url {targetUrl} not found behind proxy, absolutePath: {uri.AbsolutePath}");

                var lastSegment = uri.Segments[uri.Segments.Length - 1];
                var lastSegmentHasFileExtension = lastSegment.Contains(".");

                if (!lastSegmentHasFileExtension)
                {
                    log.LogDebug("==> We have a DEEP LINK, let's return index.html and let static app fix routing");
                    var indexUrl = Url.Combine(settings.StaticAssetsBaseUrl, previewUrl, "index.html");
                    log.LogDebug("combined index url:" + indexUrl);
                    var indexRequest = req.HttpContext.CreateProxyHttpRequest(new Uri(indexUrl));
                    var indexResponse = await Client.SendAsync(indexRequest, HttpCompletionOption
                        .ResponseHeadersRead, req.HttpContext.RequestAborted).ConfigureAwait(false);
                    log.LogDebug("response from index request: " + indexResponse.StatusCode);
                    await req.HttpContext.CopyProxyHttpResponse(indexResponse).ConfigureAwait(false);
                    return new OkResult();
                }

                if (lastSegment.Equals("index.html", StringComparison.InvariantCultureIgnoreCase))
                {
                    log.LogWarning($"index.html not found for preview {previewUrl}. Preview is probably deleted");
                    return new OkObjectResult(new
                    {
                        Message = "Preview not found.",
                        Description = "Please re-open and rebuild PR to generate a new preview. If the PR was closed, all previews are deleted."
                    });
                }

                log.LogDebug($"==> {uri.AbsoluteUri} not found, returning 404");
            } else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new BadRequestObjectResult(new
                {
                    Error = "Bad request",
                    Status = response.StatusCode,
                    Reason = response.ReasonPhrase,
                    Response = responseContent
                });
            }

            // copy response into req.HttpContext.Response
            await req.HttpContext.CopyProxyHttpResponse(response).ConfigureAwait(false);

            log.LogDebug("🎈🎈🎈 Great success - very nice 🎈🎈🎈");

            return new OkResult();
        }
    }
}
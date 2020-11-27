using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Vendanor.Preview.DeepLinkDetection;
using Vendanor.Preview.Extensions;
using Vendanor.Preview.Settings;

namespace Vendanor.Preview.Functions
{
    public static class GetPreviewContent
    {
        private static readonly HttpClient Client = new HttpClient();

        private static Dictionary<string, AppInstanceSettings> appInstanceSettings;

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

            var azureSettings = AzureSettingsFactory.GetSettings();

            // Get disguised host, example: vnmfa-278-b2e7537.preview.domain.com/
            var disguisedHost = req.Headers["DISGUISED-HOST"].ToString();
            var host = !string.IsNullOrEmpty(disguisedHost) ? disguisedHost : req.Host.Host;


            var subdomain = host.Replace(azureSettings.PreviewBaseUrl!, "").TrimEnd('.');
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
            var targetUrl = Url.Combine(azureSettings.StaticAssetsBaseUrl, previewUrl, restOfPath);

            log.LogDebug("restOfPath: " + restOfPath);
            log.LogDebug("Target url: " + targetUrl);

            var uri = new Uri(targetUrl);

            var request = req.HttpContext.CreateProxyHttpRequest(uri);

            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, req.HttpContext
            .RequestAborted).ConfigureAwait(false);
            log.LogDebug("response from azure static: " + response.StatusCode);




            // Handle deep links.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                log.LogDebug($"==> Url {targetUrl} not found behind proxy, absolutePath: {uri.AbsolutePath}");

                // We either have a real 404 to a static file or a deep link to a route.
                var lastSegment = uri.Segments[uri.Segments.Length - 1];
                IDeepLinkDetectionStrategy strategy;

                try
                {
                    strategy = await DeepLinkDetectionStrategyFactory.CreateStrategy(azureSettings, previewUrl, log);

                }
                catch (LocalSettingsMissingException err)
                {
                    return new OkObjectResult(new
                    {
                        Message = "Could not create deep link detection strategy, missing local settings",
                        Description = err.Message
                    });
                }

                log.LogDebug("Check if isDeepLink: " + restOfPath);
                var isDeepLink = strategy.GetIsDeepLink(restOfPath);
                log.LogDebug($"Using deep link detection strategy: {strategy.Name} Url/restOfPath:{restOfPath} IsDeep:{isDeepLink.ToString()} ");

                if (isDeepLink)
                {
                    log.LogDebug("==> Deep link detected, returning index.html and let static app handle routing");
                    var indexUrl = Url.Combine(azureSettings.StaticAssetsBaseUrl, previewUrl, "index.html");
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

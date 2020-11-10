using System;
using System.Linq;
using System.Text.RegularExpressions;
using Flurl;

namespace Vendanor.Preview.Common
{
    /// <summary>
    /// Testing some alternative strategies for resolving deep links vs. actual 404s:
    /// 1. check for dot in last segment
    /// 2. less stupid dot-check. (check known mime types, length, etc.) less stupid, but still stupid
    /// 3. always send to SPA (soft 404)
    /// 4. always send to SPA (soft 404, except for assets)
    /// 5. whitelist SPA routes (routes.json)
    ///
    ///
    /// dist => subfolder? assets/ and
    ///
    /// </summary>
    public interface IDeepLinkDetectionStrategy
    {
        bool GetIsDeepLink(string url);
    }

    public class DotStrategy : IDeepLinkDetectionStrategy
    {
        public bool GetIsDeepLink(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var lastSegment = uri.Segments[uri.Segments.Length - 1];
            var lastSegmentHasFileExtension = lastSegment.Contains(".");
            var lastSegmentEndsWithSlash = lastSegment.EndsWith("/");

            if (lastSegmentEndsWithSlash)
            {
                return true;
            }

            return !lastSegmentHasFileExtension;
        }
    }

    public class SoftNotFoundStrategy : IDeepLinkDetectionStrategy
    {
        private readonly string[] _assetFolderRegex;

        public SoftNotFoundStrategy(string[] assetFolderRegex)
        {
            _assetFolderRegex = assetFolderRegex;
        }

        public bool GetIsDeepLink(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var url = new Url(uri);
            var temp = uri.PathAndQuery;

            foreach (var expression in _assetFolderRegex)
            {
                var regex = new Regex(expression);
                if (regex.IsMatch(temp))
                {
                    // if matched to a static assets folder return false (results in 404)
                    return false;
                }
            }

            return true;
        }
    }

    public class WhitelistRoutesStrategy : IDeepLinkDetectionStrategy
    {
        public bool GetIsDeepLink(string url)
        {
            // TODO: return true if url is matched to routes.json or similar
            // Concern: maintenance. Needs to keep routes.json up-to-date
            // Possible solution? => extract route info from angular router using a angular module?
            //                       use same format as azure static web apps, can perhaps be reused?
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vendanor.Preview.DeepLinkDetection
{
    public  class DotStrategy : IDeepLinkDetectionStrategy
    {
        public string Name => "DotStrategy";

        public bool GetIsDeepLink(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var pathAndQuery = uri.PathAndQuery;
            var lastSegment = uri.Segments[uri.Segments.Length - 1];
            var lastSegmentEndsWithSlash = lastSegment.EndsWith("/");
            var lastSegmentEndsWithIndexHtml = lastSegment.EndsWith("index.html");
            var isOnRoot = pathAndQuery == "/";

            if (lastSegmentEndsWithSlash || isOnRoot || lastSegmentEndsWithIndexHtml)
            {
                return true;
            }

            return !UrlHelpers.IsUrlProbablyFile(inputUrl);
        }
    }

    public class SoftStrategy: IDeepLinkDetectionStrategy
    {
        public string Name => "SoftStrategy";

        public bool GetIsDeepLink(string url)
        {
            return true;
        }
    }

    public class SoftAssetWhitelistStrategy : IDeepLinkDetectionStrategy
    {
        private readonly string[] _assetFolderRegex;

        public SoftAssetWhitelistStrategy(string[] assetFolderRegex)
        {
            _assetFolderRegex = assetFolderRegex;
        }

        public string Name => "SoftAssetWhitelistStrategy";

        public bool GetIsDeepLink(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var pathAndQuery = uri.PathAndQuery;
            var lastSegment = uri.Segments[uri.Segments.Length - 1];
            var lastSegmentEndsWithSlash = lastSegment.EndsWith("/");
            var lastSegmentEndsWithIndexHtml = lastSegment.EndsWith("index.html");
            var isOnRoot = pathAndQuery == "/";

            if (lastSegmentEndsWithSlash || lastSegmentEndsWithIndexHtml || isOnRoot)
            {
                return true;
            }

            foreach (var expression in _assetFolderRegex)
            {
                var regex = new Regex(expression);
                if (regex.IsMatch(pathAndQuery))
                {
                    // if matched to a static assets folder return false (results in 404)
                    return false;
                }
            }

            return true; // soft
        }
    }

    /// <summary>
    /// Quick implementation, return true if url matches whitelist of known SPA routes in routes.json
    /// This works in our test cases, but maintenance of routes is a concern
    ///
    /// Possible solution? => lib to extract route info from angular router? same for react etc?
    /// use same format as azure static web apps, can perhaps be reused?
    /// </summary>
    public class RouteWhitelistStrategy : IDeepLinkDetectionStrategy
    {
        private readonly List<string> _whitelist;
        public RouteWhitelistStrategy(List<string> whitelist)
        {
            _whitelist = whitelist;
        }
        public bool GetIsDeepLink(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var pathAndQuery = uri.PathAndQuery;

            if (pathAndQuery.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (pathAndQuery.Equals("/index.html", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            // just checking string.contains for now, should implement some sort of route matching, stars etc.
            foreach (var c in _whitelist)
            {
                if (pathAndQuery.ToLower().Contains(c.ToLower()))
                {
                    // everything that matches valid SPA routes results in soft 404 (200)
                    // if we have files in under valid routes, these will result in soft 404 too, but I don't think this is the case?
                    return true;
                }
            };

            return false; // hard 404
        }

        public string Name => "RouteWhitelistStrategy";
    }
}
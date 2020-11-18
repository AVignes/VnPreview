using System;

namespace Vendanor.Preview.DeepLinkDetection
{
    public static class UrlHelpers
    {
        /// <summary>
        /// It's not possible to detect if url is a static file or not.
        /// Example false positive: https://app.com/refund/customer/email@isvalidinurl.com
        /// </summary>
        /// <param name="inputUrl"></param>
        /// <returns></returns>
        public static bool IsUrlProbablyFile(string inputUrl)
        {
            var uri = new Uri(inputUrl);
            var lastSegment = uri.Segments[uri.Segments.Length - 1];
            var lastSegmentHasFileExtension = lastSegment.Contains(".");
            var lastSegmentEndsWithSlash = lastSegment.EndsWith("/");

            if (lastSegmentEndsWithSlash)
            {
                return false;
            }

            if (lastSegmentHasFileExtension)
            {
                var parts = lastSegment.Split(".");
                var lastPart = parts[parts.Length - 1];
                if (lastPart.Length > 1 && lastPart.Length <= 4)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
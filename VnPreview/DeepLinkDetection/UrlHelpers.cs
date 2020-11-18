using System;

namespace Vendanor.Preview.DeepLinkDetection
{
    public static class UrlHelpers
    {
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

            // false positive:             Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/refund/customer/email@isvalidinurl.com"));


            // 4 characters dot detection strategy.. works for now?
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
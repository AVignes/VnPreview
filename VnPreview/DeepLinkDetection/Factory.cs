using System;
using Vendanor.Preview.Common;

namespace Vendanor.Preview.DeepLinkDetection
{
    public static class Factory
    {
        public static IDeepLinkDetectionStrategy CreateStrategy(Settings settings)
        {
            if (settings.DeepLinkDetectionStrategy != null)
            {
                if (settings.DeepLinkDetectionStrategy.Equals("soft", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new SoftStrategy();
                }
                if (settings.DeepLinkDetectionStrategy.Equals("assets", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new SoftAssetWhitelistStrategy(settings.AssetFolders);
                }
                if (settings.DeepLinkDetectionStrategy.Equals("route", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new RouteWhitelistStrategy(settings.Routes);
                }
            }

            return new DotStrategy();
        }
    }
}
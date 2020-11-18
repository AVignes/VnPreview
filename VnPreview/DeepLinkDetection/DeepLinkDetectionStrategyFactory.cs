using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vendanor.Preview.Settings;

namespace Vendanor.Preview.DeepLinkDetection
{
    public static class DeepLinkDetectionStrategyFactory
    {
	    private static readonly HttpClient Client = new HttpClient();

	    public static async Task<IDeepLinkDetectionStrategy> CreateStrategy(AzureSettings azureSettings, string previewUrl, ILogger log)
        {
            if (azureSettings.DeepLinkDetectionStrategy != null)
            {
                if (azureSettings.DeepLinkDetectionStrategy.Equals("soft", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new SoftStrategy();
                }

                var isAssets =
	                azureSettings.DeepLinkDetectionStrategy.Equals("assets",
		                StringComparison.InvariantCultureIgnoreCase);
                var isRoutes =
	                azureSettings.DeepLinkDetectionStrategy.Equals("routes",
		                StringComparison.InvariantCultureIgnoreCase);

                if (isAssets || isRoutes)
                {
	                var appInstanceSettings = await AppInstanceSettingsFactory.GetSettingsAsync(azureSettings, previewUrl, log);

	                if (isAssets)
	                {
	                    return new SoftAssetWhitelistStrategy(appInstanceSettings.AssetFolders ?? new string[0]);
	                }
	                return new RouteWhitelistStrategy((appInstanceSettings.Routes ?? new string[0]).ToList());
                }

            }

            return new DotStrategy();
        }
    }
}

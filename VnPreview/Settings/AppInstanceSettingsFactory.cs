using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Vendanor.Preview.Settings
{
	public static class AppInstanceSettingsFactory
	{
		private static readonly HttpClient Client = new HttpClient();

		private static readonly ConcurrentDictionary<string, AppInstanceSettings> Cache = new ConcurrentDictionary<string, AppInstanceSettings>();

		public static async Task<AppInstanceSettings> GetSettingsAsync(AzureSettings azureSettings, string previewUrl, ILogger log)
		{
			if (Cache.TryGetValue(previewUrl, out var temp))
			{
				log.LogDebug("Returning AppInstanceSettings from cache: " + previewUrl);
				return temp;
			}

			var targetUrl = Url.Combine(azureSettings.StaticAssetsBaseUrl, previewUrl,"assets", "vnpreview.json");
			log.LogDebug("Url getAppSettings: " + targetUrl);

			var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
			var response = await Client.SendAsync(request).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				throw new LocalSettingsMissingException($"Could not find required {targetUrl}");
			}

			var raw = await response.Content.ReadAsStringAsync();
			var appInstanceSettings = await Task.Run(() => JsonConvert.DeserializeObject<AppInstanceSettings>(raw));
			var greatSuccess = Cache.TryAdd(previewUrl, appInstanceSettings);
			log.LogDebug($"Added to cache: {greatSuccess.ToString()}");

			return appInstanceSettings;
		}
	}

	public class LocalSettingsMissingException : Exception
	{
		public LocalSettingsMissingException(string message) : base(message)
		{
		}
	}
}

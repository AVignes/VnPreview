using System;

namespace Vendanor.Preview.Settings
{
    public static class AzureSettingsFactory
    {
        public static AzureSettings GetSettings()
        {
            var settings = new AzureSettings()
            {
                PreviewBaseUrl =
                    System.Environment.GetEnvironmentVariable("PREVIEW_BASE_URL", EnvironmentVariableTarget.Process),
                StaticAssetsBaseUrl =
                    System.Environment.GetEnvironmentVariable("STATIC_BASE_URL", EnvironmentVariableTarget.Process),
                StreamCopyBufferSize =
                    System.Environment.GetEnvironmentVariable("STREAM_COPY_BUFFER_SIZE", EnvironmentVariableTarget.Process),
                DeepLinkDetectionStrategy =
                    System.Environment.GetEnvironmentVariable("DEEP_LINK_DETECTION_STRATEGY", EnvironmentVariableTarget.Process),
            };

            if (settings.PreviewBaseUrl == null)
            {
                throw new Exception("Missing setting PREVIEW_BASE_URL");
            }
            if (settings.StaticAssetsBaseUrl == null)
            {
                throw new Exception("Missing setting STATIC_BASE_URL");
            }

            return settings;
        }
    }
}

using System;

namespace Vendanor.Preview.Common
{
    public class Settings
    {
        public string StaticAssetsBaseUrl { get; set; }
        public string PreviewBaseUrl { get; set; }
        public string StreamCopyBufferSize { get; set; }
    }

    public static class EnvSettings
    {
        public static Settings GetSettings()
        {
            return new Settings()
            {
                PreviewBaseUrl =
                    System.Environment.GetEnvironmentVariable("PREVIEW_BASE_URL", EnvironmentVariableTarget.Process),
                StaticAssetsBaseUrl =
                    System.Environment.GetEnvironmentVariable("STATIC_BASE_URL", EnvironmentVariableTarget.Process),
                StreamCopyBufferSize =
                    System.Environment.GetEnvironmentVariable("STREAM_COPY_BUFFER_SIZE", EnvironmentVariableTarget.Process)
            };
        }
    }
}
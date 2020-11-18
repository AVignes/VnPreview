using System;
using System.Collections.Generic;
using System.Linq;


namespace Vendanor.Preview.Common
{
    #nullable enable

    public class Settings
    {
        public string? StaticAssetsBaseUrl { get; set; }
        public string? PreviewBaseUrl { get; set; }
        public string? StreamCopyBufferSize { get; set; }

        public string? DeepLinkDetectionStrategy { get; set; }
        public string[]? AssetFolders { get; set; }
        public List<string>? Routes { get; set; }
    }

    public static class EnvSettings
    {
        public static Settings GetSettings()
        {
            var settings = new Settings()
            {
                PreviewBaseUrl =
                    System.Environment.GetEnvironmentVariable("PREVIEW_BASE_URL", EnvironmentVariableTarget.Process),
                StaticAssetsBaseUrl =
                    System.Environment.GetEnvironmentVariable("STATIC_BASE_URL", EnvironmentVariableTarget.Process),
                StreamCopyBufferSize =
                    System.Environment.GetEnvironmentVariable("STREAM_COPY_BUFFER_SIZE", EnvironmentVariableTarget.Process),
                DeepLinkDetectionStrategy =
                    System.Environment.GetEnvironmentVariable("DEEP_LINK_DETECTION_STRATEGY", EnvironmentVariableTarget.Process),
                AssetFolders =
                    (Environment.GetEnvironmentVariable("ASSET_FOLDERS", EnvironmentVariableTarget.Process) ?? "")
                .Split(","),
                Routes = (Environment.GetEnvironmentVariable("ROUTES", EnvironmentVariableTarget.Process) ?? "").Split(",").ToList()
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
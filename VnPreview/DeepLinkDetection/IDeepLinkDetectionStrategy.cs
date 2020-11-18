namespace Vendanor.Preview.DeepLinkDetection
{
    /// <summary>
    /// Testing some alternative strategies for resolving deep links vs. actual 404s:
    /// 1. check for dot in last segment
    /// 2. less stupid dot-check, check file extension length => not optimal either
    /// 3. always send to SPA (soft 404) => not optimal, we want actual 404s for missing assets
    /// 4. soft 404, whitelist asset folders => should work, in our case at least? but assets on root will be soft 404s
    /// 5. soft 404, whitelist SPA routes (routes.json) => same approach as Azure, but concern about maintenance
    /// </summary>
    public interface IDeepLinkDetectionStrategy
    {
        bool GetIsDeepLink(string pathAndQuery);
        string Name { get; }
    }
}

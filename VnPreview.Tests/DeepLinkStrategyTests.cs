using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Vendanor.Preview.DeepLinkDetection;

namespace VnPreview.Tests
{
    /// <summary>
    /// Testing various strategies for detecting deep links
    /// </summary>
    public class DeepLinkStrategyTests
    {
        private IDeepLinkDetectionStrategy _strategy;

        [SetUp]
        public void Setup()
        {
            // fails on https://app.com/refund/customer/email@isvalidinurl.com, returns 404, should be deep link
            // _strategy = new DotStrategy();

            // works pretty well, but fails on https://app.com/font.woff, soft 404 (200) for static resources on root
            // also some maintenance required keeping asset folders in sync?
            // _strategy = new SoftAssetWhitelistStrategy(new []{@"^(/assets)\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)"});

            // this returns soft 404 (200) for all static resources, no actual 404s with this
            // _strategy = new SoftStrategy();

            // this works on all tests. But requires maintenance + settings per preview / app..
            var routes = new List<string> {"/subroute", "refund", "/refund/customer"};
            _strategy = new RouteWhitelistStrategy(routes);
        }

        [Test]
        public void TestRegexStuff()
        {
            var temp = @"^(/assets)\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
            var reg = new Regex(temp);
            Assert.IsTrue(reg.IsMatch("/assets/temp.json"));
            Assert.IsTrue(reg.IsMatch("/assets/"));
            Assert.IsTrue(reg.IsMatch("/assets"));
            Assert.IsTrue(reg.IsMatch("/assets/sub/fol/der/temp.json"));
            Assert.IsTrue(reg.IsMatch("/assets/sub/fol/der/valid.url@somethin.gng"));
        }

        [Test]
        public void TestRegexRootAssets()
        {
            // example url: https://app.com/font.woff
            var temp = @"^(/assets)\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";
            var reg = new Regex(temp);
            Assert.IsTrue(reg.IsMatch("/assets/temp.json"));
        }

        [Test]
        public void TestValidRoute()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/subroute"));
        }
        [Test]
        public void TestValidRouteWithSlash()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/subroute/"));
        }

        [Test]
        public void TestValidRootRoute()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com"));
        }

        [Test]
        public void TestValidRootRouteWithSlash()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/"));
        }

        [Test]
        public void TestValidRootRouteWithIndexHtml()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/index.html"));
        }

        [Test]
        public void TestAssetFolder()
        {
            Assert.IsFalse(_strategy.GetIsDeepLink("https://app.com/assets/image.png"));
            Assert.IsFalse(_strategy.GetIsDeepLink("https://app.com/assets/subfolder/image.png"));
        }

        [Test]
        public void TestAssetsInRoot()
        {
            Assert.IsFalse(_strategy.GetIsDeepLink("https://app.com/favicon.ico"));
            Assert.IsFalse(_strategy.GetIsDeepLink("https://app.com/font.woff"));
        }

        [Test]
        public void TestValidDotsInLastSegment()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/refund/customer/email@isvalidinurl.com&secret=28347234823764827"));
        }

        [Test]
        public void TestValidDotsInLastSegmentCouldBeFileOrRoute()
        {
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/refund/customer/email@isvalidinurl.com"));
        }
    }
}
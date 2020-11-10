using System.Text.RegularExpressions;
using NUnit.Framework;
using Vendanor.Preview.Common;

namespace VnPreview.Tests
{
    /// <summary>
    /// Testing various cases for resolving deep links
    /// </summary>
    public class DeepLinkStrategyTests
    {
        private IDeepLinkDetectionStrategy _strategy;

        [SetUp]
        public void Setup()
        {
            // Our current solution fails on valid dot in last url segment:
            // _strategy = new DotStrategy();

            // works with valid urls with dot in last segment
            // works with assets/ folder whitelisting
            // but doesn't catch static resources at root, example: https://app.com/font.woff
            _strategy = new SoftNotFoundStrategy(new []{@"^(/assets)\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)"});

            // Example 404:         https://app.com/font.woff         <== soft 404 (200 ok)
            // Example valid:       https://app.com/routewith.dot
            // Example valid:       https://app.com/my@email.com

            // => known file extensions? => no..

            // TODO: check what happens when we return 200 and index.html for route  https://app.com/font.woff ?
            // assume font is missing on server. wrong mime type in result.

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
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/subroute/"));
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
            Assert.IsTrue(_strategy.GetIsDeepLink("https://app.com/refund/customer/email@isvalidinurl.com"));
        }

    }
}
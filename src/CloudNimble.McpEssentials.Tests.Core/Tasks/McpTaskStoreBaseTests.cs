using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Claims;
using System.Security.Principal;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks
{

    /// <summary>
    /// Coverage for claim lookup, fail-closed identity, and owner comparison on the store base.
    /// </summary>
    [TestClass]
    public class McpTaskStoreBaseTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void RequireCurrent_ReturnsUserId()
        {
            var inspect = new InspectableStore("alice");

            inspect.RequireCurrent().Should().Be("alice");
        }

        [TestMethod]
        public void RequireCurrent_WhenMissing_Throws()
        {
            var inspect = new InspectableStore((string?)null);

            Action act = () => inspect.RequireCurrent();

            act.Should().Throw<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public void RequireCurrent_WhenWhitespace_Throws()
        {
            var inspect = new InspectableStore("   ");

            Action act = () => inspect.RequireCurrent();

            act.Should().Throw<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public void RequirePrincipal_ReturnsUserId()
        {
            var inspect = new InspectableStore((string?)null);

            inspect.RequirePrincipal(TestUser.Authenticated("alice")).Should().Be("alice");
        }

        [TestMethod]
        public void RequirePrincipal_WhenUnauthenticated_Throws()
        {
            var inspect = new InspectableStore("alice");

            Action act = () => inspect.RequirePrincipal(TestUser.Unauthenticated());

            act.Should().Throw<McpTaskUserRequiredException>();
        }

        [TestMethod]
        public void RequirePrincipal_WhenNull_Throws()
        {
            var inspect = new InspectableStore("alice");

            Action act = () => inspect.RequirePrincipal(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("user");
        }

        [TestMethod]
        public void IsOwnedBy_UsesOrdinalComparison()
        {
            var inspect = new InspectableStore("alice");

            inspect.OwnedBy("User", "User").Should().BeTrue();
            inspect.OwnedBy("User", "user").Should().BeFalse();
            inspect.OwnedBy("alice", "bob").Should().BeFalse();
            inspect.OwnedBy(null, "alice").Should().BeFalse();
            inspect.OwnedBy("alice", null).Should().BeFalse();
            inspect.OwnedBy(" ", " ").Should().BeFalse();
        }

        [TestMethod]
        public void GetUserId_WhenPrincipalIsNull_ReturnsNull()
        {
            McpTaskStoreBase.GetUserId(null).Should().BeNull();
        }

        [TestMethod]
        public void GetUserId_WhenUnauthenticated_ReturnsNull()
        {
            McpTaskStoreBase.GetUserId(TestUser.Unauthenticated()).Should().BeNull();
        }

        [TestMethod]
        public void GetUserId_WhenAuthenticatedWithoutClaimsOrName_ReturnsNull()
        {
            var identity = new ClaimsIdentity(authenticationType: "Test");

            McpTaskStoreBase.GetUserId(new ClaimsPrincipal(identity)).Should().BeNull();
        }

        [TestMethod]
        public void GetUserId_PrefersNameIdentifier()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "id-1"),
                    new Claim("sub", "sub-1"),
                    new Claim(ClaimTypes.Upn, "upn@example.com"),
                    new Claim(ClaimTypes.Name, "display")
                ],
                authenticationType: "Test");

            McpTaskStoreBase.GetUserId(new ClaimsPrincipal(identity)).Should().Be("id-1");
        }

        [TestMethod]
        public void GetUserId_FallsBackToSub()
        {
            McpTaskStoreBase.GetUserId(TestUser.Authenticated("oid-sub", "sub")).Should().Be("oid-sub");
        }

        [TestMethod]
        public void GetUserId_FallsBackToUpn()
        {
            McpTaskStoreBase.GetUserId(TestUser.Authenticated("user@tenant.com", ClaimTypes.Upn)).Should().Be("user@tenant.com");
        }

        [TestMethod]
        public void GetUserId_FallsBackToName()
        {
            McpTaskStoreBase.GetUserId(TestUser.Authenticated("display", ClaimTypes.Name)).Should().Be("display");
        }

        [TestMethod]
        public void GetUserId_FallsBackToIdentityName()
        {
            var identity = new GenericIdentity("legacy-name");
            var principal = new ClaimsPrincipal(identity);

            McpTaskStoreBase.GetUserId(principal).Should().Be("legacy-name");
        }

        [TestMethod]
        public void GetUserId_DuplicateFallbacks_KeepFirst()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", "first-sub"),
                    new Claim("sub", "second-sub"),
                    new Claim(ClaimTypes.Upn, "first@ex.com"),
                    new Claim(ClaimTypes.Upn, "second@ex.com"),
                    new Claim(ClaimTypes.Name, "first-name"),
                    new Claim(ClaimTypes.Name, "second-name")
                ],
                authenticationType: "Test");

            McpTaskStoreBase.GetUserId(new ClaimsPrincipal(identity)).Should().Be("first-sub");
        }

        [TestMethod]
        public void GetUserId_SkipsWhitespaceNameIdentifier()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "  "),
                    new Claim("sub", "sub-1")
                ],
                authenticationType: "Test");

            McpTaskStoreBase.GetUserId(new ClaimsPrincipal(identity)).Should().Be("sub-1");
        }

        [TestMethod]
        public void GetUserId_SkipsWhitespaceFallbacksUntilName()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", "  "),
                    new Claim(ClaimTypes.Upn, " "),
                    new Claim(ClaimTypes.Name, "display")
                ],
                authenticationType: "Test");

            McpTaskStoreBase.GetUserId(new ClaimsPrincipal(identity)).Should().Be("display");
        }

        #endregion

    }

}

using System.Security.Claims;

namespace CloudNimble.McpEssentials.Tests.Cosmos.Tasks
{

    /// <summary>
    /// Builds authenticated principals and a switchable user id for store tests.
    /// </summary>
    internal class TestUser
    {

        #region Properties

        public string? UserId { get; set; }

        #endregion

        #region Constructors

        public TestUser(string? userId)
        {
            UserId = userId;
        }

        #endregion

        #region Public Methods

        public static ClaimsPrincipal Authenticated(string userId, string claimType = ClaimTypes.NameIdentifier)
        {
            var identity = new ClaimsIdentity(
                [new Claim(claimType, userId)],
                authenticationType: "Test");
            return new ClaimsPrincipal(identity);
        }

        public static ClaimsPrincipal Unauthenticated()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        #endregion

    }

}

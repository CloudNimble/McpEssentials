using System.Security.Claims;

namespace CloudNimble.McpEssentials.Tests.PostgreSQL.Tasks
{

    /// <summary>
    /// Mutable current-user holder plus a factory for authenticated principals.
    /// </summary>
    internal class TestUser
    {

        #region Properties

        /// <summary>
        /// Gets or sets the user id the store callback returns.
        /// </summary>
        public string? UserId { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestUser"/> class.
        /// </summary>
        /// <param name="userId">The starting user id.</param>
        public TestUser(string? userId)
        {
            UserId = userId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns an authenticated principal whose NameIdentifier is <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>An authenticated principal.</returns>
        public static ClaimsPrincipal Authenticated(string userId)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                authenticationType: "test");
            return new ClaimsPrincipal(identity);
        }

        #endregion

    }

}

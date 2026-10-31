using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CloudNimble.McpEssentials.Samples.EntityFrameworkCore
{

    /// <summary>
    /// Stamps <see cref="HttpContext.User"/> from the <c>X-User-Id</c> header so the sample
    /// can demonstrate per-user task isolation without a real identity provider.
    /// </summary>
    public class FakeAuthenticationMiddleware
    {

        #region Fields

        /// <summary>
        /// The request header that carries the fake user identifier.
        /// </summary>
        public const string HeaderName = "X-User-Id";

        private readonly RequestDelegate _next;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeAuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public FakeAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads <c>X-User-Id</c> and assigns an authenticated principal when the header is present.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task that completes when the remainder of the pipeline has finished.</returns>
        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var values))
            {
                var userId = values.ToString();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var identity = new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.Trim())],
                        authenticationType: "Fake");
                    context.User = new ClaimsPrincipal(identity);
                }
            }

            return _next(context);
        }

        #endregion

    }

}

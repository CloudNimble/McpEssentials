using System.Net.Http;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.McpEssentials.Samples.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Breakdance TestServer that hosts the sample Streamable HTTP MCP pipeline:
    /// fake <c>X-User-Id</c> auth, <c>AddHttpContextAccessor</c>, and
    /// <c>InMemoryMcpTaskStorePlus</c>.
    /// </summary>
    public abstract class AuthenticatedMcpHost : AspNetCoreBreakdanceTestBase
    {

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            TestHostBuilder.ConfigureServices((_, services) =>
            {
                services.AddHttpContextAccessor();
                services.AddMcpServer()
                    .WithHttpTransport(options => options.Stateless = true)
                    .WithTools<HelloWorldTools>()
                    .WithTasksPlus(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);
            });

            AddMinimalMvc();
            TestHostBuilder.ConfigureWebHost(web =>
            {
                web.Configure(app =>
                {
                    app.UseMiddleware<FakeAuthenticationMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMcp());
                });
            });
            TestSetup();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates an HTTP client that sends <c>X-User-Id</c> on every request.
        /// </summary>
        /// <param name="userId">The fake user identifier.</param>
        /// <returns>A client bound to <see cref="AspNetCoreBreakdanceTestBase.TestServer"/>.</returns>
        protected HttpClient CreateUserClient(string userId)
        {
            var client = TestServer.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation(FakeAuthenticationMiddleware.HeaderName, userId);
            return client;
        }

        #endregion

    }

}

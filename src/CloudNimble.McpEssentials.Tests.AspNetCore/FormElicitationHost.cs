using System.Net.Http;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
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
    /// Breakdance TestServer with form-elicitation tools, HelloWorld, and <c>WithTasksPlus</c>.
    /// </summary>
    public abstract class FormElicitationHost : AspNetCoreBreakdanceTestBase
    {

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            TestHostBuilder.ConfigureServices((_, services) =>
            {
                services.AddHttpContextAccessor();
                services.AddFormElicitation("ship",
                    FormElicitationBuilder.Create("Ship this order")
                        .String("trackingNumber").Required());
                services.AddFormElicitation("typed",
                    FormElicitationBuilder.From<ShipForm>("Ship this order"));
                services.AddFormElicitation("live",
                    FormElicitationBuilder.Create("Ship this order")
                        .String("trackingNumber").Required());
                services.AddFormElicitation("facade",
                    FormElicitationBuilder.Create("Ship this order")
                        .String("trackingNumber").Required());
                services.AddFormElicitation("class-ship",
                    FormElicitationBuilder.Create("Ship this order")
                        .String("trackingNumber").Required());
                services.AddFormElicitation("destination",
                    FormElicitationBuilder.Create("Where to?")
                        .String("city").Required());
                services.AddFormElicitation("dates",
                    FormElicitationBuilder.Create("When?")
                        .String("when").Required());

                services.AddMcpServer()
                    .WithHttpTransport(options => options.Stateless = true)
                    .WithTools<HelloWorldTools>()
                    .WithTools<ShipOrderTools>()
                    .WithTools<ClassLevelFormTools>()
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
        /// <returns>A client bound to the test server.</returns>
        protected HttpClient CreateUserClient(string userId)
        {
            var client = TestServer.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation(FakeAuthenticationMiddleware.HeaderName, userId);
            return client;
        }

        #endregion

    }

}

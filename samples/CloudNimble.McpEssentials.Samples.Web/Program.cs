using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Samples.Web
{

    /// <summary>
    /// Hosts a Streamable HTTP MCP server with fake header authentication and
    /// <c>InMemoryAuthenticatedMcpTaskStore</c>.
    /// </summary>
    public class Program
    {

        #region Public Methods

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddMcpServer()
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<HelloWorldTools>()
                .WithAuthenticatedTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);

            var app = builder.Build();
            app.UseMiddleware<FakeAuthenticationMiddleware>();
            app.MapMcp();
            app.Run();
        }

        #endregion

    }

}

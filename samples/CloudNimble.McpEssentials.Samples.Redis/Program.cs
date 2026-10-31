using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CloudNimble.McpEssentials.Samples.Redis
{

    /// <summary>
    /// Hosts a Streamable HTTP MCP server with fake header authentication and
    /// a Redis task store.
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
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

            builder.Services.AddMcpServer()
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<HelloWorldTools>()
                .WithRedisTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);

            var app = builder.Build();
            app.UseMiddleware<FakeAuthenticationMiddleware>();
            app.MapMcp();
            app.Run();
        }

        #endregion

    }

}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CloudNimble.McpEssentials.Samples.PostgreSQL
{

    /// <summary>
    /// Hosts a Streamable HTTP MCP server with fake header authentication and
    /// a native PostgreSQL task store.
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
            builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
                builder.Configuration.GetConnectionString("Postgres")
                    ?? "Host=localhost;Database=mcpessentials;Username=postgres;Password=postgres"));

            builder.Services.AddMcpServer()
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<HelloWorldTools>()
                .WithPostgreSqlTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);

            var app = builder.Build();
            app.UseMiddleware<FakeAuthenticationMiddleware>();
            app.MapMcp();
            app.Run();
        }

        #endregion

    }

}

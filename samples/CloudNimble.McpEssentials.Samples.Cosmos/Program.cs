using CloudNimble.McpEssentials.Cosmos.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Samples.Cosmos
{

    /// <summary>
    /// Hosts a Streamable HTTP MCP server with fake header authentication and
    /// a Cosmos DB task store.
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
            builder.Services.AddSingleton(_ => new CosmosClient(
                builder.Configuration.GetConnectionString("Cosmos")
                    ?? "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;DisableServerCertificateValidation=True;",
                CosmosMcpTaskStore.DefaultClientOptions()));

            builder.Services.AddMcpServer()
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<HelloWorldTools>()
                .WithCosmosTasks(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);

            var app = builder.Build();
            app.UseMiddleware<FakeAuthenticationMiddleware>();
            app.MapMcp();
            app.Run();
        }

        #endregion

    }

}

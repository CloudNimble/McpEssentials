using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Samples.EntityFrameworkCore
{

    /// <summary>
    /// Hosts a Streamable HTTP MCP server with fake header authentication and
    /// an EF Core task store on SQLite.
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
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=mcp-tasks.db"));

            builder.Services.AddMcpServer()
                .WithHttpTransport(options => options.Stateless = true)
                .WithTools<HelloWorldTools>()
                .WithTasksPlus<AppDbContext>(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User);

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
            }

            app.UseMiddleware<FakeAuthenticationMiddleware>();
            app.MapMcp();
            app.Run();
        }

        #endregion

    }

}

using System.ComponentModel;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.McpEssentials.Core.Tasks;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Samples.EntityFrameworkCore
{

    /// <summary>
    /// A single authenticated hello-world tool. It returns <see cref="Task{TResult}"/> so the
    /// MCP SDK advertises task support and clients that opt into <c>io.modelcontextprotocol/tasks</c>
    /// get a trackable task owned by the caller.
    /// </summary>
    [McpServerToolType]
    public class HelloWorldTools
    {

        #region Public Methods

        /// <summary>
        /// Greets the authenticated user. Async so a task-capable client can poll
        /// <c>tasks/get</c> until it completes.
        /// </summary>
        /// <param name="user">The principal stamped by fake authentication.</param>
        /// <param name="cancellationToken">Cancelled when the client calls <c>tasks/cancel</c>.</param>
        /// <returns>A greeting that includes the current user id.</returns>
        [McpServerTool(Name = "HelloWorld"), Description("Says hello to the authenticated user. Clients that opt into MCP Tasks receive a trackable task.")]
        public static async Task<string> HelloWorld(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var userId = McpTaskStoreBase.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
            {
                return "Unauthenticated. Send the X-User-Id header.";
            }

            await Task.Delay(2000, cancellationToken);
            return $"Hello, {userId}.";
        }

        #endregion

    }

}

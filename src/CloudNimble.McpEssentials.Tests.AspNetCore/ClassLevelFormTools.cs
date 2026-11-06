using System.ComponentModel;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Form elicitation stamped on the type so every method is synchronous under Tasks.
    /// </summary>
    [McpServerToolType]
    [McpElicitForm("class-ship")]
    public class ClassLevelFormTools
    {

        #region Public Methods

        /// <summary>
        /// Ships using the class-level form name.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipClass"), Description("Ships with a class-level form attribute.")]
        public static string ShipClass(RequestContext<CallToolRequestParams> context)
        {
            var result = context.RequireForm("class-ship");
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        }

        #endregion

    }

}

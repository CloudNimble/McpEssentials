using System.ComponentModel;
using System.Text.Json;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CloudNimble.McpEssentials.Tests.AspNetCore
{

    /// <summary>
    /// Form-elicitation tools exercised through Streamable HTTP.
    /// </summary>
    [McpServerToolType]
    public class ShipOrderTools
    {

        #region Public Methods

        /// <summary>
        /// Asks for destination then dates in two MRTR rounds.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The planned trip.</returns>
        [McpServerTool(Name = "PlanTrip"), Description("Plans a trip in two form rounds.")]
        [FormElicitation("destination")]
        [FormElicitation("dates")]
        public static string PlanTrip(RequestContext<CallToolRequestParams> context)
        {
            var destination = context.RequireForm("destination");
            if (!destination.IsAccepted)
            {
                return "Trip cancelled.";
            }

            var dates = context.RequireForm("dates");
            if (!dates.IsAccepted)
            {
                return "Trip cancelled.";
            }

            return $"{destination.Content!["city"].GetString()} {dates.Content!["when"].GetString()}";
        }

        /// <summary>
        /// Calls <c>RequireForm</c> with a blank name so the tool result is an error.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>Never returns on the first visit.</returns>
        [McpServerTool(Name = "ShipBlank"), Description("Blank form name.")]
        [FormElicitation("ship")]
        public static string ShipBlank(RequestContext<CallToolRequestParams> context)
        {
            return context.RequireForm(" ").Action;
        }

        /// <summary>
        /// Passes a field facade into <c>RequireForm</c>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipFacade"), Description("Ships using a field facade.")]
        [FormElicitation("facade")]
        public static string ShipFacade(RequestContext<CallToolRequestParams> context)
        {
            var form = context.Services!.GetRequiredKeyedService<IFormElicitationSource>("facade");
            var result = context.RequireForm(form);
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        }

        /// <summary>
        /// Passes a registered form instance into <c>RequireForm</c>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipLive"), Description("Ships using a live form instance.")]
        [FormElicitation("live")]
        public static string ShipLive(RequestContext<CallToolRequestParams> context)
        {
            var form = context.Services!.GetRequiredKeyedService<IFormElicitationSource>("live");
            var result = context.RequireForm(form);
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        }

        /// <summary>
        /// Passes a custom form source with no name.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>Never returns a success string.</returns>
        [McpServerTool(Name = "ShipNameless"), Description("Nameless custom form.")]
        [FormElicitation("ship")]
        public static string ShipNameless(RequestContext<CallToolRequestParams> context)
        {
            return context.RequireForm(new NamelessFormSource()).Action;
        }

        /// <summary>
        /// Ships using the named <c>ship</c> form.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipOrder"), Description("Asks for a tracking number, then ships.")]
        [FormElicitation("ship")]
        public static string ShipOrder(RequestContext<CallToolRequestParams> context)
        {
            var result = context.RequireForm("ship");
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        }

        /// <summary>
        /// Returns typed <see cref="ShipForm"/> content from the <c>typed</c> form.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipTyped"), Description("Ships with a typed form.")]
        [FormElicitation("typed")]
        public static string ShipTyped(RequestContext<CallToolRequestParams> context)
        {
            var result = context.RequireForm<ShipForm>("typed");
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!.TrackingNumber}.";
        }

        /// <summary>
        /// Returns typed <see cref="ShipForm"/> content from a live form instance.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result.</returns>
        [McpServerTool(Name = "ShipTypedLive"), Description("Ships with a typed live form.")]
        [FormElicitation("typed")]
        public static string ShipTypedLive(RequestContext<CallToolRequestParams> context)
        {
            var form = context.Services!.GetRequiredKeyedService<IFormElicitationSource>("typed");
            var result = context.RequireForm<ShipForm>(form, JsonSerializerOptions.Web);
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!.TrackingNumber}.";
        }

        /// <summary>
        /// Calls <c>RequireForm</c> without <see cref="FormElicitationAttribute"/>.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>The shipping result when the client did not opt into Tasks.</returns>
        [McpServerTool(Name = "ShipUnattributed"), Description("Form tool without the attribute.")]
        public static string ShipUnattributed(RequestContext<CallToolRequestParams> context)
        {
            var result = context.RequireForm("ship");
            if (!result.IsAccepted)
            {
                return "Shipping cancelled.";
            }

            return $"Shipped as {result.Content!["trackingNumber"].GetString()}.";
        }

        /// <summary>
        /// Resolves a form that was never registered.
        /// </summary>
        /// <param name="context">The current <c>tools/call</c> context.</param>
        /// <returns>Never returns a success string.</returns>
        [McpServerTool(Name = "ShipUnknown"), Description("Unknown form name.")]
        [FormElicitation("missing")]
        public static string ShipUnknown(RequestContext<CallToolRequestParams> context)
        {
            return context.RequireForm("missing").Action;
        }

        #endregion

    }

}

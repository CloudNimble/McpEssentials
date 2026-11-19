using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Guard and helper coverage for <see cref="McpEssentials_Core_RequestContextExtensions"/>.
    /// Live round-trips live in Tests.AspNetCore.
    /// </summary>
    [TestClass]
    public class RequestContextFormElicitationExtensionsTests : BreakdanceMSTestBase
    {

        #region Fields

        private FormElicitationBuilder _form = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _form = FormElicitationBuilder.Create("Ship this order");
            _form.String("trackingNumber").Required();
            TestHostBuilder.ConfigureServices(services => services.AddFormElicitation("ship", _form));
            TestSetup();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void GetFormName_BooleanFacade_UsesParentName()
        {
            var form = FormElicitationBuilder.Create("m");
            form.Name = "flag";
            McpEssentials_Core_RequestContextExtensions.GetFormName(form.Boolean("ok")).Should().Be("flag");
        }

        [TestMethod]
        public void GetFormName_ChoiceFacade_UsesParentName()
        {
            var form = FormElicitationBuilder.Create("m");
            form.Name = "pick";
            McpEssentials_Core_RequestContextExtensions.GetFormName(form.Choice("color").Option("red")).Should().Be("pick");
        }

        [TestMethod]
        public void GetFormName_CustomSource_IsNull()
        {
            McpEssentials_Core_RequestContextExtensions.GetFormName(new CustomFormSource()).Should().BeNull();
        }

        [TestMethod]
        public void GetFormName_MultiChoiceFacade_UsesParentName()
        {
            var form = FormElicitationBuilder.Create("m");
            form.Name = "picks";
            McpEssentials_Core_RequestContextExtensions.GetFormName(form.MultiChoice("tags").Option("a")).Should().Be("picks");
        }

        [TestMethod]
        public void GetFormName_NumberFacade_UsesParentName()
        {
            var form = FormElicitationBuilder.Create("m");
            form.Name = "qty";
            McpEssentials_Core_RequestContextExtensions.GetFormName(form.Integer("count")).Should().Be("qty");
        }

        [TestMethod]
        public void GetFormName_StringFacade_UsesParentName()
        {
            var form = FormElicitationBuilder.Create("m");
            form.Name = "ship";
            McpEssentials_Core_RequestContextExtensions.GetFormName(form.String("trackingNumber")).Should().Be("ship");
        }

        [TestMethod]
        public void RequireForm_BlankName_Throws()
        {
            var context = CreateContext();
            var act = () => context.RequireForm("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        public void RequireForm_NamedFirstVisit_ThrowsInputRequired()
        {
            var context = CreateContext(TestHost.Services, elicit: true);

            var act = () => context.RequireForm("ship");
            act.Should().Throw<InputRequiredException>()
                .Which.Result.InputRequests.Should().ContainKey("ship");
        }

        [TestMethod]
        public void RequireForm_NullParams_ThrowsInputRequired()
        {
            var context = CreateContext(TestHost.Services, elicit: true);
            context.Params = null!;

            var act = () => context.RequireForm("ship");
            act.Should().Throw<InputRequiredException>();
        }

        [TestMethod]
        public void RequireForm_NullJsonResponse_ThrowsInputRequired()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            form.Name = "ship";
            var context = CreateContext(elicit: true);
            context.Params!.InputResponses = new Dictionary<string, InputResponse>
            {
                ["ship"] = JsonSerializer.Deserialize<InputResponse>("null")!
            };

            var act = () => context.RequireForm(form);
            act.Should().Throw<InputRequiredException>();
        }

        [TestMethod]
        public void RequireForm_MalformedResponse_ThrowsInputRequired()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            form.Name = "ship";
            var context = CreateContext(elicit: true);
            context.Params!.InputResponses = new Dictionary<string, InputResponse>
            {
                ["ship"] = JsonSerializer.Deserialize<InputResponse>("1")!
            };

            var act = () => context.RequireForm(form);
            act.Should().Throw<InputRequiredException>();
        }

        [TestMethod]
        public void RequireForm_MissingElicitation_Throws()
        {
            var context = CreateContext();
            var act = () => context.RequireForm("ship");
            act.Should().Throw<MissingRequiredClientCapabilityException>();
        }

        [TestMethod]
        public void RequireForm_NoServiceProvider_Throws()
        {
            var context = CreateContext(elicit: true);
            var act = () => context.RequireForm("ship");
            act.Should().Throw<InvalidOperationException>().WithMessage("*no service provider*");
        }

        [TestMethod]
        public void RequireForm_NullContext_Throws()
        {
            RequestContext<CallToolRequestParams> context = null!;
            var act = () => context.RequireForm("ship");
            act.Should().Throw<ArgumentNullException>().WithParameterName("context");
        }

        [TestMethod]
        public void RequireForm_NullForm_Throws()
        {
            var context = CreateContext();
            var act = () => context.RequireForm((IFormElicitationSource)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("form");
        }

        [TestMethod]
        public void RequireForm_ReadsAcceptedResponse()
        {
            var context = CreateContext();
            context.Params!.InputResponses = new Dictionary<string, InputResponse>
            {
                ["ship"] = InputResponse.FromElicitResult(new ElicitResult
                {
                    Action = "accept",
                    Content = new Dictionary<string, JsonElement>
                    {
                        ["trackingNumber"] = JsonSerializer.SerializeToElement("1Z999")
                    }
                })
            };

            var result = context.RequireForm("ship");
            result.IsAccepted.Should().BeTrue();
            result.Content!["trackingNumber"].GetString().Should().Be("1Z999");
        }

        [TestMethod]
        public void RequireForm_TypedForm_BindsContent()
        {
            var form = FormElicitationBuilder.From<BindTargetForm>("m");
            form.Name = "ship";
            var context = CreateContext();
            context.Params!.InputResponses = new Dictionary<string, InputResponse>
            {
                ["ship"] = InputResponse.FromElicitResult(new ElicitResult
                {
                    Action = "accept",
                    Content = new Dictionary<string, JsonElement>
                    {
                        ["name"] = JsonSerializer.SerializeToElement("Ada")
                    }
                })
            };

            var result = context.RequireForm<BindTargetForm>(form);
            result.IsAccepted.Should().BeTrue();
            result.Content!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void RequireForm_Typed_BindsContent()
        {
            var context = CreateContext();
            context.Params!.InputResponses = new Dictionary<string, InputResponse>
            {
                ["ship"] = InputResponse.FromElicitResult(new ElicitResult
                {
                    Action = "accept",
                    Content = new Dictionary<string, JsonElement>
                    {
                        ["name"] = JsonSerializer.SerializeToElement("Ada")
                    }
                })
            };

            var result = context.RequireForm<BindTargetForm>("ship");
            result.IsAccepted.Should().BeTrue();
            result.Content!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void RequireForm_UnnamedForm_Throws()
        {
            var context = CreateContext(elicit: true);
            var form = FormElicitationBuilder.Create("m");
            var act = () => context.RequireForm(form);
            act.Should().Throw<InvalidOperationException>().WithMessage("*no name*");
        }

        [TestMethod]
        public void ResolveNamedForm_UsesRequestServices()
        {
            var context = CreateContext(TestHost.Services);
            var resolved = McpEssentials_Core_RequestContextExtensions.ResolveNamedForm(context, "ship");
            resolved.Should().BeSameAs(_form);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Builds a <c>tools/call</c> context, optionally with a service provider.
        /// </summary>
        /// <param name="services">The request services, or <see langword="null"/>.</param>
        /// <returns>A context for helper tests.</returns>
        internal static RequestContext<CallToolRequestParams> CreateContext(IServiceProvider? services = null, bool elicit = false)
        {
            var transport = new StreamServerTransport(new MemoryStream(), new MemoryStream(), "test", loggerFactory: null);
            var server = McpServer.Create(transport, new McpServerOptions(), loggerFactory: null, serviceProvider: services);
            var request = new JsonRpcRequest { Method = "tools/call" };
            if (elicit)
            {
                request.Context = new JsonRpcMessageContext
                {
                    ClientCapabilities = new ClientCapabilities
                    {
                        Elicitation = new ElicitationCapability
                        {
                            Form = new FormElicitationCapability()
                        }
                    }
                };
            }

            return new RequestContext<CallToolRequestParams>(server, request, new CallToolRequestParams { Name = "ShipOrder" })
            {
                Services = services
            };
        }

        #endregion

    }

}

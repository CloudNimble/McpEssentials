using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for binding accepted elicitation content onto a CLR form type.
    /// </summary>
    [TestClass]
    public class FormElicitationBindTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Bind_AcceptedContent_MapsCamelCaseAndEnums()
        {
            var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement("Ada"),
                    ["age"] = JsonSerializer.SerializeToElement(36),
                    ["active"] = JsonSerializer.SerializeToElement(true),
                    ["color"] = JsonSerializer.SerializeToElement("BlueGreen"),
                    ["colors"] = JsonSerializer.SerializeToElement(new[] { "Red", "BlueGreen" }),
                    ["id"] = JsonSerializer.SerializeToElement(id.ToString())
                }
            };

            var bound = FormElicitation.Bind<BindTargetForm>(raw);

            bound.IsAccepted.Should().BeTrue();
            bound.Content.Should().NotBeNull();
            bound.Content!.Name.Should().Be("Ada");
            bound.Content.Age.Should().Be(36);
            bound.Content.Active.Should().BeTrue();
            bound.Content.Color.Should().Be(ColorChoice.BlueGreen);
            bound.Content.Colors.Should().Equal(ColorChoice.Red, ColorChoice.BlueGreen);
            bound.Content.Id.Should().Be(id);
            bound.Content.Nickname.Should().BeNull();
        }

        [TestMethod]
        public void Bind_PascalCaseKeys_StillMapsWhenCaseInsensitive()
        {
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["Name"] = JsonSerializer.SerializeToElement("Bob")
                }
            };

            FormElicitation.Bind<BindTargetForm>(raw).Content!.Name.Should().Be("Bob");
        }

        [TestMethod]
        [DataRow("cancel")]
        [DataRow("decline")]
        public void Bind_NotAccepted_ReturnsDefaultContent(string action)
        {
            var raw = new ElicitResult
            {
                Action = action,
                Content = new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement("nope")
                }
            };

            var bound = FormElicitation.Bind<BindTargetForm>(raw);
            bound.Action.Should().Be(action);
            bound.IsAccepted.Should().BeFalse();
            bound.Content.Should().BeNull();
        }

        [TestMethod]
        public void Bind_AcceptedNullContent_ReturnsDefault()
        {
            var raw = new ElicitResult { Action = "accept", Content = null };
            var bound = FormElicitation.Bind<BindTargetForm>(raw);
            bound.IsAccepted.Should().BeTrue();
            bound.Content.Should().BeNull();
        }

        [TestMethod]
        public void Bind_NullResult_Throws()
        {
            var act = () => FormElicitation.Bind<BindTargetForm>(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("result");
        }

        [TestMethod]
        public void Bind_UnknownEnum_ThrowsJsonException()
        {
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["color"] = JsonSerializer.SerializeToElement("NotAColor")
                }
            };

            var act = () => FormElicitation.Bind<BindTargetForm>(raw);
            act.Should().Throw<JsonException>();
        }

        [TestMethod]
        public void Bind_ExtraKeys_AreIgnored()
        {
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement("Ada"),
                    ["notAField"] = JsonSerializer.SerializeToElement(1)
                }
            };

            FormElicitation.Bind<BindTargetForm>(raw).Content!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void BindersAndFacades_AreFormSources()
        {
            IFormElicitationSource untyped = FormElicitation.Create("m").String("n");
            IFormElicitationSource<EmptyForm> typed = FormElicitation.From<EmptyForm>("m");
            IFormElicitationSource<EmptyForm> typedField = FormElicitation.From<EmptyForm>("m").String("extra");

            untyped.Build().Message.Should().Be("m");
            typed.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            typedField.Should().BeAssignableTo<IFormElicitationSource>();
        }

        #endregion

    }

}

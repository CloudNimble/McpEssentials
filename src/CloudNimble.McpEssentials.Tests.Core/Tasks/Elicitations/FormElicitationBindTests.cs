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
    /// Coverage for deserializing accepted <see cref="ElicitResult.Content"/> onto a CLR type.
    /// </summary>
    [TestClass]
    public class FormElicitationBindTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void ToTyped_AcceptedContent_MapsCamelCaseAndEnums()
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

            var bound = FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null);

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
        public void ToTyped_PascalCaseKeys_StillMapsWhenCaseInsensitive()
        {
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["Name"] = JsonSerializer.SerializeToElement("Bob")
                }
            };

            FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null).Content!.Name.Should().Be("Bob");
        }

        [TestMethod]
        [DataRow("cancel")]
        [DataRow("decline")]
        public void ToTyped_NotAccepted_ReturnsDefaultContent(string action)
        {
            var raw = new ElicitResult
            {
                Action = action,
                Content = new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement("nope")
                }
            };

            var bound = FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null);
            bound.Action.Should().Be(action);
            bound.IsAccepted.Should().BeFalse();
            bound.Content.Should().BeNull();
        }

        [TestMethod]
        public void ToTyped_AcceptedNullContent_ReturnsDefault()
        {
            var raw = new ElicitResult { Action = "accept", Content = null };
            var bound = FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null);
            bound.IsAccepted.Should().BeTrue();
            bound.Content.Should().BeNull();
        }

        [TestMethod]
        public void ToTyped_NullResult_Throws()
        {
            var act = () => FormElicitationBinder.ToTyped<BindTargetForm>(null!, options: null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("result");
        }

        [TestMethod]
        public void ToTyped_UnknownEnum_ThrowsJsonException()
        {
            var raw = new ElicitResult
            {
                Action = "accept",
                Content = new Dictionary<string, JsonElement>
                {
                    ["color"] = JsonSerializer.SerializeToElement("NotAColor")
                }
            };

            var act = () => FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null);
            act.Should().Throw<JsonException>();
        }

        [TestMethod]
        public void ToTyped_ExtraKeys_AreIgnored()
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

            FormElicitationBinder.ToTyped<BindTargetForm>(raw, options: null).Content!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void BindersAndFacades_AreFormSources()
        {
            IFormElicitationSource untyped = FormElicitationBuilder.Create("m").String("n");
            IFormElicitationSource<EmptyForm> typed = FormElicitationBuilder.From<EmptyForm>("m");
            IFormElicitationSource<EmptyForm> typedField = FormElicitationBuilder.From<EmptyForm>("m").String("extra");

            untyped.Build().Message.Should().Be("m");
            typed.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            typedField.Should().BeAssignableTo<IFormElicitationSource>();
        }

        #endregion

    }

}

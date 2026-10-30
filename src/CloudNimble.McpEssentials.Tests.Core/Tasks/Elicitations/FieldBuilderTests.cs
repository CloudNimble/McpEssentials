using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for untyped field facades: constraints, forwarding, and Build/ToInputRequest.
    /// </summary>
    [TestClass]
    public class FieldBuilderTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void StringFacade_ConstraintsAndForwarding()
        {
            var facade = FormElicitation.Create("m").String("name")
                .Title("N")
                .Description("d")
                .Required()
                .Optional()
                .MinLength(0)
                .MaxLength(0)
                .Default("x");

            facade.Field.Required.Should().BeFalse();
            facade.Field.MinLength.Should().Be(0);
            facade.Field.DefaultString.Should().Be("x");

            facade.String("a").Field.Name.Should().Be("a");
            facade.Email("b").Field.Format.Should().Be("email");
            facade.Uri("c").Field.Format.Should().Be("uri");
            facade.Date("d1").Field.Format.Should().Be("date");
            facade.DateTime("e").Field.Format.Should().Be("date-time");
            facade.Integer("f").Field.Kind.Should().Be(ElicitationFieldKind.Integer);
            facade.Number("g").Field.Kind.Should().Be(ElicitationFieldKind.Number);
            facade.Boolean("h").Field.Kind.Should().Be(ElicitationFieldKind.Boolean);
            facade.Choice("i").Option("x").Field.Kind.Should().Be(ElicitationFieldKind.Choice);
            facade.MultiChoice("j").Option("y").Field.Kind.Should().Be(ElicitationFieldKind.MultiChoice);

            facade.Build().Message.Should().Be("m");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            facade.ToXml().Should().Contain("<Form ");
            facade.ToDefinition().Message.Should().Be("m");
        }

        [TestMethod]
        public void NumberFacade_ConstraintsAndForwarding()
        {
            var facade = FormElicitation.Create("m").Integer("age")
                .Title("Age")
                .Description("Years")
                .Required()
                .Optional()
                .Min(1)
                .Max(9)
                .Default(5);

            facade.Field.Minimum.Should().Be(1);
            facade.Field.Maximum.Should().Be(9);
            facade.Field.DefaultNumber.Should().Be(5);
            facade.Field.Required.Should().BeFalse();

            facade.Between(2, 8);
            facade.Field.Minimum.Should().Be(2);
            facade.Field.Maximum.Should().Be(8);

            facade.String("a").Field.Kind.Should().Be(ElicitationFieldKind.String);
            facade.Email("b").Field.Format.Should().Be("email");
            facade.Uri("c").Field.Format.Should().Be("uri");
            facade.Date("d").Field.Format.Should().Be("date");
            facade.DateTime("e").Field.Format.Should().Be("date-time");
            facade.Integer("f").Field.Kind.Should().Be(ElicitationFieldKind.Integer);
            facade.Number("g").Field.Kind.Should().Be(ElicitationFieldKind.Number);
            facade.Boolean("h").Field.Kind.Should().Be(ElicitationFieldKind.Boolean);
            facade.Choice("i").Option("x").Field.Kind.Should().Be(ElicitationFieldKind.Choice);
            facade.MultiChoice("j").Option("y").Field.Kind.Should().Be(ElicitationFieldKind.MultiChoice);
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("age");
            facade.ToInputRequest().ElicitationParams.Should().NotBeNull();
            facade.ToXml().Should().Contain("Type=\"integer\"");
            facade.ToDefinition().Fields.Should().Contain(f => f.Name == "age");
        }

        [TestMethod]
        public void BooleanFacade_ConstraintsAndForwarding()
        {
            var facade = FormElicitation.Create("m").Boolean("on")
                .Title("On")
                .Description("Switch")
                .Required()
                .Default(true)
                .Optional();

            facade.Field.DefaultBool.Should().BeTrue();
            facade.Field.Required.Should().BeFalse();

            facade.String("a");
            facade.Email("b");
            facade.Uri("c");
            facade.Date("d");
            facade.DateTime("e");
            facade.Integer("f");
            facade.Number("g");
            facade.Boolean("h");
            facade.Choice("i").Option("x");
            facade.MultiChoice("j").Option("y");
            facade.Build().Mode.Should().Be("form");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            facade.ToXml().Should().Contain("Type=\"boolean\"");
            facade.ToDefinition().Message.Should().Be("m");
        }

        [TestMethod]
        public void ChoiceFacade_ConstraintsAndForwarding()
        {
            var facade = FormElicitation.Create("m").Choice("color")
                .Title("Color")
                .Description("Pick")
                .Required()
                .Optional()
                .Option("red")
                .Option("blue", "Blue")
                .Default("red");

            facade.Field.Options.Should().HaveCount(2);
            facade.Field.DefaultString.Should().Be("red");

            facade.String("a");
            facade.Email("b");
            facade.Uri("c");
            facade.Date("d");
            facade.DateTime("e");
            facade.Integer("f");
            facade.Number("g");
            facade.Boolean("h");
            facade.Choice("i").Option("x");
            facade.MultiChoice("j").Option("y");
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("color");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            facade.ToXml().Should().Contain("Type=\"enum\"");
            facade.ToDefinition().Fields.Should().Contain(f => f.Name == "color");
        }

        [TestMethod]
        public void MultiChoiceFacade_ConstraintsAndForwarding()
        {
            var facade = FormElicitation.Create("m").MultiChoice("roles")
                .Title("Roles")
                .Description("Pick")
                .Required()
                .Optional()
                .Option("admin")
                .Option("user", "User")
                .MinItems(0)
                .MaxItems(2)
                .Default("admin", "user");

            facade.Field.DefaultStrings.Should().Equal("admin", "user");
            facade.Field.MinItems.Should().Be(0);

            IEnumerable<string> generated = Yield("admin");
            facade.Default(generated);
            facade.Field.DefaultStrings.Should().Equal("admin");

            var list = new List<string> { "user" };
            facade.Default(list);
            facade.Field.DefaultStrings.Should().BeSameAs(list);

            facade.String("a");
            facade.Email("b");
            facade.Uri("c");
            facade.Date("d");
            facade.DateTime("e");
            facade.Integer("f");
            facade.Number("g");
            facade.Boolean("h");
            facade.Choice("i").Option("x");
            facade.MultiChoice("j").Option("y");
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("roles");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
            facade.ToXml().Should().Contain("Type=\"multi\"");
            facade.ToDefinition().Fields.Should().Contain(f => f.Name == "roles");
        }

        [TestMethod]
        public void UntypedBuilder_ForwardsFromEachStarter()
        {
            var form = FormElicitation.Create("m");
            form.String("s").Field.Kind.Should().Be(ElicitationFieldKind.String);
            form.Email("e").Field.Format.Should().Be("email");
            form.Uri("u").Field.Format.Should().Be("uri");
            form.Date("d").Field.Format.Should().Be("date");
            form.DateTime("t").Field.Format.Should().Be("date-time");
            form.Integer("i").Field.Kind.Should().Be(ElicitationFieldKind.Integer);
            form.Number("n").Field.Kind.Should().Be(ElicitationFieldKind.Number);
            form.Boolean("b").Field.Kind.Should().Be(ElicitationFieldKind.Boolean);
            form.Choice("c").Field.Kind.Should().Be(ElicitationFieldKind.Choice);
            form.MultiChoice("m1").Field.Kind.Should().Be(ElicitationFieldKind.MultiChoice);
            form.Fields.Select(f => f.Name).Should().Equal("s", "e", "u", "d", "t", "i", "n", "b", "c", "m1");
        }

        [TestMethod]
        public void Remove_MissingName_IsNoOp()
        {
            var form = FormElicitation.Create("m");
            form.String("keep");
            form.Remove("missing");
            form.Fields.Should().HaveCount(1);
            form.Remove("keep");
            form.Fields.Should().BeEmpty();
        }

        #endregion

        #region Private Methods

        private static IEnumerable<string> Yield(params string[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        #endregion

    }

}

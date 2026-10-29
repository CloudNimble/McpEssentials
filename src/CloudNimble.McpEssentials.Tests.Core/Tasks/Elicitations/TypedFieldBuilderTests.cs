using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for typed field facades: covariant constraint methods, Exclude, and starters.
    /// </summary>
    [TestClass]
    public class TypedFieldBuilderTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void TypedStringFacade_HitsEveryMember()
        {
            var facade = FormElicitation.From<KitchenSinkForm>("m").String(x => x.Name)
                .Title("N")
                .Description("d")
                .Required()
                .Optional()
                .MinLength(1)
                .MaxLength(8)
                .Default("Ada");

            facade.Should().BeOfType<StringFieldBuilder<KitchenSinkForm>>();
            facade.Exclude(x => x.Nickname).Should().BeOfType<FormElicitationBuilder<KitchenSinkForm>>();

            facade = FormElicitation.From<KitchenSinkForm>("m").String(x => x.Name);
            facade.String("extra").Should().BeOfType<StringFieldBuilder<KitchenSinkForm>>();
            facade.String(x => x.Nickname).Should().BeOfType<StringFieldBuilder<KitchenSinkForm>>();
            facade.Email("e").Should().BeOfType<StringFieldBuilder<KitchenSinkForm>>();
            facade.Email(x => x.Nickname).Field.Format.Should().Be("email");
            facade.Uri("u").Field.Format.Should().Be("uri");
            facade.Uri(x => x.Homepage).Field.Format.Should().Be("uri");
            facade.Date("d").Field.Format.Should().Be("date");
            facade.Date(x => x.Birthday).Field.Format.Should().Be("date");
            facade.DateTime("t").Field.Format.Should().Be("date-time");
            facade.DateTime(x => x.CreatedAt).Field.Format.Should().Be("date-time");
            facade.Integer("i").Should().BeOfType<NumberFieldBuilder<KitchenSinkForm>>();
            facade.Integer(x => x.Age).Field.Kind.Should().Be(ElicitationFieldKind.Integer);
            facade.Number("n").Should().BeOfType<NumberFieldBuilder<KitchenSinkForm>>();
            facade.Number(x => x.Price).Field.Kind.Should().Be(ElicitationFieldKind.Number);
            facade.Boolean("b").Should().BeOfType<BooleanFieldBuilder<KitchenSinkForm>>();
            facade.Boolean(x => x.Active).Field.Kind.Should().Be(ElicitationFieldKind.Boolean);
            facade.Choice("c").Option("x").Should().BeOfType<ChoiceFieldBuilder<KitchenSinkForm>>();
            facade.Choice(x => x.Color).Field.Kind.Should().Be(ElicitationFieldKind.Choice);
            facade.MultiChoice("mc").Option("y").Should().BeOfType<MultiChoiceFieldBuilder<KitchenSinkForm>>();
            facade.MultiChoice(x => x.Access).Field.Kind.Should().Be(ElicitationFieldKind.MultiChoice);
            facade.Build().Mode.Should().Be("form");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
        }

        [TestMethod]
        public void TypedNumberFacade_HitsEveryMember()
        {
            var facade = FormElicitation.From<KitchenSinkForm>("m").Integer(x => x.Age)
                .Title("Age")
                .Description("Years")
                .Required()
                .Optional()
                .Min(0)
                .Max(120)
                .Between(1, 99)
                .Default(18);

            facade.Should().BeOfType<NumberFieldBuilder<KitchenSinkForm>>();
            facade.Field.Minimum.Should().Be(1);
            facade.Exclude(x => x.FloatValue).Should().BeOfType<FormElicitationBuilder<KitchenSinkForm>>();

            facade = FormElicitation.From<KitchenSinkForm>("m").Number(x => x.Price);
            facade.String("s").Should().BeOfType<StringFieldBuilder<KitchenSinkForm>>();
            facade.String(x => x.Name);
            facade.Email("e");
            facade.Email(x => x.Nickname);
            facade.Uri("u");
            facade.Uri(x => x.Homepage);
            facade.Date("d");
            facade.Date(x => x.Birthday);
            facade.DateTime("t");
            facade.DateTime(x => x.CreatedAt);
            facade.Integer("i");
            facade.Integer(x => x.Age);
            facade.Number("n");
            facade.Number(x => x.Price);
            facade.Boolean("b");
            facade.Boolean(x => x.Active);
            facade.Choice("c").Option("x");
            facade.Choice(x => x.Color);
            facade.MultiChoice("mc").Option("y");
            facade.MultiChoice(x => x.Access);
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("price");
            facade.ToInputRequest().ElicitationParams.Should().NotBeNull();
        }

        [TestMethod]
        public void TypedBooleanFacade_HitsEveryMember()
        {
            var facade = FormElicitation.From<KitchenSinkForm>("m").Boolean(x => x.Active)
                .Title("On")
                .Description("Switch")
                .Required()
                .Optional()
                .Default(false);

            facade.Should().BeOfType<BooleanFieldBuilder<KitchenSinkForm>>();
            facade.Exclude(x => x.Name).Should().BeOfType<FormElicitationBuilder<KitchenSinkForm>>();

            facade = FormElicitation.From<KitchenSinkForm>("m").Boolean(x => x.Active);
            facade.String("s");
            facade.String(x => x.Name);
            facade.Email("e");
            facade.Email(x => x.Nickname);
            facade.Uri("u");
            facade.Uri(x => x.Homepage);
            facade.Date("d");
            facade.Date(x => x.Birthday);
            facade.DateTime("t");
            facade.DateTime(x => x.CreatedAt);
            facade.Integer("i");
            facade.Integer(x => x.Age);
            facade.Number("n");
            facade.Number(x => x.Price);
            facade.Boolean("b");
            facade.Boolean(x => x.Active);
            facade.Choice("c").Option("x");
            facade.Choice(x => x.Color);
            facade.MultiChoice("mc").Option("y");
            facade.MultiChoice(x => x.Access);
            facade.Build().Mode.Should().Be("form");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
        }

        [TestMethod]
        public void TypedChoiceFacade_HitsEveryMember()
        {
            var facade = FormElicitation.From<KitchenSinkForm>("m").Choice(x => x.Color)
                .Title("Color")
                .Description("Pick")
                .Required()
                .Optional()
                .Option("extra")
                .Option("other", "Other")
                .Default("Red");

            facade.Should().BeOfType<ChoiceFieldBuilder<KitchenSinkForm>>();
            facade.Exclude(x => x.Access).Should().BeOfType<FormElicitationBuilder<KitchenSinkForm>>();

            facade = FormElicitation.From<KitchenSinkForm>("m").Choice(x => x.Color);
            facade.String("s");
            facade.String(x => x.Name);
            facade.Email("e");
            facade.Email(x => x.Nickname);
            facade.Uri("u");
            facade.Uri(x => x.Homepage);
            facade.Date("d");
            facade.Date(x => x.Birthday);
            facade.DateTime("t");
            facade.DateTime(x => x.CreatedAt);
            facade.Integer("i");
            facade.Integer(x => x.Age);
            facade.Number("n");
            facade.Number(x => x.Price);
            facade.Boolean("b");
            facade.Boolean(x => x.Active);
            facade.Choice("c").Option("x");
            facade.Choice(x => x.Color);
            facade.MultiChoice("mc").Option("y");
            facade.MultiChoice(x => x.Access);
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("color");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
        }

        [TestMethod]
        public void TypedMultiChoiceFacade_HitsEveryMember()
        {
            var facade = FormElicitation.From<KitchenSinkForm>("m").MultiChoice(x => x.Access)
                .Title("Access")
                .Description("Pick")
                .Required()
                .Optional()
                .Option("extra")
                .Option("other", "Other")
                .MinItems(0)
                .MaxItems(3)
                .Default("Read");

            facade.Default(new List<string> { "Write" });
            facade.Should().BeOfType<MultiChoiceFieldBuilder<KitchenSinkForm>>();
            facade.Exclude(x => x.Colors).Should().BeOfType<FormElicitationBuilder<KitchenSinkForm>>();

            facade = FormElicitation.From<KitchenSinkForm>("m").MultiChoice(x => x.Access);
            facade.String("s");
            facade.String(x => x.Name);
            facade.Email("e");
            facade.Email(x => x.Nickname);
            facade.Uri("u");
            facade.Uri(x => x.Homepage);
            facade.Date("d");
            facade.Date(x => x.Birthday);
            facade.DateTime("t");
            facade.DateTime(x => x.CreatedAt);
            facade.Integer("i");
            facade.Integer(x => x.Age);
            facade.Number("n");
            facade.Number(x => x.Price);
            facade.Boolean("b");
            facade.Boolean(x => x.Active);
            facade.Choice("c").Option("x");
            facade.Choice(x => x.Color);
            facade.MultiChoice("mc").Option("y");
            facade.MultiChoice(x => x.Access);
            facade.Build().RequestedSchema!.Properties.Should().ContainKey("access");
            facade.ToInputRequest().Method.Should().Be(RequestMethods.ElicitationCreate);
        }

        [TestMethod]
        public void TypedChoice_AddingOptionToSeededEnum_KeepsExisting()
        {
            var built = FormElicitation.From<KitchenSinkForm>("m")
                .Choice(x => x.Color).Option("Custom", "Custom color")
                .Build();

            var color = built.RequestedSchema!.Properties["color"].Should().BeOfType<ElicitRequestParams.TitledSingleSelectEnumSchema>().Subject;
            color.OneOf.Should().Contain(o => o.Const == "Red");
            color.OneOf.Should().Contain(o => o.Const == "Custom" && o.Title == "Custom color");
        }

        #endregion

    }

}

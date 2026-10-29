using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for <see cref="FormElicitationBuilder{T}"/> expression starters and string extras.
    /// </summary>
    [TestClass]
    public class TypedFormElicitationBuilderTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void ExpressionStarters_ReconfigureSeededFields()
        {
            var builder = FormElicitation.From<KitchenSinkForm>("m");

            builder.String(x => x.Name).Field.Name.Should().Be("name");
            builder.Email(x => x.Nickname).Field.Format.Should().Be("email");
            builder.Uri(x => x.Homepage).Field.Format.Should().Be("uri");
            builder.Date(x => x.Birthday).Field.Format.Should().Be("date");
            builder.DateTime(x => x.CreatedAt).Field.Format.Should().Be("date-time");
            builder.Integer(x => x.Age).Field.Kind.Should().Be(ElicitationFieldKind.Integer);
            builder.Number(x => x.Price).Field.Kind.Should().Be(ElicitationFieldKind.Number);
            builder.Boolean(x => x.Active).Field.Kind.Should().Be(ElicitationFieldKind.Boolean);
            builder.Choice(x => x.Color).Field.Kind.Should().Be(ElicitationFieldKind.Choice);
            builder.MultiChoice(x => x.Access).Field.Kind.Should().Be(ElicitationFieldKind.MultiChoice);
        }

        [TestMethod]
        public void StringNamedStarters_AddExtraFieldsWithoutDroppingT()
        {
            var built = FormElicitation.From<EmptyForm>("m")
                .String("notes")
                .Email("email")
                .Uri("site")
                .Date("day")
                .DateTime("when")
                .Integer("count")
                .Number("amount")
                .Boolean("ok")
                .Choice("pick").Option("a")
                .MultiChoice("many").Option("b")
                .Build();

            built.RequestedSchema!.Properties.Keys.Should().BeEquivalentTo(
                "notes", "email", "site", "day", "when", "count", "amount", "ok", "pick", "many");
        }

        [TestMethod]
        public void String_OnTypedBuilder_ClearsFormat()
        {
            var builder = FormElicitation.From<KitchenSinkForm>("m");
            builder.Email("homepage");
            builder.Fields.Find(f => f.Name == "homepage")!.Format.Should().Be("email");
            builder.String("homepage");
            builder.Fields.Find(f => f.Name == "homepage")!.Format.Should().BeNull();
        }

        [TestMethod]
        public void Exclude_FromBuilder_ThenBuild()
        {
            var built = FormElicitation.From<DerivedSignupForm>("m")
                .Exclude(x => x.Email)
                .Build();

            built.RequestedSchema!.Properties.Keys.Should().Equal("name");
        }

        [TestMethod]
        public void ChainStaysTyped_AfterExpressionAndString()
        {
            var built = FormElicitation.From<KitchenSinkForm>("Need details")
                .Exclude(x => x.Nickname)
                .String(x => x.Name).MinLength(1)
                .Integer("extraCount").Min(0)
                .Boolean(x => x.Active).Default(false)
                .Build();

            built.RequestedSchema!.Properties.Should().ContainKey("name");
            built.RequestedSchema!.Properties.Should().ContainKey("extraCount");
            built.RequestedSchema!.Properties.Should().NotContainKey("nickname");
            built.RequestedSchema!.Properties["active"].Should().BeOfType<ElicitRequestParams.BooleanSchema>()
                .Which.Default.Should().BeFalse();
        }

        #endregion

    }

}

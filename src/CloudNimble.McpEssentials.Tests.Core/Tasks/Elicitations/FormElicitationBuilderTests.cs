using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System.Linq;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Happy-path coverage for untyped <see cref="FormElicitationBuilder"/> schema projection.
    /// </summary>
    [TestClass]
    public class FormElicitationBuilderTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Build_EmptyForm_IsConfirmDialog()
        {
            var built = FormElicitation.Create("Are you sure?").Build();

            built.Mode.Should().Be("form");
            built.Message.Should().Be("Are you sure?");
            built.RequestedSchema!.Properties.Should().BeEmpty();
            built.RequestedSchema!.Required.Should().BeNull();
        }

        [TestMethod]
        public void Build_ProjectsEveryPrimitiveKind()
        {
            var built = FormElicitation.Create("Fill this out")
                .String("firstName").Title("Given name").Description("Legal first name").Required().MinLength(1).MaxLength(40).Default("Ada")
                .Email("email").Required()
                .Uri("homepage")
                .Date("birthday")
                .DateTime("startsAt")
                .Integer("age").Min(0).Max(130).Default(42)
                .Number("price").Between(0.5, 99.9).Default(9.99)
                .Boolean("subscribe").Default(true).Optional()
                .Choice("color").Option("red").Option("blue").Default("red")
                .MultiChoice("roles").Option("admin", "Administrator").Option("user", "User").MinItems(1).MaxItems(2).Default("user")
                .Build();

            built.Mode.Should().Be("form");
            built.RequestedSchema!.Required.Should().Equal("firstName", "email");

            var firstName = built.RequestedSchema!.Properties["firstName"].Should().BeOfType<ElicitRequestParams.StringSchema>().Subject;
            firstName.Title.Should().Be("Given name");
            firstName.Description.Should().Be("Legal first name");
            firstName.MinLength.Should().Be(1);
            firstName.MaxLength.Should().Be(40);
            firstName.Default.Should().Be("Ada");
            firstName.Format.Should().BeNull();

            built.RequestedSchema!.Properties["email"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("email");
            built.RequestedSchema!.Properties["homepage"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("uri");
            built.RequestedSchema!.Properties["birthday"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date");
            built.RequestedSchema!.Properties["startsAt"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date-time");

            var age = built.RequestedSchema!.Properties["age"].Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            age.Type.Should().Be("integer");
            age.Minimum.Should().Be(0);
            age.Maximum.Should().Be(130);
            age.Default.Should().Be(42);

            var price = built.RequestedSchema!.Properties["price"].Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            price.Type.Should().Be("number");
            price.Minimum.Should().Be(0.5);
            price.Maximum.Should().Be(99.9);
            price.Default.Should().Be(9.99);

            var subscribe = built.RequestedSchema!.Properties["subscribe"].Should().BeOfType<ElicitRequestParams.BooleanSchema>().Subject;
            subscribe.Default.Should().BeTrue();
            built.RequestedSchema!.Required.Should().NotContain("subscribe");

            var color = built.RequestedSchema!.Properties["color"].Should().BeOfType<ElicitRequestParams.UntitledSingleSelectEnumSchema>().Subject;
            color.Enum.Should().Equal("red", "blue");
            color.Default.Should().Be("red");

            var roles = built.RequestedSchema!.Properties["roles"].Should().BeOfType<ElicitRequestParams.TitledMultiSelectEnumSchema>().Subject;
            roles.MinItems.Should().Be(1);
            roles.MaxItems.Should().Be(2);
            roles.Default.Should().Equal("user");
            roles.Items.AnyOf.Should().HaveCount(2);
            roles.Items.AnyOf[0].Const.Should().Be("admin");
            roles.Items.AnyOf[0].Title.Should().Be("Administrator");
            roles.Items.AnyOf[1].Title.Should().Be("User");
        }

        [TestMethod]
        public void Build_TitledSingleSelect_WhenAnyOptionHasTitle()
        {
            var built = FormElicitation.Create("Pick")
                .Choice("color").Option("red", "Crimson").Option("blue").Build();

            var color = built.RequestedSchema!.Properties["color"].Should().BeOfType<ElicitRequestParams.TitledSingleSelectEnumSchema>().Subject;
            color.OneOf.Select(o => o.Const).Should().Equal("red", "blue");
            color.OneOf[0].Title.Should().Be("Crimson");
            color.OneOf[1].Title.Should().Be("blue");
        }

        [TestMethod]
        public void Build_UntitledMultiSelect_WhenNoOptionHasTitle()
        {
            var built = FormElicitation.Create("Pick")
                .MultiChoice("roles").Option("admin").Option("user").Default("admin", "user").Build();

            var roles = built.RequestedSchema!.Properties["roles"].Should().BeOfType<ElicitRequestParams.UntitledMultiSelectEnumSchema>().Subject;
            roles.Items.Enum.Should().Equal("admin", "user");
            roles.Default.Should().Equal("admin", "user");
        }

        [TestMethod]
        public void String_CamelCasesNameAndTitleizes()
        {
            var builder = FormElicitation.Create("m").String("TimeToLive");

            builder.Field.Name.Should().Be("timeToLive");
            builder.Field.Title.Should().Be("Time To Live");
        }

        [TestMethod]
        public void String_ReconfigureClearsFormat()
        {
            var form = FormElicitation.Create("m");
            form.Email("contact");
            form.Fields.Should().HaveCount(1);
            form.Fields[0].Format.Should().Be("email");

            form.String("contact");
            form.Fields.Should().HaveCount(1);
            form.Fields[0].Kind.Should().Be(ElicitationFieldKind.String);
            form.Fields[0].Format.Should().BeNull();
        }

        [TestMethod]
        public void Starter_ReconfigureDifferentKind_ResetsConstraints()
        {
            var form = FormElicitation.Create("m");
            form.String("age").MinLength(2).MaxLength(4).Default("42");
            form.Integer("age");

            form.Fields.Should().HaveCount(1);
            var field = form.Fields[0];
            field.Kind.Should().Be(ElicitationFieldKind.Integer);
            field.MinLength.Should().BeNull();
            field.MaxLength.Should().BeNull();
            field.DefaultString.Should().BeNull();
            field.Title.Should().Be("Age");
        }

        [TestMethod]
        public void Starter_SameName_KeepsTitleAndRequired()
        {
            var form = FormElicitation.Create("m");
            form.String("name").Title("Custom").Required();
            form.String("name").MinLength(1);

            form.Fields.Should().HaveCount(1);
            form.Fields[0].Title.Should().Be("Custom");
            form.Fields[0].Required.Should().BeTrue();
            form.Fields[0].MinLength.Should().Be(1);
        }

        [TestMethod]
        public void ToInputRequest_WrapsElicitationCreate()
        {
            var request = FormElicitation.Create("Need a name")
                .String("name").Required()
                .ToInputRequest();

            request.Method.Should().Be(RequestMethods.ElicitationCreate);
            request.ElicitationParams.Should().NotBeNull();
            request.ElicitationParams!.Message.Should().Be("Need a name");
            request.ElicitationParams.RequestedSchema!.Properties.Should().ContainKey("name");
        }

        [TestMethod]
        public void Constructor_AcceptsMessage()
        {
            var builder = new FormElicitationBuilder("hello");
            builder.Message.Should().Be("hello");
        }

        [TestMethod]
        public void MinLengthEqualsMaxLength_IsValid()
        {
            var built = FormElicitation.Create("m").String("pin").MinLength(4).MaxLength(4).Build();
            var pin = built.RequestedSchema!.Properties["pin"].Should().BeOfType<ElicitRequestParams.StringSchema>().Subject;
            pin.MinLength.Should().Be(4);
            pin.MaxLength.Should().Be(4);
        }

        [TestMethod]
        public void Between_EqualBounds_IsValid()
        {
            var built = FormElicitation.Create("m").Integer("n").Between(3, 3).Build();
            var n = built.RequestedSchema!.Properties["n"].Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            n.Minimum.Should().Be(3);
            n.Maximum.Should().Be(3);
        }

        [TestMethod]
        public void FormatStarters_OverwriteEachOther()
        {
            var form = FormElicitation.Create("m");
            form.Email("when");
            form.Date("when");
            form.DateTime("when");
            form.Uri("when");

            form.Fields.Should().HaveCount(1);
            form.Fields[0].Format.Should().Be("uri");
        }

        #endregion

    }

}

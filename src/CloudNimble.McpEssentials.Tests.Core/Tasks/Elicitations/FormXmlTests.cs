using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for the compact <c>Form</c> XML codec: round-trip, dialect, and fail-first parse.
    /// </summary>
    [TestClass]
    public class FormXmlTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void RoundTrip_KitchenSink_BuildSchemaMatches()
        {
            var form = KitchenSink();
            var original = form.Build();
            var xml = form.ToXml();
            var rebuilt = FormElicitation.ParseXml(xml).Build();

            AssertSchemaEquals(original, rebuilt);
        }

        [TestMethod]
        public void ToXml_WritesRequiredTrue_OmitsRequiredFalse()
        {
            var xml = FormElicitation.Create("Ship this order")
                .String("trackingNumber").Required()
                .String("note")
                .ToXml();

            xml.Should().Contain("Required=\"true\"");
            xml.Should().NotContain("Required=\"false\"");
        }

        [TestMethod]
        public void ToXml_WritesSelectedTrue_OnDefaultOptions()
        {
            var xml = FormElicitation.Create("Pick")
                .Choice("priority").Option("low").Option("high").Default("high")
                .MultiChoice("roles").Option("admin").Option("user").Default("admin", "user")
                .ToXml();

            xml.Should().Contain("Type=\"enum\"");
            xml.Should().Contain("Type=\"multi\"");
            xml.Should().Contain("Selected=\"true\"");
            xml.Should().NotContain("Selected=\"false\"");
            xml.Should().NotContain("Default=");
        }

        [TestMethod]
        public void ToXml_WritesIntegerAndNumberTypeSpellings()
        {
            var xml = FormElicitation.Create("n")
                .Integer("age")
                .Number("price")
                .ToXml();

            xml.Should().Contain("Type=\"integer\"");
            xml.Should().Contain("Type=\"number\"");
        }

        [TestMethod]
        public void ToXml_XmlNamespace_EmitsXmlnsOnForm()
        {
            const string ns = "https://github.com/odata/mcp/vocabularies/Microsoft.OData.Mcp.V1.xml";
            var xml = FormElicitation.Create("Ship this order")
                .String("trackingNumber").Required()
                .ToXml(ns);

            xml.Should().Contain($"xmlns=\"{ns}\"");
            xml.Should().StartWith("<Form ");
            xml.Should().NotContain("<?xml");
        }

        [TestMethod]
        public void Parse_IgnoresNamespace()
        {
            const string xml =
                """
                <Form xmlns="https://example.invalid/form" Message="Ship this order">
                  <Field Name="trackingNumber" Type="string" Required="true"/>
                </Form>
                """;

            var built = FormElicitation.ParseXml(xml).Build();
            built.Message.Should().Be("Ship this order");
            built.RequestedSchema!.Properties.Should().ContainKey("trackingNumber");
            built.RequestedSchema.Required.Should().Equal("trackingNumber");
        }

        [TestMethod]
        public void Parse_AcceptsTrueTrueAndOne_ForRequired()
        {
            foreach (var token in new[] { "true", "True", "1" })
            {
                var xml = $"""<Form Message="m"><Field Name="a" Type="string" Required="{token}"/></Form>""";
                FormXml.Parse(xml).Fields[0].Required.Should().BeTrue();
            }
        }

        [TestMethod]
        public void Parse_UnknownType_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="object"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form field Type 'object'*");
        }

        [TestMethod]
        public void Parse_EmptyMessage_ThrowsInvalidOperationException()
        {
            var act = () => FormXml.Parse("""<Form Message="   "/>""");
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        public void Parse_Dtd_ThrowsXmlException()
        {
            const string xml =
                """
                <!DOCTYPE Form [
                  <!ENTITY xxe SYSTEM "file:///etc/passwd">
                ]>
                <Form Message="x">&xxe;</Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Parse_DuplicateFieldName_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="a" Type="string"/>
                  <Field Name="a" Type="integer"/>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*already has a field named 'a'*");
        }

        [TestMethod]
        public void Parse_DefaultOnEnum_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="color" Type="enum" Default="red">
                    <Option Value="red"/>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Selected on Option*");
        }

        [TestMethod]
        public void FromDefinition_DuplicateWireName_ThrowsInvalidOperationException()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("FirstName", FormFieldType.String),
                new FormFieldDefinition("firstName", FormFieldType.String)
            ]);

            var act = () => FormElicitation.FromDefinition(definition);
            act.Should().Throw<InvalidOperationException>().WithMessage("*already has a field named*");
        }

        [TestMethod]
        public void FromDefinition_Null_ThrowsArgumentNullException()
        {
            var act = () => FormElicitation.FromDefinition(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("definition");
        }

        [TestMethod]
        public void ToXml_EmptyMessage_ThrowsInvalidOperationException()
        {
            var act = () => FormElicitation.Create("").ToXml();
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        public void FacadeToXml_MatchesParentBuilder()
        {
            var form = FormElicitation.Create("Ship this order");
            var facade = form.String("trackingNumber").Required();

            facade.ToXml().Should().Be(form.ToXml());
            facade.ToDefinition().Message.Should().Be(form.ToDefinition().Message);
        }

        [TestMethod]
        public void TypedFacadeToXml_MatchesParentBuilder()
        {
            var form = FormElicitation.From<EmptyForm>("Confirm?");
            var facade = form.String("note");

            facade.ToXml().Should().Be(form.ToXml());
        }

        [TestMethod]
        public void ToXml_AndParse_HaveNoRequiresDynamicCode()
        {
            AssertNoDynamicCode(typeof(FormXml).GetMethod(nameof(FormXml.Serialize), [typeof(FormDefinition), typeof(string)]));
            AssertNoDynamicCode(typeof(FormXml).GetMethod(nameof(FormXml.Parse), [typeof(string)]));
            AssertNoDynamicCode(typeof(FormElicitationBuilder).GetMethod(nameof(FormElicitationBuilder.ToXml), [typeof(string)]));
            AssertNoDynamicCode(typeof(FormElicitationBuilder).GetMethod(nameof(FormElicitationBuilder.ToDefinition), Type.EmptyTypes));
        }

        [TestMethod]
        public void ToDefinition_CopiesOptionalName()
        {
            var form = FormElicitation.Create("Ship this order");
            form.Name = "ship";
            form.String("trackingNumber").Required();

            var definition = form.ToDefinition();
            definition.Name.Should().Be("ship");
            var xml = form.ToXml();
            xml.Should().Contain("Name=\"ship\"");
            FormElicitation.ParseXml(xml).Name.Should().Be("ship");
        }

        [TestMethod]
        public void RoundTrip_ConfirmDialog_EmptyFields()
        {
            var xml = FormElicitation.Create("Are you sure?").ToXml();
            var rebuilt = FormElicitation.ParseXml(xml).Build();
            rebuilt.Message.Should().Be("Are you sure?");
            rebuilt.RequestedSchema!.Properties.Should().BeEmpty();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the kitchen-sink form used by round-trip tests.
        /// </summary>
        /// <returns>The form.</returns>
        private static IFormElicitationSource KitchenSink()
        {
            return FormElicitation.Create("Fill this out")
                .String("firstName").Title("Given name").Description("Legal first name").Required().MinLength(1).MaxLength(40).Default("Ada")
                .Email("email").Required()
                .Uri("homepage")
                .Date("birthday")
                .DateTime("startsAt")
                .Integer("age").Min(0).Max(130).Default(42)
                .Number("price").Between(0.5, 99.9).Default(9.99)
                .Boolean("subscribe").Default(true).Optional()
                .Choice("color").Option("red").Option("blue").Default("red")
                .MultiChoice("roles").Option("admin", "Administrator").Option("user", "User").MinItems(1).MaxItems(2).Default("user");
        }

        /// <summary>
        /// Asserts two <see cref="ElicitRequestParams"/> kitchen-sink schemas match.
        /// </summary>
        /// <param name="original">The original <see cref="FormElicitationBuilder.Build"/> result.</param>
        /// <param name="rebuilt">The result after XML round-trip.</param>
        private static void AssertSchemaEquals(ElicitRequestParams original, ElicitRequestParams rebuilt)
        {
            rebuilt.Mode.Should().Be(original.Mode);
            rebuilt.Message.Should().Be(original.Message);
            rebuilt.RequestedSchema!.Required.Should().Equal(original.RequestedSchema!.Required);
            rebuilt.RequestedSchema.Properties.Keys.Should().Equal(original.RequestedSchema.Properties.Keys);

            var firstName = rebuilt.RequestedSchema.Properties["firstName"].Should().BeOfType<ElicitRequestParams.StringSchema>().Subject;
            firstName.Title.Should().Be("Given name");
            firstName.Description.Should().Be("Legal first name");
            firstName.MinLength.Should().Be(1);
            firstName.MaxLength.Should().Be(40);
            firstName.Default.Should().Be("Ada");
            firstName.Format.Should().BeNull();

            rebuilt.RequestedSchema.Properties["email"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("email");
            rebuilt.RequestedSchema.Properties["homepage"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("uri");
            rebuilt.RequestedSchema.Properties["birthday"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date");
            rebuilt.RequestedSchema.Properties["startsAt"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date-time");

            var age = rebuilt.RequestedSchema.Properties["age"].Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            age.Type.Should().Be("integer");
            age.Minimum.Should().Be(0);
            age.Maximum.Should().Be(130);
            age.Default.Should().Be(42);

            var price = rebuilt.RequestedSchema.Properties["price"].Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            price.Type.Should().Be("number");
            price.Minimum.Should().Be(0.5);
            price.Maximum.Should().Be(99.9);
            price.Default.Should().Be(9.99);

            rebuilt.RequestedSchema.Properties["subscribe"].Should().BeOfType<ElicitRequestParams.BooleanSchema>()
                .Which.Default.Should().BeTrue();

            var color = rebuilt.RequestedSchema.Properties["color"].Should().BeOfType<ElicitRequestParams.UntitledSingleSelectEnumSchema>().Subject;
            color.Enum.Should().Equal("red", "blue");
            color.Default.Should().Be("red");

            var roles = rebuilt.RequestedSchema.Properties["roles"].Should().BeOfType<ElicitRequestParams.TitledMultiSelectEnumSchema>().Subject;
            roles.MinItems.Should().Be(1);
            roles.MaxItems.Should().Be(2);
            roles.Default.Should().Equal("user");
            roles.Items.AnyOf.Should().HaveCount(2);
            roles.Items.AnyOf[0].Const.Should().Be("admin");
            roles.Items.AnyOf[0].Title.Should().Be("Administrator");
        }

        /// <summary>
        /// Asserts <paramref name="method"/> is not annotated with <see cref="RequiresDynamicCodeAttribute"/>.
        /// </summary>
        /// <param name="method">The method under test.</param>
        private static void AssertNoDynamicCode(MethodInfo? method)
        {
            method.Should().NotBeNull();
            method!.GetCustomAttributes(typeof(RequiresDynamicCodeAttribute), inherit: true)
                .Should().BeEmpty();
        }

        #endregion

    }

}

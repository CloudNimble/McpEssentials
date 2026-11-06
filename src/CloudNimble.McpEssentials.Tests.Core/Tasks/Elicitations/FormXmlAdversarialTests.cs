using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Fail-first coverage for the compact XML codec and <see cref="FormElicitationBuilder.FromDefinition"/>.
    /// </summary>
    [TestClass]
    public class FormXmlAdversarialTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Parse_BlankXml_ThrowsArgumentException(string? xml)
        {
            var act = () => FormXml.Parse(xml!);
            act.Should().Throw<ArgumentException>().WithParameterName("xml");
        }

        [TestMethod]
        public void Parse_NoFormRoot_ThrowsInvalidOperationException()
        {
            var act = () => FormXml.Parse("""<Nope Message="m"/>""");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Form root*");
        }

        [TestMethod]
        public void Parse_CommentOnly_ThrowsXmlException()
        {
            var act = () => FormXml.Parse("<!-- no form -->");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Parse_MalformedXml_ThrowsXmlException()
        {
            var act = () => FormXml.Parse("""<Form Message="m">""");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Parse_FormWithoutAttributes_ThrowsInvalidOperationException()
        {
            var act = () => FormXml.Parse("<Form/>");
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        public void Parse_UnknownFormAttribute_ThrowsInvalidOperationException()
        {
            var act = () => FormXml.Parse("""<Form Message="m" Mode="dialog"/>""");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form Form attribute 'Mode'*");
        }

        [TestMethod]
        public void Parse_UnexpectedFormChild_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Nope/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected child 'Nope'*");
        }

        [TestMethod]
        public void Parse_FormMixedContent_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m">hello<Field Name="a" Type="string"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*mixed content*");
        }

        [TestMethod]
        public void Parse_XmlLangOnForm_IsIgnored()
        {
            const string xml = """<Form xml:lang="en" Message="m"><Field Name="a" Type="string"/></Form>""";
            FormXml.Parse(xml).Fields.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parse_XmlnsPrefixOnForm_IsIgnored()
        {
            const string xml = """<Form xmlns:foo="https://example.invalid" Message="m"><Field Name="a" Type="string"/></Form>""";
            FormXml.Parse(xml).Message.Should().Be("m");
        }

        [TestMethod]
        public void Parse_FieldWithoutAttributes_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Field Name is required*");
        }

        [TestMethod]
        public void Parse_FieldMissingType_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*missing Type*");
        }

        [TestMethod]
        public void Parse_UnknownFieldAttribute_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string" Pattern="x+"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form Field attribute 'Pattern'*");
        }

        [TestMethod]
        public void Parse_UnexpectedFieldChild_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string"><Nope/></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected child 'Nope'*");
        }

        [TestMethod]
        public void Parse_FieldMixedContent_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string">oops</Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*mixed content*");
        }

        [TestMethod]
        public void Parse_FieldCData_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string"><![CDATA[x]]></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*mixed content*");
        }

        [TestMethod]
        public void Parse_XmlLangOnField_IsIgnored()
        {
            const string xml = """<Form Message="m"><Field xml:lang="en" Name="a" Type="string"/></Form>""";
            FormXml.Parse(xml).Fields[0].Name.Should().Be("a");
        }

        [TestMethod]
        public void Parse_OptionWithoutAttributes_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="c" Type="enum"><Option/></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Option Value is required*");
        }

        [TestMethod]
        public void Parse_UnknownOptionAttribute_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="c" Type="enum"><Option Value="red" Extra="1"/></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form Option attribute 'Extra'*");
        }

        [TestMethod]
        public void Parse_UnexpectedOptionChild_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="c" Type="enum"><Option Value="red"><Nope/></Option></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected child 'Nope'*");
        }

        [TestMethod]
        public void Parse_OptionMixedContent_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="c" Type="enum"><Option Value="red">x</Option></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*mixed content*");
        }

        [TestMethod]
        public void Parse_XmlLangOnOption_IsIgnored()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="c" Type="enum">
                    <Option xml:lang="en" Value="red"/>
                  </Field>
                </Form>
                """;

            FormXml.Parse(xml).Fields[0].Options[0].Value.Should().Be("red");
        }

        [TestMethod]
        [DataRow("false")]
        [DataRow("False")]
        [DataRow("0")]
        public void Parse_RequiredFalseTokens_AreNotRequired(string token)
        {
            var xml = $"""<Form Message="m"><Field Name="a" Type="string" Required="{token}"/></Form>""";
            FormXml.Parse(xml).Fields[0].Required.Should().BeFalse();
        }

        [TestMethod]
        public void Parse_InvalidBoolean_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string" Required="yes"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a boolean*");
        }

        [TestMethod]
        public void Parse_InvalidMinLength_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string" MinLength="nope"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not an integer*");
        }

        [TestMethod]
        public void Parse_OverflowMinLength_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string" MinLength="2147483648"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not an integer*");
        }

        [TestMethod]
        public void Parse_InvalidMin_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="age" Type="integer" Min="nope"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a number*");
        }

        [TestMethod]
        public void Parse_DefaultOnMulti_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="roles" Type="multi" Default="admin">
                    <Option Value="admin"/>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Selected on Option*");
        }

        [TestMethod]
        public void Parse_TwoSelectedOnEnum_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="color" Type="enum">
                    <Option Value="red" Selected="true"/>
                    <Option Value="blue" Selected="true"/>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*at most one Selected*");
        }

        [TestMethod]
        public void Parse_MultiMinOutOfRange_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="roles" Type="multi" Min="1E+20">
                    <Option Value="admin"/>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*out of range*");
        }

        [TestMethod]
        public void Parse_BooleanWithoutDefault_HasNoDefault()
        {
            const string xml = """<Form Message="m"><Field Name="on" Type="boolean"/></Form>""";
            FormXml.Parse(xml).Fields[0].DefaultBool.Should().BeNull();
        }

        [TestMethod]
        public void Parse_BooleanDefaultFalse_RoundTrips()
        {
            const string xml = """<Form Message="m"><Field Name="on" Type="boolean" Default="false"/></Form>""";
            var parsed = FormXml.Parse(xml);
            parsed.Fields[0].DefaultBool.Should().BeFalse();

            var rebuilt = FormElicitationBuilder.FromDefinition(parsed).Build();
            rebuilt.RequestedSchema!.Properties["on"].Should().BeOfType<ModelContextProtocol.Protocol.ElicitRequestParams.BooleanSchema>()
                .Which.Default.Should().BeFalse();
        }

        [TestMethod]
        public void Parse_IntegerOnlyMin_AndNumberOnlyMax()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="age" Type="integer" Min="0"/>
                  <Field Name="price" Type="number" Max="9.9"/>
                </Form>
                """;

            var parsed = FormXml.Parse(xml);
            parsed.Fields[0].Minimum.Should().Be(0);
            parsed.Fields[0].Maximum.Should().BeNull();
            parsed.Fields[1].Maximum.Should().Be(9.9);
            parsed.Fields[1].Minimum.Should().BeNull();

            var rebuilt = FormElicitationBuilder.FromDefinition(parsed);
            rebuilt.Fields[0].Minimum.Should().Be(0);
            rebuilt.Fields[1].Maximum.Should().Be(9.9);
        }

        [TestMethod]
        public void Parse_MultiOnlyMin_AndOnlyMax()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="roles" Type="multi" Min="1">
                    <Option Value="admin"/>
                    <Option Value="user" Selected="true"/>
                  </Field>
                  <Field Name="tags" Type="multi" Max="3">
                    <Option Value="a"/>
                  </Field>
                </Form>
                """;

            var parsed = FormXml.Parse(xml);
            parsed.Fields[0].MinItems.Should().Be(1);
            parsed.Fields[0].MaxItems.Should().BeNull();
            parsed.Fields[0].DefaultStrings.Should().Equal("user");
            parsed.Fields[1].MaxItems.Should().Be(3);
            parsed.Fields[1].MinItems.Should().BeNull();
            parsed.Fields[1].DefaultStrings.Should().BeNull();
        }

        [TestMethod]
        public void Parse_EnumWithNoSelected_HasNoDefault()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="color" Type="enum">
                    <Option Value="red"/>
                    <Option Value="blue"/>
                  </Field>
                </Form>
                """;

            FormXml.Parse(xml).Fields[0].DefaultString.Should().BeNull();
        }

        [TestMethod]
        public void Parse_NonEmptyFieldWithoutChildren_IsValid()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string"></Field></Form>""";
            FormXml.Parse(xml).Fields[0].Name.Should().Be("a");
        }

        [TestMethod]
        public void Serialize_NullDefinition_ThrowsArgumentNullException()
        {
            var act = () => FormXml.Serialize(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("definition");
        }

        [TestMethod]
        public void Serialize_UnknownFieldType_ThrowsInvalidOperationException()
        {
            var field = new FormFieldDefinition("a", (FormFieldType)99);
            var definition = new FormDefinition("m", [field]);
            var act = () => FormXml.Serialize(definition);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form field type*");
        }

        [TestMethod]
        public void Serialize_WhitespaceNamespace_OmitsXmlns()
        {
            var xml = FormElicitationBuilder.Create("m").String("a").ToXml("  ");
            xml.Should().NotContain("xmlns=");
        }

        [TestMethod]
        public void Serialize_BooleanFalseAndIntegerZeroDefaults()
        {
            var xml = FormElicitationBuilder.Create("m")
                .Boolean("on").Default(false)
                .Integer("n").Default(0)
                .ToXml();

            xml.Should().Contain("Default=\"false\"");
            xml.Should().Contain("Default=\"0\"");
        }

        [TestMethod]
        public void Serialize_SelectedFromDefaultStringWithoutOptionFlag()
        {
            var field = new FormFieldDefinition("color", FormFieldType.Enum)
            {
                DefaultString = "red",
                Options =
                [
                    new FormOptionDefinition("red"),
                    new FormOptionDefinition("blue")
                ]
            };
            var xml = FormXml.Serialize(new FormDefinition("m", [field]));
            xml.Should().Contain("Value=\"red\"");
            xml.Should().Contain("Selected=\"true\"");
            xml.Should().NotContain("Default=");
        }

        [TestMethod]
        public void Serialize_SelectedFromDefaultStringsWithoutOptionFlag()
        {
            var field = new FormFieldDefinition("roles", FormFieldType.Multi)
            {
                DefaultStrings = ["admin"],
                Options =
                [
                    new FormOptionDefinition("admin"),
                    new FormOptionDefinition("user")
                ]
            };
            var xml = FormXml.Serialize(new FormDefinition("m", [field]));
            xml.Should().Contain("Value=\"admin\"");
            xml.Should().Contain("Selected=\"true\"");
        }

        [TestMethod]
        public void Serialize_DefaultStringsThatMatchNoOption_OmitsSelected()
        {
            var field = new FormFieldDefinition("roles", FormFieldType.Multi)
            {
                DefaultStrings = ["missing"],
                Options = [new FormOptionDefinition("admin")]
            };
            var xml = FormXml.Serialize(new FormDefinition("m", [field]));
            xml.Should().NotContain("Selected=");
        }

        [TestMethod]
        public void Serialize_WhitespaceTitle_IsOmitted()
        {
            var field = new FormFieldDefinition("a", FormFieldType.String)
            {
                Title = "  ",
                Description = "  "
            };
            var xml = FormXml.Serialize(new FormDefinition("m", [field]));
            xml.Should().NotContain("Title=");
            xml.Should().NotContain("Description=");
        }

        [TestMethod]
        public void Serialize_NumberOnlyMin_AndOnlyMax()
        {
            var fields = new FormFieldDefinition[]
            {
                new("age", FormFieldType.Integer) { Minimum = 1 },
                new("cap", FormFieldType.Integer) { Maximum = 9 },
                new("roles", FormFieldType.Multi)
                {
                    MinItems = 1,
                    Options = [new FormOptionDefinition("a")]
                },
                new("tags", FormFieldType.Multi)
                {
                    MaxItems = 3,
                    Options = [new FormOptionDefinition("a")]
                }
            };
            var xml = FormXml.Serialize(new FormDefinition("m", fields));
            xml.Should().Contain("Min=\"1\"");
            xml.Should().Contain("Max=\"9\"");
            xml.Should().Contain("Max=\"3\"");
        }

        [TestMethod]
        public void FromDefinition_NullField_ThrowsArgumentNullException()
        {
            var definition = new FormDefinition("m", [null!]);
            var act = () => FormElicitationBuilder.FromDefinition(definition);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void FromDefinition_UnknownFormat_ThrowsInvalidOperationException()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("phone", FormFieldType.String) { Format = "tel" }
            ]);
            var act = () => FormElicitationBuilder.FromDefinition(definition);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown string format 'tel'*");
        }

        [TestMethod]
        public void FromDefinition_UnknownFieldType_ThrowsInvalidOperationException()
        {
            var builder = FormElicitationBuilder.Create("m");
            var act = () => builder.ApplyField(new FormFieldDefinition("a", (FormFieldType)99));
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown form field type*");
        }

        [TestMethod]
        public void FromDefinition_UnknownKind_ThrowsInvalidOperationException()
        {
            var act = () => FormElicitationBuilder.ToFieldType((ElicitationFieldKind)99);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Unknown elicitation field kind*");
        }

        [TestMethod]
        public void FromDefinition_ApplyField_Null_ThrowsArgumentNullException()
        {
            var act = () => FormElicitationBuilder.Create("m").ApplyField(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("field");
        }

        [TestMethod]
        public void FromDefinition_InvertedNumberBounds_ThrowsArgumentException()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("age", FormFieldType.Integer) { Minimum = 10, Maximum = 1 }
            ]);
            var act = () => FormElicitationBuilder.FromDefinition(definition);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void FromDefinition_EmptyFormat_UsesStringStarter()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("note", FormFieldType.String) { Format = "" }
            ]);
            FormElicitationBuilder.FromDefinition(definition).Fields[0].Format.Should().BeNull();
        }

        [TestMethod]
        public void FromDefinition_BooleanWithoutDefault_AndChoiceWithoutSelected()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("on", FormFieldType.Boolean),
                new FormFieldDefinition("color", FormFieldType.Enum)
                {
                    Options = [new FormOptionDefinition("red"), new FormOptionDefinition("blue")]
                },
                new FormFieldDefinition("roles", FormFieldType.Multi)
                {
                    Options = [new FormOptionDefinition("admin")]
                }
            ]);

            var builder = FormElicitationBuilder.FromDefinition(definition);
            builder.Fields[0].DefaultBool.Should().BeNull();
            builder.Fields[1].DefaultString.Should().BeNull();
            builder.Fields[2].DefaultStrings.Should().BeNull();
        }

        [TestMethod]
        public void FromDefinition_WhitespaceTitle_KeepsTitleize()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("firstName", FormFieldType.String)
                {
                    Title = "  ",
                    Description = "  "
                }
            ]);

            var field = FormElicitationBuilder.FromDefinition(definition).Fields[0];
            field.Title.Should().Be("First Name");
            field.Description.Should().BeNull();
        }

        [TestMethod]
        public void FromDefinition_SelectedOptionWithoutDefaultString()
        {
            var definition = new FormDefinition("m",
            [
                new FormFieldDefinition("color", FormFieldType.Enum)
                {
                    Options = [new FormOptionDefinition("red", selected: true), new FormOptionDefinition("blue")]
                },
                new FormFieldDefinition("roles", FormFieldType.Multi)
                {
                    MinItems = 1,
                    MaxItems = 2,
                    Options =
                    [
                        new FormOptionDefinition("admin", selected: true),
                        new FormOptionDefinition("user", selected: true)
                    ]
                }
            ]);

            var builder = FormElicitationBuilder.FromDefinition(definition);
            builder.Fields[0].DefaultString.Should().Be("red");
            builder.Fields[1].DefaultStrings.Should().Equal("admin", "user");
            builder.Fields[1].MinItems.Should().Be(1);
            builder.Fields[1].MaxItems.Should().Be(2);
        }

        [TestMethod]
        public void ToDefinition_ChoiceWithoutOptions_ThrowsInvalidOperationException()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("c").ToDefinition();
            act.Should().Throw<InvalidOperationException>().WithMessage("*no options*");
        }

        [TestMethod]
        public void ToXmlType_RoundTripsEveryDeclaredType()
        {
            foreach (FormFieldType type in Enum.GetValues<FormFieldType>())
            {
                FormXml.ParseFieldType(FormXml.ToXmlType(type)).Should().Be(type);
            }
        }

        [TestMethod]
        public void Parse_XmlDeclarationBeforeForm_IsSkipped()
        {
            const string xml =
                """
                <?xml version="1.0"?>
                <Form Message="m"><Field Name="a" Type="string"/></Form>
                """;

            FormXml.Parse(xml).Fields[0].Name.Should().Be("a");
        }

        [TestMethod]
        public void Parse_SelfClosingForm_HasNoFields()
        {
            FormXml.Parse("""<Form Message="Are you sure?"/>""").Fields.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_ProcessingInstructionInsideForm_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><?pi data?><Field Name="a" Type="string"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected node*");
        }

        [TestMethod]
        public void Parse_ProcessingInstructionInsideField_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string"><?pi data?></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected node*");
        }

        [TestMethod]
        public void Parse_ProcessingInstructionInsideOption_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="c" Type="enum">
                    <Option Value="red"><?pi data?></Option>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*unexpected node*");
        }

        [TestMethod]
        public void Parse_WhitespaceFieldName_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="  " Type="string"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Field Name is required*");
        }

        [TestMethod]
        public void Parse_WhitespaceOptionValue_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="c" Type="enum"><Option Value="  "/></Field></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Option Value is required*");
        }

        [TestMethod]
        public void Parse_NonEmptyOption_RoundTrips()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="c" Type="enum">
                    <Option Value="red"></Option>
                  </Field>
                </Form>
                """;

            FormXml.Parse(xml).Fields[0].Options[0].Value.Should().Be("red");
        }

        [TestMethod]
        public void Parse_MultiMaxOutOfRange_ThrowsInvalidOperationException()
        {
            const string xml =
                """
                <Form Message="m">
                  <Field Name="roles" Type="multi" Max="1E+20">
                    <Option Value="admin"/>
                  </Field>
                </Form>
                """;

            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*out of range*");
        }

        [TestMethod]
        public void Parse_InvalidIntegerDefault_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="age" Type="integer" Default="nope"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a number*");
        }

        [TestMethod]
        public void Parse_InvalidBooleanDefault_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="on" Type="boolean" Default="yes"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a boolean*");
        }

        [TestMethod]
        public void Parse_InvalidNumberDefault_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="price" Type="number" Default="nope"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a number*");
        }

        [TestMethod]
        [DataRow("false")]
        [DataRow("False")]
        [DataRow("0")]
        public void Parse_SelectedFalseTokens_AreNotSelected(string token)
        {
            var xml = $"""<Form Message="m"><Field Name="c" Type="enum"><Option Value="red" Selected="{token}"/></Field></Form>""";
            FormXml.Parse(xml).Fields[0].Options[0].Selected.Should().BeFalse();
            FormXml.Parse(xml).Fields[0].DefaultString.Should().BeNull();
        }

        [TestMethod]
        public void Parse_OverflowMaxLength_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m"><Field Name="a" Type="string" MaxLength="2147483648"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not an integer*");
        }

        [TestMethod]
        public void Parse_XmlSpacePreserveWhitespace_ThrowsInvalidOperationException()
        {
            const string xml = """<Form Message="m" xml:space="preserve"> <Field Name="a" Type="string"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*mixed content*");
        }

        [TestMethod]
        [DataRow("1E+400")]
        [DataRow("INF")]
        [DataRow("NaN")]
        [DataRow("-INF")]
        public void Parse_NonFiniteMin_ThrowsInvalidOperationException(string token)
        {
            var xml = $"""<Form Message="m"><Field Name="age" Type="integer" Min="{token}"/></Form>""";
            var act = () => FormXml.Parse(xml);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not a number*");
        }

        [TestMethod]
        public void Parse_DefaultXmlnsOnField_IsIgnored()
        {
            const string xml = """<Form Message="m"><Field xmlns="https://example.invalid" Name="a" Type="string"/></Form>""";
            FormXml.Parse(xml).Fields[0].Name.Should().Be("a");
        }

        [TestMethod]
        public void ParseXml_Blank_ThrowsArgumentException()
        {
            var act = () => FormElicitationBuilder.ParseXml("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("xml");
        }

        [TestMethod]
        public void ToFieldDefinition_StringKindWithOptions_MarksNoneSelected()
        {
            var field = new ElicitationField
            {
                Name = "note",
                Kind = ElicitationFieldKind.String
            };
            field.AddOption("x", "X");

            var definition = FormElicitationBuilder.ToFieldDefinition(field);
            definition.Options.Should().ContainSingle();
            definition.Options[0].Selected.Should().BeFalse();
            definition.Options[0].Value.Should().Be("x");
            definition.DefaultString.Should().BeNull();
        }

        [TestMethod]
        public void ToFieldDefinition_MultiChoiceUnmatchedDefault_MarksNoneSelected()
        {
            var field = new ElicitationField
            {
                Name = "roles",
                Kind = ElicitationFieldKind.MultiChoice,
                DefaultStrings = ["missing"]
            };
            field.AddOption("admin", null);
            field.AddOption("user", null);

            var definition = FormElicitationBuilder.ToFieldDefinition(field);
            definition.Options.Should().OnlyContain(option => !option.Selected);
            definition.DefaultStrings.Should().Equal("missing");
        }

        [TestMethod]
        public void ToFieldDefinition_CopiesDefaultStrings()
        {
            var live = new List<string> { "admin" };
            var field = new ElicitationField
            {
                Name = "roles",
                Kind = ElicitationFieldKind.MultiChoice,
                DefaultStrings = live
            };
            field.AddOption("admin", null);

            var definition = FormElicitationBuilder.ToFieldDefinition(field);
            live.Add("user");
            definition.DefaultStrings.Should().Equal("admin");
        }

        [TestMethod]
        public void ToDefinition_ChoiceWithoutDefault_HasUnselectedOptions()
        {
            var definition = FormElicitationBuilder.Create("m")
                .Choice("color").Option("red").Option("blue")
                .ToDefinition();

            definition.Fields[0].DefaultString.Should().BeNull();
            definition.Fields[0].Options.Should().OnlyContain(option => !option.Selected);
        }

        #endregion

    }

}

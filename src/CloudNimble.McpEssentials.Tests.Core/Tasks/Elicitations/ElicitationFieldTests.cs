using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Direct coverage for the mutable field model, including unknown kinds and option duplicates.
    /// </summary>
    [TestClass]
    public class ElicitationFieldTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Validate_EmptyName_Throws()
        {
            var field = new ElicitationField { Name = "  ", Kind = ElicitationFieldKind.String };
            var act = () => field.Validate();
            act.Should().Throw<InvalidOperationException>().WithMessage("*name cannot be empty*");
        }

        [TestMethod]
        public void ToSchema_UnknownKind_Throws()
        {
            var field = new ElicitationField { Name = "x", Kind = (ElicitationFieldKind)99 };
            var act = () => field.ToSchema();
            act.Should().Throw<InvalidOperationException>().WithMessage("*unknown kind*");
        }

        [TestMethod]
        public void AddOption_Duplicate_Throws()
        {
            var field = new ElicitationField { Name = "color", Kind = ElicitationFieldKind.Choice };
            field.AddOption("red", null);
            var act = () => field.AddOption("red", "Crimson");
            act.Should().Throw<InvalidOperationException>().WithMessage("*already has option 'red'*");
        }

        [TestMethod]
        public void AddOption_BlankValue_Throws()
        {
            var field = new ElicitationField { Name = "color" };
            var act = () => field.AddOption(" ", "title");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ResetKind_KeepsIdentityClearsConstraints()
        {
            var field = new ElicitationField
            {
                Name = "age",
                Title = "Age",
                Description = "Years",
                Required = true,
                Kind = ElicitationFieldKind.String,
                Format = "email",
                MinLength = 1,
                MaxLength = 2,
                Minimum = 0,
                Maximum = 1,
                MinItems = 1,
                MaxItems = 2,
                DefaultString = "x",
                DefaultNumber = 1,
                DefaultBool = true,
                DefaultStrings = ["a"]
            };
            field.AddOption("a", "A");

            field.ResetKind(ElicitationFieldKind.Integer);

            field.Name.Should().Be("age");
            field.Title.Should().Be("Age");
            field.Description.Should().Be("Years");
            field.Required.Should().BeTrue();
            field.Kind.Should().Be(ElicitationFieldKind.Integer);
            field.Format.Should().BeNull();
            field.MinLength.Should().BeNull();
            field.MaxLength.Should().BeNull();
            field.Minimum.Should().BeNull();
            field.Maximum.Should().BeNull();
            field.MinItems.Should().BeNull();
            field.MaxItems.Should().BeNull();
            field.DefaultString.Should().BeNull();
            field.DefaultNumber.Should().BeNull();
            field.DefaultBool.Should().BeNull();
            field.DefaultStrings.Should().BeNull();
            field.Options.Should().BeEmpty();
        }

        [TestMethod]
        public void ToSchema_BooleanAndNumber_ProjectDefaults()
        {
            var boolean = new ElicitationField
            {
                Name = "on",
                Title = "On",
                Description = "Switch",
                Kind = ElicitationFieldKind.Boolean,
                DefaultBool = false
            };
            var number = new ElicitationField
            {
                Name = "n",
                Kind = ElicitationFieldKind.Number,
                Minimum = 1.5,
                Maximum = 3.5,
                DefaultNumber = 2.5
            };

            var boolSchema = boolean.ToSchema().Should().BeOfType<ElicitRequestParams.BooleanSchema>().Subject;
            boolSchema.Default.Should().BeFalse();
            boolSchema.Title.Should().Be("On");
            boolSchema.Description.Should().Be("Switch");

            var numberSchema = number.ToSchema().Should().BeOfType<ElicitRequestParams.NumberSchema>().Subject;
            numberSchema.Type.Should().Be("number");
            numberSchema.Default.Should().Be(2.5);
        }

        [TestMethod]
        public void ToSchema_TitledChoice_FillsMissingOptionTitlesWithValue()
        {
            var field = new ElicitationField { Name = "c", Kind = ElicitationFieldKind.Choice };
            field.AddOption("red", "Crimson");
            field.AddOption("blue", null);

            var schema = field.ToSchema().Should().BeOfType<ElicitRequestParams.TitledSingleSelectEnumSchema>().Subject;
            schema.OneOf[1].Const.Should().Be("blue");
            schema.OneOf[1].Title.Should().Be("blue");
        }

        [TestMethod]
        public void Validate_DefaultStringsEmptyList_DoesNotThrow()
        {
            var field = new ElicitationField
            {
                Name = "roles",
                Kind = ElicitationFieldKind.MultiChoice,
                DefaultStrings = []
            };
            field.AddOption("admin", null);

            var act = () => field.Validate();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ElicitationOption_Defaults()
        {
            var option = new ElicitationOption();
            option.Value.Should().BeEmpty();
            option.Title.Should().BeNull();
        }

        #endregion

    }

}

using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Constructor fail-first coverage for the public snapshot types.
    /// </summary>
    [TestClass]
    public class FormSnapshotTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void FormDefinition_BlankMessage_ThrowsArgumentException(string? message)
        {
            var act = () => new FormDefinition(message!, []);
            act.Should().Throw<ArgumentException>().WithParameterName("message");
        }

        [TestMethod]
        public void FormDefinition_NullFields_ThrowsArgumentNullException()
        {
            var act = () => new FormDefinition("m", null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("fields");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void FormDefinition_BlankName_IsNull(string? name)
        {
            new FormDefinition("m", [], name).Name.Should().BeNull();
        }

        [TestMethod]
        public void FormDefinition_Name_IsPreserved()
        {
            new FormDefinition("m", [], "ship").Name.Should().Be("ship");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void FormFieldDefinition_BlankName_ThrowsArgumentException(string? name)
        {
            var act = () => new FormFieldDefinition(name!, FormFieldType.String);
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void FormOptionDefinition_BlankValue_ThrowsArgumentException(string? value)
        {
            var act = () => new FormOptionDefinition(value!);
            act.Should().Throw<ArgumentException>().WithParameterName("value");
        }

        [TestMethod]
        public void FormOptionDefinition_StoresTitleAndSelected()
        {
            var option = new FormOptionDefinition("red", "Crimson", selected: true);
            option.Value.Should().Be("red");
            option.Title.Should().Be("Crimson");
            option.Selected.Should().BeTrue();
        }

        [TestMethod]
        public void FormFieldDefinition_InitFacets_AreReadable()
        {
            var field = new FormFieldDefinition("age", FormFieldType.Integer)
            {
                Required = true,
                Minimum = 0,
                Maximum = 120,
                DefaultNumber = 42
            };

            field.Name.Should().Be("age");
            field.Type.Should().Be(FormFieldType.Integer);
            field.Required.Should().BeTrue();
            field.Minimum.Should().Be(0);
            field.Maximum.Should().Be(120);
            field.DefaultNumber.Should().Be(42);
            field.Options.Should().BeEmpty();
        }

        #endregion

    }

}

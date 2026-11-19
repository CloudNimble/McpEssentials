using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Adversarial coverage: empty names, inverted bounds, missing options, bad defaults.
    /// </summary>
    [TestClass]
    public class FormElicitationBuilderAdversarialTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_NullMessage_ThrowsArgumentNullException()
        {
            var act = () => FormElicitationBuilder.Create(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("message");
        }

        [TestMethod]
        public void Build_WhitespaceMessage_ThrowsInvalidOperationException()
        {
            var act = () => FormElicitationBuilder.Create(" \t ").Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void String_BlankName_ThrowsArgumentException(string? name)
        {
            var act = () => FormElicitationBuilder.Create("m").String(name!);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Choice_NoOptions_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*no options*");
        }

        [TestMethod]
        public void MultiChoice_NoOptions_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("roles").Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*no options*");
        }

        [TestMethod]
        public void Choice_DuplicateOption_ThrowsImmediately()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Option("red").Option("red");
            act.Should().Throw<InvalidOperationException>().WithMessage("*already has option 'red'*");
        }

        [TestMethod]
        public void Choice_DuplicateOption_IsOrdinal()
        {
            var built = FormElicitationBuilder.Create("m").Choice("color").Option("red").Option("Red").Build();
            built.RequestedSchema!.Properties.Should().ContainKey("color");
        }

        [TestMethod]
        public void Option_BlankValue_ThrowsArgumentException()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Option("  ");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Option_BlankTitle_ThrowsArgumentException()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Option("red", "  ");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Choice_DefaultNotInOptions_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Option("red").Default("blue").Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*default 'blue' is not in the option list*");
        }

        [TestMethod]
        public void MultiChoice_DefaultNotInOptions_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m")
                .MultiChoice("roles").Option("admin").Default("user").Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*default 'user' is not in the option list*");
        }

        [TestMethod]
        public void Choice_DefaultBlank_ThrowsArgumentException()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("color").Default("");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void MinLength_GreaterThanMaxLength_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m").String("n").MinLength(5).MaxLength(2).Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*minLength 5 greater than maxLength 2*");
        }

        [TestMethod]
        public void Min_GreaterThanMax_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m").Integer("n").Min(10).Max(1).Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*minimum 10 greater than maximum 1*");
        }

        [TestMethod]
        public void Between_MinGreaterThanMax_ThrowsImmediately()
        {
            var act = () => FormElicitationBuilder.Create("m").Number("n").Between(5, 1);
            act.Should().Throw<ArgumentException>().WithParameterName("maximum");
        }

        [TestMethod]
        public void MinItems_GreaterThanMaxItems_ThrowsAtBuild()
        {
            var act = () => FormElicitationBuilder.Create("m")
                .MultiChoice("roles").Option("a").MinItems(3).MaxItems(1).Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*minItems 3 greater than maxItems 1*");
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void MinLength_Negative_Throws(int length)
        {
            var act = () => FormElicitationBuilder.Create("m").String("n").MinLength(length);
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("length");
        }

        [TestMethod]
        public void MaxLength_Negative_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").String("n").MaxLength(-1);
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("length");
        }

        [TestMethod]
        public void MinItems_Negative_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").MinItems(-1);
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
        }

        [TestMethod]
        public void MaxItems_Negative_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").MaxItems(-1);
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
        }

        [TestMethod]
        public void MultiChoice_DefaultEmptyParams_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").Default();
            act.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [TestMethod]
        public void MultiChoice_DefaultEmptyEnumerable_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").Default(Array.Empty<string>());
            act.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [TestMethod]
        public void MultiChoice_DefaultNullEnumerable_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").Default((IEnumerable<string>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("values");
        }

        [TestMethod]
        public void MultiChoice_DefaultNullParams_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").Default((string[])null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("values");
        }

        [TestMethod]
        public void String_DefaultNull_Throws()
        {
            var act = () => FormElicitationBuilder.Create("m").String("n").Default(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("value");
        }

        [TestMethod]
        public void Title_Null_ThrowsOnEveryFacade()
        {
            FormElicitationBuilder.Create("m").String("n").Invoking(f => f.Title(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("title");
            FormElicitationBuilder.Create("m").Integer("n").Invoking(f => f.Title(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("title");
            FormElicitationBuilder.Create("m").Boolean("n").Invoking(f => f.Title(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("title");
            FormElicitationBuilder.Create("m").Choice("n").Invoking(f => f.Title(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("title");
            FormElicitationBuilder.Create("m").MultiChoice("n").Invoking(f => f.Title(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("title");
        }

        [TestMethod]
        public void Description_Null_ThrowsOnEveryFacade()
        {
            FormElicitationBuilder.Create("m").String("n").Invoking(f => f.Description(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("description");
            FormElicitationBuilder.Create("m").Integer("n").Invoking(f => f.Description(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("description");
            FormElicitationBuilder.Create("m").Boolean("n").Invoking(f => f.Description(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("description");
            FormElicitationBuilder.Create("m").Choice("n").Invoking(f => f.Description(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("description");
            FormElicitationBuilder.Create("m").MultiChoice("n").Invoking(f => f.Description(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("description");
        }

        [TestMethod]
        public void MultiChoice_DefaultEmptyNonListEnumerable_Throws()
        {
            IEnumerable<string> empty = EmptyStrings();
            var act = () => FormElicitationBuilder.Create("m").MultiChoice("n").Default(empty);
            act.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [TestMethod]
        public void ToInputRequest_PropagatesBuildFailure()
        {
            var act = () => FormElicitationBuilder.Create("m").Choice("c").ToInputRequest();
            act.Should().Throw<InvalidOperationException>().WithMessage("*no options*");
        }

        #endregion

        #region Private Methods

        private static IEnumerable<string> EmptyStrings()
        {
            yield break;
        }

        #endregion

    }

}

using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Direct coverage for convention helpers, including unmappable and enumerable-of-enum paths.
    /// </summary>
    [TestClass]
    public class FormElicitationConventionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Apply_NullBuilder_Throws()
        {
            var act = () => FormElicitationConventions.Apply<EmptyForm>(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void TryCreateField_NullProperty_Throws()
        {
            var act = () => FormElicitationConventions.TryCreateField(null!, new NullabilityInfoContext(), out _);
            act.Should().Throw<ArgumentNullException>().WithParameterName("property");
        }

        [TestMethod]
        public void TryCreateField_NullContext_Throws()
        {
            var property = typeof(KitchenSinkForm).GetProperty(nameof(KitchenSinkForm.Name))!;
            var act = () => FormElicitationConventions.TryCreateField(property, null!, out _);
            act.Should().Throw<ArgumentNullException>().WithParameterName("context");
        }

        [TestMethod]
        public void TryCreateField_Unmappable_ReturnsFalse()
        {
            var property = typeof(UnmappableForm).GetProperty(nameof(UnmappableForm.Home))!;
            FormElicitationConventions.TryCreateField(property, new NullabilityInfoContext(), out var field)
                .Should().BeFalse();
            field.Should().BeNull();
        }

        [TestMethod]
        public void TryMapKind_StringIsNotEnumerableOfChar()
        {
            FormElicitationConventions.TryMapKind(typeof(string), out var kind, out var format, out var enumType)
                .Should().BeTrue();
            kind.Should().Be(ElicitationFieldKind.String);
            format.Should().BeNull();
            enumType.Should().BeNull();
        }

        [TestMethod]
        public void TryMapKind_Unsupported_ReturnsFalse()
        {
            FormElicitationConventions.TryMapKind(typeof(TimeSpan), out _, out _, out _).Should().BeFalse();
            FormElicitationConventions.TryMapKind(typeof(object), out _, out _, out _).Should().BeFalse();
            FormElicitationConventions.TryMapKind(typeof(int[]), out _, out _, out _).Should().BeFalse();
            FormElicitationConventions.TryMapKind(typeof(List<string>), out _, out _, out _).Should().BeFalse();
        }

        [TestMethod]
        public void TryGetEnumEnumerable_String_IsFalse()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(string), out var enumType).Should().BeFalse();
            enumType.Should().BeNull();
        }

        [TestMethod]
        public void TryGetEnumEnumerable_ArrayOfEnum_IsTrue()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(ColorChoice[]), out var enumType).Should().BeTrue();
            enumType.Should().Be(typeof(ColorChoice));
        }

        [TestMethod]
        public void TryGetEnumEnumerable_ArrayOfNonEnum_IsFalse()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(int[]), out _).Should().BeFalse();
            FormElicitationConventions.TryGetEnumEnumerable(typeof(string[]), out _).Should().BeFalse();
        }

        [TestMethod]
        public void TryGetEnumEnumerable_ListOfEnum_IsTrue()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(List<ColorChoice>), out var enumType).Should().BeTrue();
            enumType.Should().Be(typeof(ColorChoice));
        }

        [TestMethod]
        public void TryGetEnumEnumerable_IEnumerableOfEnum_IsTrue()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(IEnumerable<ColorChoice>), out var enumType).Should().BeTrue();
            enumType.Should().Be(typeof(ColorChoice));
        }

        [TestMethod]
        public void TryGetEnumEnumerable_IEnumerableOfInt_IsFalse()
        {
            FormElicitationConventions.TryGetEnumEnumerable(typeof(IEnumerable<int>), out _).Should().BeFalse();
        }

        [TestMethod]
        public void EnumerateEnumerableArguments_GenericInterface()
        {
            FormElicitationConventions.EnumerateEnumerableArguments(typeof(IEnumerable<ColorChoice>))
                .Should().Equal(typeof(ColorChoice));
        }

        [TestMethod]
        public void EnumerateEnumerableArguments_ListWalksInterfaces()
        {
            FormElicitationConventions.EnumerateEnumerableArguments(typeof(List<ColorChoice>))
                .Should().Contain(typeof(ColorChoice));
        }

        [TestMethod]
        public void EnumerateEnumerableArguments_NonEnumerable_IsEmpty()
        {
            FormElicitationConventions.EnumerateEnumerableArguments(typeof(int)).Should().BeEmpty();
        }

        [TestMethod]
        public void TryMapKind_FlagsEnum_IsMultiChoice()
        {
            FormElicitationConventions.TryMapKind(typeof(FileAccessFlags), out var kind, out _, out var enumType)
                .Should().BeTrue();
            kind.Should().Be(ElicitationFieldKind.MultiChoice);
            enumType.Should().Be(typeof(FileAccessFlags));
        }

        [TestMethod]
        public void TryMapKind_PlainEnum_IsChoice()
        {
            FormElicitationConventions.TryMapKind(typeof(ColorChoice), out var kind, out _, out var enumType)
                .Should().BeTrue();
            kind.Should().Be(ElicitationFieldKind.Choice);
            enumType.Should().Be(typeof(ColorChoice));
        }

        #endregion

    }

}

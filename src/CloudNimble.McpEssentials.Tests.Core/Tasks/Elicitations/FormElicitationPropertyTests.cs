using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for expression resolution and camelCase wire names.
    /// </summary>
    [TestClass]
    public class FormElicitationPropertyTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void From_SimpleProperty_ReturnsPropertyInfo()
        {
            Expression<Func<KitchenSinkForm, string>> expr = x => x.Name;
            var property = FormElicitationProperty.From(expr);
            property.Name.Should().Be("Name");
        }

        [TestMethod]
        public void From_BoxedValueType_UnwrapsConvert()
        {
            Expression<Func<KitchenSinkForm, object>> expr = x => x.Age;
            var property = FormElicitationProperty.From(expr);
            property.Name.Should().Be("Age");
        }

        [TestMethod]
        public void From_ConvertChecked_Unwraps()
        {
            var parameter = Expression.Parameter(typeof(KitchenSinkForm), "x");
            var access = Expression.Property(parameter, nameof(KitchenSinkForm.Age));
            var convert = Expression.ConvertChecked(access, typeof(object));
            var expr = Expression.Lambda<Func<KitchenSinkForm, object>>(convert, parameter);

            FormElicitationProperty.From(expr).Name.Should().Be("Age");
        }

        [TestMethod]
        public void From_NestedProperty_ThrowsArgumentException()
        {
            Expression<Func<UnmappableForm, string>> expr = x => x.Home.City;
            var act = () => FormElicitationProperty.From(expr);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("expression")
                .WithMessage("*simple property access*");
        }

        [TestMethod]
        public void From_MethodCall_ThrowsArgumentException()
        {
            Expression<Func<KitchenSinkForm, string>> expr = x => x.Name.Trim();
            var act = () => FormElicitationProperty.From(expr);
            act.Should().Throw<ArgumentException>().WithParameterName("expression");
        }

        [TestMethod]
        public void From_Field_ThrowsArgumentException()
        {
            Expression<Func<FieldForm, string>> expr = x => x.Name;
            var act = () => FormElicitationProperty.From(expr);
            act.Should().Throw<ArgumentException>().WithParameterName("expression");
        }

        [TestMethod]
        public void From_Null_ThrowsArgumentNullException()
        {
            var act = () => FormElicitationProperty.From<KitchenSinkForm, string>(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("expression");
        }

        [TestMethod]
        public void From_Constant_ThrowsArgumentException()
        {
            Expression<Func<KitchenSinkForm, string>> expr = x => "nope";
            var act = () => FormElicitationProperty.From(expr);
            act.Should().Throw<ArgumentException>().WithParameterName("expression");
        }

        [TestMethod]
        public void WireName_CamelCases()
        {
            FormElicitationProperty.WireName("Email").Should().Be("email");
            FormElicitationProperty.WireName("TimeToLive").Should().Be("timeToLive");
            FormElicitationProperty.WireName("alreadyCamel").Should().Be("alreadyCamel");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void WireName_Blank_ThrowsArgumentException(string? name)
        {
            var act = () => FormElicitationProperty.WireName(name!);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Exclude_NestedExpression_ThrowsBeforeRemove()
        {
            var act = () => FormElicitation.From<UnmappableForm>("m").Exclude(x => x.Home.City);
            act.Should().Throw<ArgumentException>().WithParameterName("expression");
        }

        #endregion

    }

}

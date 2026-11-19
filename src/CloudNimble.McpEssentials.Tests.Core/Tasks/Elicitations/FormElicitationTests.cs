using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for the <see cref="FormElicitationBuilder"/> factory entry points.
    /// </summary>
    [TestClass]
    public class FormElicitationTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Create_ReturnsEmptyUntypedBuilder()
        {
            var builder = FormElicitationBuilder.Create("Pick a color");

            builder.Should().BeOfType<FormElicitationBuilder>();
            builder.Should().NotBeOfType<FormElicitationBuilder<KitchenSinkForm>>();
            builder.Message.Should().Be("Pick a color");
            builder.Fields.Should().BeEmpty();
        }

        [TestMethod]
        public void Create_NullMessage_ThrowsArgumentNullException()
        {
            var act = () => FormElicitationBuilder.Create(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("message");
        }

        [TestMethod]
        public void Create_EmptyMessage_IsAllowedUntilBuild()
        {
            var builder = FormElicitationBuilder.Create("");

            builder.Message.Should().BeEmpty();
            var act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        public void From_SeedsTypedBuilder()
        {
            var builder = FormElicitationBuilder.From<EmptyForm>("Confirm?");

            builder.Should().BeOfType<FormElicitationBuilder<EmptyForm>>();
            builder.Message.Should().Be("Confirm?");
            builder.Fields.Should().BeEmpty();
        }

        [TestMethod]
        public void From_NullMessage_ThrowsArgumentNullException()
        {
            var act = () => FormElicitationBuilder.From<EmptyForm>(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("message");
        }

        [TestMethod]
        public void TypedBuilder_IsSiblingOfUntypedBuilder()
        {
            var typed = FormElicitationBuilder.From<EmptyForm>("Confirm?");

            typed.Should().BeAssignableTo<FormElicitationBuilderBase>();
            typed.Should().BeAssignableTo<IFormElicitationSource<EmptyForm>>();
            typed.Should().NotBeAssignableTo<FormElicitationBuilder>();
        }

        [TestMethod]
        public void TypedFrom_OnGenericBuilder_SeedsTheSameWay()
        {
            var builder = FormElicitationBuilder<EmptyForm>.From("Confirm?");

            builder.Should().BeOfType<FormElicitationBuilder<EmptyForm>>();
            builder.Message.Should().Be("Confirm?");
        }

        #endregion

    }

}

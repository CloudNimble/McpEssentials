using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for the <see cref="FormElicitation"/> factory entry points.
    /// </summary>
    [TestClass]
    public class FormElicitationTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Create_ReturnsEmptyUntypedBuilder()
        {
            var builder = FormElicitation.Create("Pick a color");

            builder.Should().BeOfType<FormElicitationBuilder>();
            builder.Should().NotBeOfType<FormElicitationBuilder<KitchenSinkForm>>();
            builder.Message.Should().Be("Pick a color");
            builder.Fields.Should().BeEmpty();
        }

        [TestMethod]
        public void Create_NullMessage_ThrowsArgumentNullException()
        {
            var act = () => FormElicitation.Create(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("message");
        }

        [TestMethod]
        public void Create_EmptyMessage_IsAllowedUntilBuild()
        {
            var builder = FormElicitation.Create("");

            builder.Message.Should().BeEmpty();
            var act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*message cannot be empty*");
        }

        [TestMethod]
        public void From_SeedsTypedBuilder()
        {
            var builder = FormElicitation.From<EmptyForm>("Confirm?");

            builder.Should().BeOfType<FormElicitationBuilder<EmptyForm>>();
            builder.Message.Should().Be("Confirm?");
            builder.Fields.Should().BeEmpty();
        }

        [TestMethod]
        public void From_NullMessage_ThrowsArgumentNullException()
        {
            var act = () => FormElicitation.From<EmptyForm>(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("message");
        }

        #endregion

    }

}

using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for sealing the field list after Build and caching XML / InputRequest JSON.
    /// </summary>
    [TestClass]
    public class FormElicitationBuilderSealTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Build_CachesAndSeals()
        {
            FormElicitationBuilder form = FormElicitation.Create("Ship this order");
            form.String("trackingNumber").Required();

            var first = form.Build();
            var second = form.Build();

            second.Should().BeSameAs(first);
            form.IsSealed.Should().BeTrue();
        }

        [TestMethod]
        public void ToXml_CachesIsland()
        {
            FormElicitationBuilder form = FormElicitation.Create("Ship this order");
            form.String("trackingNumber").Required();

            var first = form.ToXml();
            var second = form.ToXml();

            second.Should().BeSameAs(first);
            first.Should().Contain("<Form");
            form.IsSealed.Should().BeTrue();
        }

        [TestMethod]
        public void ToJson_ReturnsInputRequestParams()
        {
            var form = FormElicitation.Create("Ship this order").String("trackingNumber").Required();
            var request = form.ToInputRequest();

            form.ToJson().Should().Be(request.Params);
            form.ToJson().Should().Be(request.Params);
        }

        [TestMethod]
        public void String_AfterBuild_Throws()
        {
            var form = FormElicitation.Create("m").String("a");
            form.Build();

            var act = () => form.Integer("b");
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been built*");
        }

        [TestMethod]
        public void Exclude_AfterToXml_Throws()
        {
            var form = FormElicitation.From<BaseSignupForm>("m");
            form.ToXml();

            var act = () => form.Exclude(x => x.Email);
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been built*");
        }

        [TestMethod]
        public void Build_WhenInvalid_DoesNotSeal()
        {
            FormElicitationBuilder form = FormElicitation.Create("m");
            form.Choice("color");

            var act = () => form.Build();
            act.Should().Throw<InvalidOperationException>().WithMessage("*no options*");
            form.IsSealed.Should().BeFalse();

            form.Choice("color").Option("red");
            form.Build().RequestedSchema!.Properties.Should().ContainKey("color");
        }

        [TestMethod]
        public void Facade_ToJson_MatchesBuilder()
        {
            FormElicitation.Create("m").String("notes").ToJson().Should().NotBeNull();
            FormElicitation.Create("m").Boolean("ok").ToJson().Should().NotBeNull();
            FormElicitation.Create("m").Integer("n").ToJson().Should().NotBeNull();
            FormElicitation.Create("m").Choice("c").Option("a").ToJson().Should().NotBeNull();
            FormElicitation.Create("m").MultiChoice("m1").Option("a").ToJson().Should().NotBeNull();
            FormElicitation.From<BaseSignupForm>("m").String(x => x.Email).ToJson().Should().NotBeNull();
            FormElicitation.From<BaseSignupForm>("m").Boolean("ok").ToJson().Should().NotBeNull();
            FormElicitation.From<BaseSignupForm>("m").Integer("n").ToJson().Should().NotBeNull();
            FormElicitation.From<BaseSignupForm>("m").Choice("c").Option("a").ToJson().Should().NotBeNull();
            FormElicitation.From<BaseSignupForm>("m").MultiChoice("m1").Option("a").ToJson().Should().NotBeNull();
        }

        [TestMethod]
        public void Name_AfterBuild_Throws()
        {
            FormElicitationBuilder form = FormElicitation.Create("m");
            form.Build();

            var act = () => form.Name = "ship";
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been built*");
        }

        [TestMethod]
        public void ToDefinition_AndToInputRequest_Cache()
        {
            FormElicitationBuilder form = FormElicitation.Create("m");
            form.String("a");

            form.ToDefinition().Should().BeSameAs(form.ToDefinition());
            form.ToInputRequest().Should().BeSameAs(form.ToInputRequest());
        }

        #endregion

    }

}

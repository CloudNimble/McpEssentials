using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Extensions
{

    /// <summary>
    /// Breakdance host coverage for <c>AddFormElicitation</c>.
    /// </summary>
    [TestClass]
    public class FormElicitationDITests : BreakdanceMSTestBase
    {

        #region Fields

        private FormElicitationBuilder _form = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            _form = FormElicitationBuilder.Create("Ship this order");
            _form.String("trackingNumber").Required();
            TestHostBuilder.ConfigureServices(services => services.AddFormElicitation("ship", _form));
            TestSetup();
        }

        [TestCleanup]
        public void TearDown()
        {
            TestTearDown();
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void DoesNotOverwriteAnExistingName()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            form.Name = "dispatch";
            RebuildHost(services => services.AddFormElicitation("ship", form));

            form.Name.Should().Be("dispatch");
            GetKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
        }

        [TestMethod]
        public void DoesNotStampASealedForm()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            form.String("trackingNumber").Required();
            _ = form.Build();
            RebuildHost(services => services.AddFormElicitation("ship", form));

            form.Name.Should().BeNull();
            GetKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
        }

        [TestMethod]
        public void IgnoresUnkeyedAndNonStringKeys()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            RebuildHost(services =>
            {
                services.AddSingleton<IFormElicitationSource>(form);
                services.AddKeyedSingleton<IFormElicitationSource>(42, form);
                services.AddFormElicitation("ship", form);
            });

            GetKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
        }

        [TestMethod]
        public void ResolvesTheKeyedSingleton()
        {
            var resolved = GetKeyedService<IFormElicitationSource>("ship");
            resolved.Should().BeSameAs(_form);
            resolved!.Build().Message.Should().Be("Ship this order");
        }

        [TestMethod]
        public void SameInstance_CanRegisterUnderTwoNames()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            RebuildHost(services =>
            {
                services.AddFormElicitation("ship", form);
                services.AddFormElicitation("dispatch", form);
            });

            GetKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(form);
            GetKeyedService<IFormElicitationSource>("dispatch").Should().BeSameAs(form);
        }

        [TestMethod]
        public void StampsNameThroughAFieldFacade()
        {
            var form = FormElicitationBuilder.Create("Ship this order");
            var facade = form.String("trackingNumber").Required();
            RebuildHost(services => services.AddFormElicitation("ship", facade));

            form.Name.Should().Be("ship");
            facade.ToXml().Should().Contain("Name=\"ship\"");
            GetKeyedService<IFormElicitationSource>("ship").Should().BeSameAs(facade);
        }

        [TestMethod]
        public void StampsNameWhenEmpty()
        {
            _form.Name.Should().Be("ship");
            _form.ToXml().Should().Contain("Name=\"ship\"");
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Rebuilds the Breakdance host with a different service graph.
        /// </summary>
        /// <param name="configure">The replacement service configuration.</param>
        internal void RebuildHost(Action<IServiceCollection> configure)
        {
            ResetTestHostBuilder();
            TestHostBuilder.ConfigureServices(configure);
            TestSetup();
        }

        #endregion

    }

}

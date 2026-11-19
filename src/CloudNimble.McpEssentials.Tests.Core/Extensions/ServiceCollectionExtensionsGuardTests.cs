using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Claims;

namespace CloudNimble.McpEssentials.Tests.Core.Extensions
{

    /// <summary>
    /// Argument-guard coverage for Core DI extensions. These fail before a host exists.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsGuardTests
    {

        #region Public Methods

        [TestMethod]
        public void AddFormElicitation_BlankName_Throws()
        {
            var act = () => new ServiceCollection().AddFormElicitation("  ", FormElicitationBuilder.Create("m"));
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        public void AddFormElicitation_DuplicateName_Throws()
        {
            var services = new ServiceCollection();
            services.AddFormElicitation("ship", FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required());
            var act = () => services.AddFormElicitation("ship", FormElicitationBuilder.Create("Dispatch").String("route").Required());
            act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
        }

        [TestMethod]
        public void AddFormElicitation_DuplicateName_AfterOtherDescriptors_Throws()
        {
            var form = FormElicitationBuilder.Create("Ship this order").String("trackingNumber").Required();
            var services = new ServiceCollection();
            services.AddSingleton(form);
            services.AddKeyedSingleton<IFormElicitationSource>("dispatch", form);
            services.AddFormElicitation("ship", form);
            var act = () => services.AddFormElicitation("ship", FormElicitationBuilder.Create("other"));
            act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
        }

        [TestMethod]
        public void AddFormElicitation_NullForm_Throws()
        {
            var act = () => new ServiceCollection().AddFormElicitation("ship", null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("form");
        }

        [TestMethod]
        public void AddFormElicitation_NullServices_Throws()
        {
            IServiceCollection services = null!;
            var act = () => services.AddFormElicitation("ship", FormElicitationBuilder.Create("m"));
            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_NullGetCurrentUserId_Throws()
        {
            var act = () => new ServiceCollection().AddInMemoryMcpTaskStorePlus((Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_NullGetPrincipal_Throws()
        {
            var act = () => new ServiceCollection().AddInMemoryMcpTaskStorePlus((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_NullServices_Throws()
        {
            IServiceCollection services = null!;
            var act = () => services.AddInMemoryMcpTaskStorePlus("alice");
            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [TestMethod]
        public void AddInMemoryMcpTaskStorePlus_WhitespaceUserId_Throws()
        {
            var act = () => new ServiceCollection().AddInMemoryMcpTaskStorePlus("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [TestMethod]
        public void WithTasksPlus_NullBuilder_Throws()
        {
            IMcpServerBuilder builder = null!;
            var act = () => builder.WithTasksPlus("alice");
            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithTasksPlus_WhenConfigureDoesNotSelectStore_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithTasksPlus(_ => { });
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void WithTasksPlus_NullGetCurrentUserId_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithTasksPlus((Func<string?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getCurrentUserId");
        }

        [TestMethod]
        public void WithTasksPlus_NullGetPrincipal_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithTasksPlus((Func<ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithTasksPlus_NullServiceProviderFactory_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithTasksPlus((Func<IServiceProvider, ClaimsPrincipal?>)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("getPrincipal");
        }

        [TestMethod]
        public void WithTasksPlus_NullBuilder_ServiceProviderFactory_Throws()
        {
            IMcpServerBuilder builder = null!;
            var act = () => builder.WithTasksPlus((Func<IServiceProvider, ClaimsPrincipal?>)(_ => null));
            act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
        }

        [TestMethod]
        public void WithTasksPlus_WhitespaceUserId_Throws()
        {
            var act = () => new ServiceCollection().AddMcpServer().WithTasksPlus("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        #endregion

    }

}

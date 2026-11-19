using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Constructor guards for <see cref="McpElicitUrlAttribute"/>.
    /// </summary>
    [TestClass]
    public class McpElicitUrlAttributeTests
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_BlankPropertyName_Throws()
        {
            var act = () => new McpElicitUrlAttribute("connectUri", "  ");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Ctor_Empty_StoresEmptyNames()
        {
            var attribute = new McpElicitUrlAttribute();
            attribute.PropertyNames.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_NullPropertyNames_Throws()
        {
            var act = () => new McpElicitUrlAttribute(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("propertyNames");
        }

        [TestMethod]
        public void Ctor_StoresPropertyNames()
        {
            var attribute = new McpElicitUrlAttribute("connectUri", "authorizationUrl");
            attribute.PropertyNames.Should().Equal("connectUri", "authorizationUrl");
        }

        #endregion

    }

}

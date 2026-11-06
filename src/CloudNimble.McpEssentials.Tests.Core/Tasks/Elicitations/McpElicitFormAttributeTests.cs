using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Constructor guards for <see cref="McpElicitFormAttribute"/>.
    /// </summary>
    [TestClass]
    public class McpElicitFormAttributeTests
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_BlankName_Throws()
        {
            var act = () => new McpElicitFormAttribute("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        public void Ctor_StoresName()
        {
            var attribute = new McpElicitFormAttribute("ship");
            attribute.Name.Should().Be("ship");
        }

        #endregion

    }

}

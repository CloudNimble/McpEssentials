using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Constructor guards for <see cref="FormElicitationAttribute"/>.
    /// </summary>
    [TestClass]
    public class FormElicitationAttributeTests
    {

        #region Public Methods

        [TestMethod]
        public void Ctor_BlankName_Throws()
        {
            var act = () => new FormElicitationAttribute("  ");
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [TestMethod]
        public void Ctor_StoresName()
        {
            var attribute = new FormElicitationAttribute("ship");
            attribute.Name.Should().Be("ship");
        }

        #endregion

    }

}

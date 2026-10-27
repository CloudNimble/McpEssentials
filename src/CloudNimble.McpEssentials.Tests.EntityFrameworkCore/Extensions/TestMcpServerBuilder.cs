using Microsoft.Extensions.DependencyInjection;

namespace CloudNimble.McpEssentials.Tests.EntityFrameworkCore.Extensions
{

    /// <summary>
    /// Real <see cref="IMcpServerBuilder"/> used by Breakdance tests.
    /// </summary>
    internal class TestMcpServerBuilder : IMcpServerBuilder
    {

        #region Properties

        public IServiceCollection Services { get; } = new ServiceCollection();

        #endregion

    }

}

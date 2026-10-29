using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Enum sequences that map to multi-choice through array and <c>IEnumerable&lt;T&gt;</c> paths.
    /// </summary>
    public class EnumCollectionsForm
    {

        public ColorChoice[] Array { get; set; } = [];

        public List<ColorChoice> List { get; set; } = [];

        public IEnumerable<ColorChoice> Sequence { get; set; } = [];

        public IList<ColorChoice> IList { get; set; } = [];

        public HashSet<ColorChoice> Set { get; set; } = [];

    }

}

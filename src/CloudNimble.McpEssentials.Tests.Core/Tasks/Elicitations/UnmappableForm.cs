using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Mix of one mappable property and shapes the conventions skip.
    /// </summary>
    public class UnmappableForm
    {

        public string Name { get; set; } = "";

        public Address Home { get; set; } = new();

        public Dictionary<string, string> Meta { get; set; } = [];

        public object? Bag { get; set; }

        public TimeSpan Duration { get; set; }

        public byte[] Blob { get; set; } = [];

        public string[] Tags { get; set; } = [];

        public int[] Numbers { get; set; } = [];

        public List<string> Names { get; set; } = [];

    }

}

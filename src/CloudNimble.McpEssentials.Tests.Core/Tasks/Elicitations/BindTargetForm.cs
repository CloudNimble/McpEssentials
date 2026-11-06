using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Small DTO used to prove typed <c>ElicitResult&lt;T&gt;.Content</c>.
    /// </summary>
    public class BindTargetForm
    {

        public string Name { get; set; } = "";

        public string? Nickname { get; set; }

        public int Age { get; set; }

        public bool Active { get; set; }

        public ColorChoice Color { get; set; }

        public List<ColorChoice> Colors { get; set; } = [];

        public Guid Id { get; set; }

    }

}

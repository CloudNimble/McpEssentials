using System;
using System.Collections.Generic;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Every CLR shape the form conventions map, plus a nullable string.
    /// </summary>
    public class KitchenSinkForm
    {

        public string Name { get; set; } = "";

        public string? Nickname { get; set; }

        public Guid Id { get; set; }

        public char Initial { get; set; }

        public Uri Homepage { get; set; } = null!;

        public DateOnly Birthday { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public bool Active { get; set; }

        public byte ByteValue { get; set; }

        public sbyte SByteValue { get; set; }

        public short ShortValue { get; set; }

        public ushort UShortValue { get; set; }

        public int Age { get; set; }

        public uint UIntValue { get; set; }

        public long LongValue { get; set; }

        public ulong ULongValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public decimal Price { get; set; }

        public ColorChoice Color { get; set; }

        public FileAccessFlags Access { get; set; }

        public ColorChoice[] Colors { get; set; } = [];

        public List<ColorChoice> ColorList { get; set; } = [];

        public IEnumerable<ColorChoice> ColorSeq { get; set; } = [];

    }

}

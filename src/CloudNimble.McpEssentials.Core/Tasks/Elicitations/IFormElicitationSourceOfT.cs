using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.McpEssentials.Core.Tasks.Elicitations
{

    /// <summary>
    /// A form whose fields were seeded from <typeparamref name="T"/>, so
    /// <c>RequireForm&lt;T&gt;</c> can infer the deserialize target.
    /// </summary>
    /// <typeparam name="T">The form type.</typeparam>
    public interface IFormElicitationSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IFormElicitationSource
    {
    }

}

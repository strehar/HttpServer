#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore
{
	public interface IIndexable
	{
		object this[object key] { get; }
		bool ContainsKey(object key);
	}
}
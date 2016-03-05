using System;

namespace DotLiquidCore
{
	/// <summary>
	/// Specifies the type is safe to be rendered by DotLiquidCore.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class LiquidTypeAttribute : Attribute
	{
		/// <summary>
		/// An array of property and method names that are allowed to be called on the object.
		/// </summary>
		public string[] AllowedMembers { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="allowedMembers">An array of property and method names that are allowed to be called on the object.</param>
		public LiquidTypeAttribute(params string[] allowedMembers)
		{
			AllowedMembers = allowedMembers;
		}
	}
}
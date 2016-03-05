#region Changes
/*
Changed by Miha Strehar in 2016:
cleaned up unnecessary using.
return Attribute.IsDefined(t, typeof(CompilerGeneratedAttribute), false) to return (t.GetTypeInfo().GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null)
t.IsGenericType to t.GetTypeInfo().IsGenericType
t.Attributes to t.GetTypeInfo().Attributes
*/
#endregion

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DotLiquidCore.Util
{
    internal static class TypeUtility
	{
		private const TypeAttributes AnonymousTypeAttributes = TypeAttributes.NotPublic;

        public static bool IsAnonymousType(Type t)
		{
			return (t.GetTypeInfo().GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null)
                && t.GetTypeInfo().IsGenericType
					&& (t.Name.Contains("AnonymousType") || t.Name.Contains("AnonType"))
						&& (t.Name.StartsWith("<>") || t.Name.StartsWith("VB$"))
							&& (t.GetTypeInfo().Attributes & AnonymousTypeAttributes) == AnonymousTypeAttributes;
		}
	}
}
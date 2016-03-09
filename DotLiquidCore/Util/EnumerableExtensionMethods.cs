using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Util
{
    public static class EnumerableExtensionMethods
	{
		public static IEnumerable Flatten(this IEnumerable array)
		{
			foreach (var item in array)
				if (item is string)
					yield return item;
				else if (item is IEnumerable)
					foreach (var subitem in Flatten((IEnumerable) item))
					{
						yield return subitem;
					}
				else
					yield return item;
		}

		public static void EachWithIndex(this IEnumerable<object> array, Action<object, int> callback)
		{
			int index = 0;
			;
			foreach (object item in array)
			{
				callback(item, index);
				++index;
			}
		}
	}
}
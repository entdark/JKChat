using System.Collections.Generic;
using System.Linq;

namespace JKChat.Core {
	public static class CollectionsExtensions {
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
			return enumerable == null || !enumerable.Any();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JKChat.Core.Helpers {
	public static class CollectionsExtensions {
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
			return enumerable == null || !enumerable.Any();
		}

//not the best way to handle multi diff but fine for small data
//compares 2 collections: removes non-existing items, adds missing items, sorts and moves items if necessarily
		public static void MergeWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, T, bool> areItemsTheSame, Func<T, long> keySelector = null) {
			var newItems = items?.ToArray() ?? [];
			var toRemoveIndices = new List<int>(collection.Count);
			var toNotInsertIndices = new HashSet<int>();
			for (int i = 0; i < collection.Count; i++) {
				var oldItem = collection[i];
				bool deleteItem = true;
				for (int j = 0; j < newItems.Length; j++) {
					if (areItemsTheSame(oldItem, newItems[j])) {
						toNotInsertIndices.Add(j);
						deleteItem = false;
						break;
					}
				}
				if (deleteItem) {
					toRemoveIndices.Add(i);
				}
			}
			for (int i = toRemoveIndices.Count-1; i >= 0 ; i--) {
				collection.RemoveAt(toRemoveIndices[i]);
			}
			for (int k = 0; k < newItems.Length; k++) {
				if (!toNotInsertIndices.Contains(k)) {
					collection.Add(newItems[k]);
				}
			}
			if (keySelector != null) {
				var sortedItems = collection.OrderByDescending(keySelector).ToArray();
				for (int i = 0; i < sortedItems.Length; i++) {
					int oldIndex = collection.IndexOf(sortedItems[i]);
					int newIndex = i;
					if (oldIndex != newIndex)
						collection.Move(oldIndex, newIndex);
				}
			}
		}
	}
}
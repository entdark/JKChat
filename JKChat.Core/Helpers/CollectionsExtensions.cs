using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JKChat.Core {
	public static class CollectionsExtensions {
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
			return enumerable == null || !enumerable.Any();
		}

//not the best way to handle multi diff but fine for small data
//compares 2 collections: removes non-existing items, adds missing items, sorts and moves items if necessarily
		public static void MergeWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items, Func<T, T, bool> areItemsTheSame, Func<T, int> keySelector = null) {
			var newItems = items?.ToArray() ?? Array.Empty<T>();
			var toRemoveIndicies = new List<int>(collection.Count);
			var toNotInsertIndicies = new HashSet<int>();
			for (int i = 0; i < collection.Count; i++) {
				var oldItem = collection[i];
				bool deleteItem = true;
				int j = 0;
				foreach (var newItem in newItems) {
					if (areItemsTheSame(oldItem, newItem)) {
						toNotInsertIndicies.Add(j);
						deleteItem = false;
						break;
					}
					j++;
				}
				if (deleteItem) {
					toRemoveIndicies.Add(i);
				}
			}
/*			bool removeALot = toRemoveIndicies.Count > 1;
			bool removeOne = toRemoveIndicies.Count == 1;
			int toAddCount = Math.Abs(toNotInsertIndicies.Count-newItems.Length);
			bool addALot = toAddCount > 1;
			bool addOne = toAddCount == 1;
			if (removeALot || addALot || (removeOne && addOne)) {
				ReplaceWith(items);
				return;
			}*/
			for (int i = toRemoveIndicies.Count-1; i >= 0 ; i--) {
				collection.RemoveAt(toRemoveIndicies[i]);
			}
			int k = 0;
			foreach (var newItem in newItems) {
				if (!toNotInsertIndicies.Contains(k)) {
					collection.Add(newItem);
				}
				k++;
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
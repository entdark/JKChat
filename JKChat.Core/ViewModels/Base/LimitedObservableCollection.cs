using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public class LimitedObservableCollection<T> : MvxObservableCollection<T> {
		private readonly int limit;
		public LimitedObservableCollection(int limit) {
			if (limit < 0) {
				throw new ArgumentOutOfRangeException(nameof(limit));
			}
			this.limit = limit;
		}

		public LimitedObservableCollection(IEnumerable<T> items, int limit) : base(items) {
			if (limit < 0) {
				throw new ArgumentOutOfRangeException(nameof(limit));
			}
			this.limit = limit;
		}

		public new void Add(T item) {
			if (limit == Count) {
				base.RemoveAt(0);
			}
			base.Add(item);
		}

		public new void Insert(int index, T item) {
			if (limit == Count) {
				int removeIndex = index == 0 ? Count-1 : 0;
				base.RemoveAt(removeIndex);
			}
			base.Insert(index, item);
		}

		public override void AddRange(IEnumerable<T> items) {
			if (items is ICollection<T> collection && (Count + collection.Count) > limit) {
				base.RemoveRange(0, Math.Min(Count, (Count + collection.Count) - limit));
			}
			base.AddRange(items);
		}

		public void InsertRange(int index, IEnumerable<T> items, bool silently = false) {
			using (SuppressEvents()) {
				foreach (var item in items) {
					Insert(index, item);
				}
			}
			if (!silently) {
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, 0));
			} else {
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Items));
			}
		}

//not the best way to handle multi diff but fine for small data
//compares 2 collections: removes non-existing items, adds missing items, sorts and moves items if necessarily
		public void ReplaceWith<TKey>(IEnumerable<T> items, Func<T, T, bool> areItemsTheSame, Func<T, TKey> keySelector = null) {
			var newItems = items?.ToArray() ?? Array.Empty<T>();
			var toRemoveIndicies = new List<int>(Count);
			var toNotInsertIndicies = new HashSet<int>();
			for (int i = 0; i < Count; i++) {
				var oldItem = this[i];
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
				RemoveAt(toRemoveIndicies[i]);
			}
			int k = 0;
			foreach (var newItem in newItems) {
				if (!toNotInsertIndicies.Contains(k)) {
					Add(newItem);
				}
				k++;
			}
			if (keySelector != null) {
				var sortedItems = this.OrderByDescending(keySelector).ToArray();
				for (int i = 0; i < sortedItems.Length; i++) {
					int oldIndex = IndexOf(sortedItems[i]);
					int newIndex = i;
					if (oldIndex != newIndex)
						Move(oldIndex, newIndex);
				}
			}
		}
	}
}
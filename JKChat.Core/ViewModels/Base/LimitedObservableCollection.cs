using System;
using System.Collections.Generic;
using System.Collections.Specialized;

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
	}
}
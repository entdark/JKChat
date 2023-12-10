using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Adapters {
	public class TableGroupedRecyclerViewAdapter : BaseRecyclerViewAdapter {
		public IList<TableGroupedItemVM> Items => ItemsSource as IList<TableGroupedItemVM>;

		public ISet<int> HeaderPositions { get; private set; }
		public ISet<int> FooterPositions { get; private set; }

		public override int ItemCount => Items?.Sum(item => item.Items?.Count ?? 0) ?? 0;

		public TableGroupedRecyclerViewAdapter(IMvxAndroidBindingContext bindingContext) : base(bindingContext) {}

		public override object GetItem(int viewPosition) {
			if (Items == null)
				return null;
			for (int i = 0, totalViewCount = 0; i < Items.Count; i++) {
				var groupItem = Items[i];
				int groupItemsCount = groupItem.Items?.Count ?? 0;
				if (totalViewCount+groupItemsCount < viewPosition) {
					totalViewCount+=groupItemsCount;
				} else {
					for (int j = 0; j < groupItemsCount; j++) {
						if (totalViewCount == viewPosition) {
							return groupItem.Items[j];
						}
						totalViewCount++;
					}
				}
			}
			return null;
		}

		protected override void ExecuteCommandOnItem(ICommand command, object itemDataContext) {
			//switch gets executed twice since clicking on the item means toggling the switch, so ignore item click
			if (itemDataContext is TableItemVM { Type: TableItemType.Toggle })
				return;
			base.ExecuteCommandOnItem(command, itemDataContext);
		}

		public override void NotifyDataSetChanged(NotifyCollectionChangedEventArgs ev) {
			if (ev == null) {
				NotifyDataSetChanged();
				return;
			}
			switch (ev.Action) {
			case NotifyCollectionChangedAction.Add:
				if (ev.NewItems != null) {
					NotifyItemRangeInserted(GetViewPosition(ev.NewStartingIndex), GetGroupItemsCount(ev.NewItems));
				}
				break;
			case NotifyCollectionChangedAction.Remove:
				if (ev.OldItems != null) {
					NotifyItemRangeRemoved(GetViewPosition(ev.OldStartingIndex), GetGroupItemsCount(ev.OldItems));
				}
				break;
			default:
				NotifyDataSetChanged();
				break;
			}
		}

		protected override int GetViewPosition(int itemsSourcePosition) {
			if (Items == null && Items.Count <= itemsSourcePosition)
				return itemsSourcePosition;
			int viewPosition = 0;
			for (int i = 0; i < Items.Count; i++) {
				if (i == itemsSourcePosition)
					return viewPosition;
				viewPosition += (Items[i].Items?.Count ?? 0);
			}
			return viewPosition;
		}

		protected virtual int GetGroupItemsCount(int index) {
			if (Items == null && Items.Count <= index)
				return index;
			return Items[index].Items?.Count ?? 0;
		}

		protected virtual int GetGroupItemsCount(IList items) {
			if (items == null && items.Count <= 0)
				return 0;
			int count = 0;
			for (int i = 0; i < items.Count; i++) {
				count += ((items[i] as TableGroupedItemVM)?.Items?.Count ?? 0);
			}
			return count;
		}
	}
}
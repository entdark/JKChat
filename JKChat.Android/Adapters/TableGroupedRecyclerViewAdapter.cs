using System.Collections;
using System.Collections.Generic;

using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Adapters {
	public class TableGroupedRecyclerViewAdapter : BaseRecyclerViewAdapter {
		private IList<TableItemVM> items;

		public override int ItemCount => items?.Count ?? 0;

		public TableGroupedRecyclerViewAdapter(IMvxAndroidBindingContext bindingContext) : base(bindingContext) {}

		public override object GetItem(int viewPosition) {
			if (items == null || items.Count <= 0)
				return null;

			return items[viewPosition];
		}

		protected override void SetItemsSource(IEnumerable value) {
			if (value is IList<TableGroupedItemVM> groupItems) {
				items = new List<TableItemVM>();
				foreach (var groupItem in groupItems) {
					foreach (var item in groupItem.Items) {
						items.Add(item);
					}
				}
			} else {
				items = null;
			}

			base.SetItemsSource(value);
		}
	}
}
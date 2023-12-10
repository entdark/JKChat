using System;

using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.DroidX.RecyclerView.ItemTemplates;

namespace JKChat.Android.TemplateSelectors {
	public class TableGroupedItemTemplateSelector : MvxTemplateSelector<TableItemVM> {
		private const int ToggleViewType = 0;
		private const int ValueViewType = 1;
		private const int NavigationViewType = 2;

		private readonly bool detail;

		public TableGroupedItemTemplateSelector(bool detail) {
			this.detail = detail;
		}

		public override int GetItemLayoutId(int fromViewType) {
			return fromViewType switch {
				ToggleViewType when detail => Resource.Layout.table_toggle_detail_item,
				ValueViewType when detail => Resource.Layout.table_value_detail_item,
				NavigationViewType when detail => Resource.Layout.table_navigation_detail_item,
				ToggleViewType => Resource.Layout.table_toggle_master_item,
				ValueViewType => Resource.Layout.table_value_master_item,
				NavigationViewType => Resource.Layout.table_navigation_master_item,
				_ => throw new Exception("View type is invalid"),
			};
		}

		protected override int SelectItemViewType(TableItemVM forItemObject) {
			return forItemObject.Type switch {
				TableItemType.Toggle => ToggleViewType,
				TableItemType.Value => ValueViewType,
				TableItemType.Navigation => NavigationViewType,
				_ => throw new Exception("Item for view type is invalid"),
			};
		}
	}
}
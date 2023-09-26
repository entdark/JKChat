using System.Collections.Generic;

using Foundation;

using JKChat.Core;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.iOS.ValueConverters;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.ViewSources {
	public class TableGroupedViewSource : MvxStandardTableViewSource {
		public IList<TableGroupedItemVM> Items => ItemsSource as IList<TableGroupedItemVM>;

		public TableGroupedViewSource(UITableView tableView) : base(tableView) {
			tableView.Source = this;
			DeselectAutomatically = true;
		}

		public override nint NumberOfSections(UITableView tableView) {
			return Items?.Count ?? 0;
		}

		public override nint RowsInSection(UITableView tableview, nint section) {
			return Items.IsNullOrEmpty() ? 0 : (Items[(int)section].Items?.Count ?? 0);
		}

		protected override object GetItemAt(NSIndexPath indexPath) {
			return Items[indexPath.Section].Items[indexPath.Row];
		}

		protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item) {
			if (item is TableItemVM tableItem) {
				return tableItem.Type switch {
					TableItemType.Value => new TableValueViewCell(),
					TableItemType.Toggle => new TableToggleViewCell()
				};
			}
			return base.GetOrCreateCellFor(tableView, indexPath, item);
		}

		private abstract class TableBaseViewCell : MvxTableViewCell {
			public TableBaseViewCell(TableItemType type, NSString cellIdentifier) : base(string.Empty, FromType(type), cellIdentifier) {
				this.DelayBind(BindView);
			}

			protected virtual void BindView() {
				using var set = this.CreateBindingSet<TableBaseViewCell, TableItemVM>();
				set.Bind(TextLabel).For(v => v.Text).To(vm => vm.Title);
			}

			private static UITableViewCellStyle FromType(TableItemType type) {
				return type switch {
					TableItemType.Value => UITableViewCellStyle.Value1,
					TableItemType.Toggle => UITableViewCellStyle.Default
				};
			}
		}

		private class TableValueViewCell : TableBaseViewCell {
			public static readonly NSString Key = new(nameof(TableValueViewCell));

			private string detail;
			public string Detail {
				get => detail;
				set {
					detail = value;
					DetailTextLabel.AttributedText = ColourTextValueConverter.Convert(value);
					LayoutSubviews();
				}
			}

			public TableValueViewCell() : base(TableItemType.Value, Key) {}

			protected override void BindView() {
				base.BindView();
				using var set = this.CreateBindingSet<TableValueViewCell, TableValueItemVM>();
				set.Bind(this).For(v => v.Detail).To(vm => vm.Value);
			}
		}

		private class TableToggleViewCell : TableBaseViewCell {
			public static readonly NSString Key = new(nameof(TableToggleViewCell));
			protected UISwitch Switch { get; init; }

			public TableToggleViewCell() : base(TableItemType.Value, Key) {
				Switch = new UISwitch();
				AccessoryView = Switch;
			}

			protected override void BindView() {
				base.BindView();
				using var set = this.CreateBindingSet<TableToggleViewCell, TableToggleItemVM>();
				set.Bind(Switch).For(v => v.On).To(vm => vm.IsChecked);
			}
		}
	}
}
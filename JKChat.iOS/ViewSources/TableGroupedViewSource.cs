using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

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
			UseAnimations = true;
			AddAnimation = UITableViewRowAnimation.Fade;
			RemoveAnimation = UITableViewRowAnimation.Fade;
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
					TableItemType.Toggle => new TableToggleViewCell(),
					TableItemType.Navigation => new TableNavigationViewCell()
				};
			}
			return base.GetOrCreateCellFor(tableView, indexPath, item);
		}

		protected override void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
			if (NSThread.IsMain) {
				action();
			} else {
				InvokeOnMainThread(action);
			}
			void action() {
				if (!UseAnimations) {
					ReloadTableData();
				} else if (!TryDoAnimatedChange(args)) {
					ReloadTableData();
				}
			}
		}
		protected new bool TryDoAnimatedChange(NotifyCollectionChangedEventArgs args) {
			if (args == null) {
				return false;
			}
			switch (args.Action) {
			case NotifyCollectionChangedAction.Add: {
				TableView.InsertSections(NSIndexSet.FromNSRange(new NSRange(args.NewStartingIndex, args.NewItems.Count)), AddAnimation);
				return true;
			}
			case NotifyCollectionChangedAction.Remove: {
				TableView.DeleteSections(NSIndexSet.FromNSRange(new NSRange(args.OldStartingIndex, args.OldItems.Count)), RemoveAnimation);
				return true;
			}
			default:
				return false;
			}
		}

		protected static NSIndexPath []CreateNSIndexPathArray(int startingPosition, IList items) {
			if (items == null || items.Count <= 0)
				return Array.Empty<NSIndexPath>();
			var indexPaths = new List<NSIndexPath>();
			for (int i = 0; i < items.Count; i++) {
				if (items[i] is TableGroupedItemVM { Items: { Count: > 0 } groupedItems }) {
					for (int j = 0; j < groupedItems.Count; j++) {
						indexPaths.Add(NSIndexPath.FromRowSection(j, startingPosition + i));
					}
				}
			}
			return indexPaths.ToArray();
		}

		private abstract class TableBaseViewCell : MvxTableViewCell {
			public TableBaseViewCell(UITableViewCellStyle style, NSString cellIdentifier, UITableViewCellAccessory accessory = UITableViewCellAccessory.None) : base(string.Empty, style, cellIdentifier, accessory) {
				this.DelayBind(BindView);
			}

			protected virtual void BindView() {
				using var set = this.CreateBindingSet<TableBaseViewCell, TableItemVM>();
				set.Bind(TextLabel).For(v => v.Text).To(vm => vm.Title);
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

			public TableValueViewCell() : base(UITableViewCellStyle.Value1, Key) {}

			protected override void BindView() {
				base.BindView();
				using var set = this.CreateBindingSet<TableValueViewCell, TableValueItemVM>();
				set.Bind(this).For(v => v.Detail).To(vm => vm.Value);
			}
		}

		private class TableToggleViewCell : TableBaseViewCell {
			public static readonly NSString Key = new(nameof(TableToggleViewCell));
			protected UISwitch Switch { get; init; }

			public TableToggleViewCell() : base(UITableViewCellStyle.Default, Key) {
				Switch = new UISwitch();
				AccessoryView = Switch;
			}

			protected override void BindView() {
				base.BindView();
				using var set = this.CreateBindingSet<TableToggleViewCell, TableToggleItemVM>();
				set.Bind(Switch).For(v => v.On).To(vm => vm.IsChecked);
			}
		}

		private class TableNavigationViewCell : TableBaseViewCell {
			public static readonly NSString Key = new(nameof(TableNavigationViewCell));

			public TableNavigationViewCell() : base(UITableViewCellStyle.Default, Key, UITableViewCellAccessory.DisclosureIndicator) {}
		}
	}
}
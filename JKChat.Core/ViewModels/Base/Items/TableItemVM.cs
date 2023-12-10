using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Items {
	public abstract class TableItemVM : MvxNotifyPropertyChanged {
		private string title;
		public string Title {
			get => title;
			set => SetProperty(ref title, value);
		}

		public abstract TableItemType Type { get; }

		public IMvxAsyncCommand ClickCommand { get; init; }

		public Func<TableItemVM, Task> OnClick { get; set; }

		public TableItemVM() {
			ClickCommand = new MvxAsyncCommand(ClickExecute);
		}

		protected virtual async Task ClickExecute() {
			if (OnClick != null)
				await OnClick.Invoke(this);
		}
	}

	public class TableValueItemVM : TableItemVM {
		private string val;
		public string Value {
			get => val;
			set => SetProperty(ref val, value);
		}

		public override TableItemType Type => TableItemType.Value;

		public new Func<TableValueItemVM, Task> OnClick {
			get => base.OnClick;
			set => base.OnClick = value != null ? (item) => value.Invoke((TableValueItemVM)item) : null;
		}
	}

	public class TableToggleItemVM : TableItemVM {
		private bool isChecked;
		public bool IsChecked {
			get => isChecked;
			set => SetProperty(ref isChecked, value, () => Toggled?.Invoke(this));
		}

		public override TableItemType Type => TableItemType.Toggle;

		public Action<TableToggleItemVM> Toggled { get; set; }

		public new Func<TableToggleItemVM, Task> OnClick {
			get => base.OnClick;
			set => base.OnClick = value != null ? (item) => value.Invoke((TableToggleItemVM)item) : null;
		}

		protected override Task ClickExecute() {
			IsChecked = !IsChecked;
			return base.ClickExecute();
		}
	}

	public class TableNavigationItemVM : TableItemVM {
		public override TableItemType Type => TableItemType.Navigation;

		public new Func<TableNavigationItemVM, Task> OnClick {
			get => base.OnClick;
			set => base.OnClick = value != null ? (item) => value.Invoke((TableNavigationItemVM)item) : null;
		}
	}

	public class TableGroupedItemVM {
		public string Header { get; set; }
		public List<TableItemVM> Items { get; init; }
		public string Footer { get; set; }

		public TableGroupedItemVM() {
			Items = new(0);
		}
		public TableGroupedItemVM(IEnumerable<TableItemVM> items) {
			Items = new(items);
		}
	}

	public enum TableItemType {
		Value,
		Toggle,
		Navigation
	}
}
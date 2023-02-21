using System.Collections.Generic;

using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Dialog {
	public class DialogListViewModel {
		public List<DialogItemVM> Items { get; init; }
		public IMvxCommand ItemClickCommand { get; init; }

		public DialogListViewModel() {
			Items = new List<DialogItemVM>();
			ItemClickCommand = new MvxCommand<DialogItemVM>(ItemClickExecute);
		}

		private void ItemClickExecute(DialogItemVM selectedItem) {
			foreach (var item in Items) {
				item.IsSelected = item == selectedItem;
			}
		}
	}
}

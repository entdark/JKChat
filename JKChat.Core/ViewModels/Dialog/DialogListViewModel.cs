using System.Collections.Generic;

using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Dialog {

	public class DialogListViewModel {
		public List<DialogItemVM> Items { get; internal set; }
		public IMvxCommand ItemClickCommand { get; private set; }

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

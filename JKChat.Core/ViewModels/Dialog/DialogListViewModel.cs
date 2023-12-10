using System.Collections.Generic;
using System.Linq;

using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Dialog {
	public class DialogListViewModel {
		public List<DialogItemVM> Items { get; init; }
		public IMvxCommand ItemClickCommand { get; init; }
		public DialogSelectionType SelectionType { get; init; } = DialogSelectionType.SingleSelection;
		public bool HasItems => Items.Count > 0;
		public int SelectedIndex => Items.FindIndex(item => item.IsSelected);
		public DialogItemVM SelectedItem => Items.FirstOrDefault(item => item.IsSelected);
		public IEnumerable<DialogItemVM> SelectedItems => Items.Where(item => item.IsSelected);

		public DialogListViewModel() {
			Items = new List<DialogItemVM>();
			ItemClickCommand = new MvxCommand<DialogItemVM>(ItemClickExecute);
		}

		public DialogListViewModel(IEnumerable<DialogItemVM> items, DialogSelectionType selectionType = DialogSelectionType.SingleSelection) {
			Items = new List<DialogItemVM>(items);
			ItemClickCommand = new MvxCommand<DialogItemVM>(ItemClickExecute);
			SelectionType = selectionType;
		}

		private void ItemClickExecute(DialogItemVM selectedItem) {
			foreach (var item in Items) {
				item.IsSelected = item == selectedItem;
			}
		}
	}
	public enum DialogSelectionType {
		NoSelection,
		InstantSelection,
		SingleSelection,
		MultiSelection
	}
}
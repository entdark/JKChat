using JKChat.Core.ViewModels.Base.Items;

namespace JKChat.Core.ViewModels.Dialog.Items {
	public class DialogItemVM : SelectableItemVM {
		public string Name { get; init; }
		public object Id { get; init; }
	}
}
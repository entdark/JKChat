using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Items {
	public abstract class SelectableItemVM : MvxNotifyPropertyChanged, ISelectableItemVM {
		private bool isSelected;
		public virtual bool IsSelected {
			get => isSelected;
			set => SetProperty(ref isSelected, value);
		}
	}
}
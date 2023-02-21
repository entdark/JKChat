using System;

using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Chat.Items {
	public abstract class ChatItemVM : MvxNotifyPropertyChanged, ISelectableItemVM {
		public string Time { get; init; } = DateTime.Now.ToString("t");

		private bool isSelected;
		public bool IsSelected {
			get => isSelected;
			set => SetProperty(ref isSelected, value);
		}

		public Type ThisVMType => this.GetType();

		private Type topVMType;
		public Type TopVMType {
			get => topVMType;
			set => SetProperty(ref topVMType, value);
		}

		private Type bottomVMType;
		public Type BottomVMType {
			get => bottomVMType;
			set => SetProperty(ref bottomVMType, value);
		}
	}
}

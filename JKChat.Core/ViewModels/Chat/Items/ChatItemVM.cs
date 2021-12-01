using System;

using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Chat.Items {
	public abstract class ChatItemVM : MvxNotifyPropertyChanged {
		public string Time { get; private set; } = DateTime.Now.ToString("t");

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

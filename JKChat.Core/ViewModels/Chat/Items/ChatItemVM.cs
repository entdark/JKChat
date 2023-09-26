using System;

using JKChat.Core.ViewModels.Base.Items;

namespace JKChat.Core.ViewModels.Chat.Items {
	public abstract class ChatItemVM : SelectableItemVM {
		public string Time { get; init; } = DateTime.Now.ToString("t");

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

		public double EstimatedHeight { get; set; }
	}
}
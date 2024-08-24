using System;

using JKChat.Core.ViewModels.Base.Items;

namespace JKChat.Core.ViewModels.Chat.Items {
	public abstract class ChatItemVM : SelectableItemVM {
		internal DateTime DateTime { get; init; } = DateTime.Now;

		private string time;
		public string Time => time ??= DateTime.ToString("t");

		public double EstimatedHeight { get; set; }
	}
}
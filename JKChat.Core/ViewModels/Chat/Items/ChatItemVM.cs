using System;
using System.Collections.Generic;
using System.Text;

namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatItemVM {
		public string PlayerName { get; private set; }
		public string Message { get; private set; }
		public ChatItemVM(string playerName, string message) {
			PlayerName = playerName;
			Message = message;
		}
	}
}

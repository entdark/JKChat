namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatMessageItemVM : ChatItemVM {
		public string EscapedPlayerName { get; private set; }
		public string PlayerName { get; private set; }
		public string Message { get; private set; }
		public bool Shadow { get; private set; }
		public ChatMessageItemVM(string escapedPlayerName, string playerName, string message, bool shadow = false) {
			EscapedPlayerName = escapedPlayerName;
			PlayerName = playerName;
			Message = message;
			Shadow = shadow;
		}
	}
}

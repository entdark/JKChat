namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatMessageItemVM : ChatItemVM {
		public string EscapedPlayerName { get; init; }
		public string PlayerName { get; init; }
		public string Message { get; init; }
		public bool Shadow { get; init; }
		public ChatMessageItemVM(string escapedPlayerName, string playerName, string message, bool shadow = false) {
			EscapedPlayerName = escapedPlayerName;
			PlayerName = playerName;
			Message = message;
			Shadow = shadow;
		}
	}
}
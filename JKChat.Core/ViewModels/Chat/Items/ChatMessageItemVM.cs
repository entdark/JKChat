namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatMessageItemVM(string escapedPlayerName, string playerName, string message, bool shadow = false) : ChatItemVM {
		public string EscapedPlayerName { get; init; } = escapedPlayerName;
		public string PlayerName { get; init; } = playerName;
		public string Message { get; init; } = message;
		public bool Shadow { get; init; } = shadow;
	}
}
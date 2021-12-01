namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatMessageItemVM : ChatItemVM {
		public string PlayerName { get; private set; }
		public string Message { get; private set; }
		public ChatMessageItemVM(string playerName, string message) {
			PlayerName = playerName;
			Message = message;
		}
	}
}

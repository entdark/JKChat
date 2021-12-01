namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatInfoItemVM : ChatItemVM {
		public string Text { get; private set; }
		public ChatInfoItemVM(string text) {
			Text = text;
		}
	}
}

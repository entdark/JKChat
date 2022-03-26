namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatInfoItemVM : ChatItemVM {
		public string Text { get; private set; }
		public bool Shadow { get; private set; }
		public ChatInfoItemVM(string text, bool shadow = false) {
			Text = text;
			Shadow = shadow;
		}
	}
}

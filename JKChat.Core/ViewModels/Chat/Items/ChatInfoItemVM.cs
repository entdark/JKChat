namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatInfoItemVM : ChatItemVM {
		public string Text { get; init; }
		public bool Shadow { get; init; }
		public ChatInfoItemVM(string text, bool shadow = false) {
			Text = text;
			Shadow = shadow;
		}
	}
}
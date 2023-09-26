using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class SentMessageMessage : MvxMessage {
		public SentMessageMessage(object sender) : base(sender) {
		}
	}
}
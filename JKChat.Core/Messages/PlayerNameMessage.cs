using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class PlayerNameMessage : MvxMessage {
		public PlayerNameMessage(object sender) : base(sender) {}
	}
}
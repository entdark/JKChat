using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class LocationUpdateMessage : MvxMessage {
		public LocationUpdateMessage(object sender) : base(sender) {
		}
	}
}
using JKClient;

using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class FavouriteMessage : MvxMessage {
		internal ServerInfo ServerInfo { get; init; }
		internal bool IsFavourite { get; init; }
		internal FavouriteMessage(object sender, ServerInfo serverInfo, bool isFavourite) : base(sender) {
			ServerInfo = serverInfo;
			IsFavourite = isFavourite;
		}
	}
}
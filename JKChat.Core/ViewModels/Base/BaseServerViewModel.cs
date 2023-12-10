using JKChat.Core.Messages;

using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public abstract class BaseServerViewModel : BaseViewModel {
		private MvxSubscriptionToken serverInfoMessageToken, favouriteMessageToken;

		public BaseServerViewModel() {
			serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			favouriteMessageToken = Messenger.Subscribe<FavouriteMessage>(OnFavouriteMessage);
		}

		protected virtual void OnServerInfoMessage(ServerInfoMessage message) {}

		protected virtual void OnFavouriteMessage(FavouriteMessage message) {}

		public override void ViewCreated() {
			base.ViewCreated();
			serverInfoMessageToken ??= Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			favouriteMessageToken ??= Messenger.Subscribe<FavouriteMessage>(OnFavouriteMessage);
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (serverInfoMessageToken != null) {
					Messenger.Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
					serverInfoMessageToken = null;
				}
				if (favouriteMessageToken != null) {
					Messenger.Unsubscribe<FavouriteMessage>(favouriteMessageToken);
					favouriteMessageToken = null;
				}
			}
			base.ViewDestroy(viewFinishing);
		}
	}

	public abstract class BaseServerViewModel<TParameter> : BaseServerViewModel, IMvxViewModel<TParameter> {
		public abstract void Prepare(TParameter parameter);
	}
}
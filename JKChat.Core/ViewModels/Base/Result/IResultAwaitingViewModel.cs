using JKChat.Core.Messages;

using MvvmCross;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Result {
	public interface IResultAwaitingViewModel<TResult> : IMvxViewModel {
		private protected MvxSubscriptionToken ResultAwaitingToken { get; set; }

		bool ResultSet(IResultSettingViewModel<TResult> sender, TResult result);

		public void SubscribeToResult() {
			UnsubscribeToResult();

			ResultAwaitingToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<NavigationResultMessage<TResult>>(message => {
				bool set = ResultSet(message.Sender, message.Result);
				if (set)
					UnsubscribeToResult();
			});
		}

		public void UnsubscribeToResult() {
			if (ResultAwaitingToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<NavigationResultMessage<TResult>>(ResultAwaitingToken);
				ResultAwaitingToken = null;
			}
		}
	}
}
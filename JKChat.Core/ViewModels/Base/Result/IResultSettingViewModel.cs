using JKChat.Core.Messages;

using MvvmCross;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Result {
	public interface IResultSettingViewModel<TResult> : IMvxViewModel {
		public void SetResult(TResult result) {
			Mvx.IoCProvider.Resolve<IMvxMessenger>().Publish(new NavigationResultMessage<TResult>(this, result));
		}
	}
}
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Result {
	public abstract class ResultAwaitingViewModel<TResult> : BaseViewModel, IResultAwaitingViewModel<TResult> {
		MvxSubscriptionToken IResultAwaitingViewModel<TResult>.ResultAwaitingToken { get; set; }

		public override void Prepare() {
			base.Prepare();
			(this as IResultAwaitingViewModel<TResult>).SubscribeToResult();
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing)
				(this as IResultAwaitingViewModel<TResult>).UnsubscribeToResult();
			base.ViewDestroy(viewFinishing);
		}

		public abstract bool ResultSet(IResultSettingViewModel<TResult> sender, TResult result);
	}

	public abstract class ResultAwaitingViewModel<TParameter, TResult> : ResultAwaitingViewModel<TResult>, IMvxViewModel<TParameter> {
		public abstract void Prepare(TParameter parameter);
	}
}
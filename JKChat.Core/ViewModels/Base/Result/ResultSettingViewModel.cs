using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Result {
	public abstract class ResultSettingViewModel<TResult> : BaseViewModel, IResultSettingViewModel<TResult> {
	}

	public abstract class ResultSettingViewModel<TParameter, TResult> : ResultSettingViewModel<TResult>, IMvxViewModel<TParameter> {
		public abstract void Prepare(TParameter parameter);
	}
}
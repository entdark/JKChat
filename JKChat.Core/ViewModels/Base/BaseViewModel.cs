using System;
using System.Threading.Tasks;

using JKChat.Core.Navigation;
using JKChat.Core.Services;

using MvvmCross;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public abstract class BaseViewModel : MvxViewModel, IBaseViewModel {
		protected INavigationService NavigationService { get; private set; }
		protected IDialogService DialogService { get; private set; }
		protected IMvxMessenger Messenger { get; private set; }

		private string title;
		public virtual string Title {
			get => title;
			set => SetProperty(ref title, value);
		}

		private bool isLoading;
		public bool IsLoading {
			get => isLoading;
			set => SetProperty(ref isLoading, value);
		}

		public BaseViewModel() {
			NavigationService = Mvx.IoCProvider.Resolve<INavigationService>();
			DialogService = Mvx.IoCProvider.Resolve<IDialogService>();
			Messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
		}

		protected virtual Task ExceptionCallback(Exception exception) {
			return Helpers.Common.ExceptionCallback(exception);
		}
	}

	public abstract class BaseViewModel<TParameter> : BaseViewModel, IMvxViewModel<TParameter> {
		public abstract void Prepare(TParameter parameter);
	}
}

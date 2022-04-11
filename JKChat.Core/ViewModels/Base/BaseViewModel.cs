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

		protected virtual async Task ExceptionCallback(Exception exception) {
			Exception realException;
			if (exception.InnerException is AggregateException aggregateException) {
				realException = aggregateException.InnerExceptions != null ? aggregateException.InnerExceptions[0] : aggregateException;
			} else if (exception.InnerException != null) {
				realException = exception.InnerException;
			} else {
				realException = exception;
			}
			string message = realException.Message + (!string.IsNullOrEmpty(realException.StackTrace) ? ("\n\n" + realException.StackTrace) : string.Empty);

			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				LeftButton = "Copy",
				LeftClick = (_) => {
					Xamarin.Essentials.Clipboard.SetTextAsync(message);
				},
				RightButton = "OK",
				Type = JKDialogType.Title | JKDialogType.Message
			});
		}
	}

	public abstract class BaseViewModel<TParameter> : BaseViewModel, IMvxViewModel<TParameter> {
		public abstract void Prepare(TParameter parameter);
	}
}

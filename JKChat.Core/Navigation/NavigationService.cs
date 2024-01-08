using System.Collections.Generic;
using System.Threading.Tasks;

using JKChat.Core.Navigation.Hints;
using JKChat.Core.ViewModels.Base.Result;

using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using MvvmCross.Views;

namespace JKChat.Core.Navigation {
	public class NavigationService : MvxNavigationService, INavigationService {
		public const string PathKey = nameof(PathKey);
		public const string DataKey = nameof(DataKey);
		public const string FromRootKey = nameof(FromRootKey);

		public bool RootIsInitialized { get; set; }

		public NavigationService(IMvxViewModelLoader viewModelLoader, IMvxViewDispatcher viewDispatcher, IMvxIoCProvider iocProvider) : base(viewModelLoader, viewDispatcher, iocProvider) {
		}

		public async Task<bool> NavigateFromRoot<TViewModel>(object data = null)
			where TViewModel : IMvxViewModel {
			var hint = new PopToRootPresentationHint() {
				ViewModelType = typeof(TViewModel),
				Data = data
			};
			bool result = await ChangePresentation(hint);
			if (hint.PoppedToRoot) {
				result = await Navigate<TViewModel>();
			}
			return result;
		}

		public async Task<bool> NavigateFromRoot<TViewModel, TParameter>(TParameter parameter, object data = null)
			where TViewModel : IMvxViewModel<TParameter>
			where TParameter : notnull {
			var hint = new PopToRootPresentationHint() {
				ViewModelType = typeof(TViewModel),
				Data = data
			};
			bool result = await ChangePresentation(hint);
			if (hint.PoppedToRoot) {
				result = await Navigate<TViewModel, TParameter>(parameter);
			}
			return result;
		}

		public async Task<bool> NavigateFromRoot(string path, object data = null) {
			var request = await NavigationRouteRequest(path).ConfigureAwait(false);
			if (request.ViewModelInstance == null) {
				return false;
			}
			var hint = new PopToRootPresentationHint() {
				ViewModelType = request.ViewModelType,
				Data = data
			};
			bool result = await ChangePresentation(hint);
			if (hint.PoppedToRoot) {
				result = await Navigate(request, request.ViewModelInstance).ConfigureAwait(false);
			}
			return result;
		}

		public async Task<bool> Navigate(IDictionary<string, string> parameters) {
			if (parameters != null && parameters.TryGetValue(PathKey, out string path)) {
				parameters.TryGetValue(DataKey, out string data);
				const int step = 200;
				int initializationDelay = 2000;
				while (!RootIsInitialized && initializationDelay > 0) {
					await Task.Delay(step);
					initializationDelay -= step;
				}
				if (!RootIsInitialized) {
					return false;
				}
				if (parameters.TryGetValue(FromRootKey, out string fromRootValue) && bool.TryParse(fromRootValue, out bool fromRoot) && fromRoot)
					return await NavigateFromRoot(path, data);
				else //TODO: pass data too
					return await Navigate(path);
			}
			return false;
		}

		public IDictionary<string, string> MakeNavigationParameters(string path, string data = null, bool fromRoot = true) {
			return new Dictionary<string, string>() {
				[PathKey] = path,
				[DataKey] = data,
				[FromRootKey] = fromRoot.ToString()
			};
		}

		public async Task NavigateSubscribingToResult<TViewModel, TResult>(IResultAwaitingViewModel<TResult> fromViewModel)
			where TViewModel : IResultSettingViewModel<TResult> {
			bool navigated = await Navigate<TViewModel>();
			if (navigated)
				fromViewModel.SubscribeToResult();
		}

		public async Task NavigateSubscribingToResult<TViewModel, TParameter, TResult>(IResultAwaitingViewModel<TResult> fromViewModel, TParameter parameter)
			where TViewModel : IResultSettingViewModel<TResult>, IMvxViewModel<TParameter> {
			bool navigated = await Navigate<TViewModel, TParameter>(parameter);
			if (navigated)
				fromViewModel.SubscribeToResult();
		}

		public async Task CloseSettingResult<TViewModel, TResult>(TViewModel viewModel, TResult result)
			where TViewModel : IResultSettingViewModel<TResult> {
			bool closed = await Close(viewModel);
			if (closed)
				viewModel.SetResult(result);
		}
	}
}
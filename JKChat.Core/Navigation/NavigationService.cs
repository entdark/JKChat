using System;
using System.Threading.Tasks;

using JKChat.Core.Navigation.Hints;

using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using MvvmCross.Views;

namespace JKChat.Core.Navigation {
	public class NavigationService : MvxNavigationService, INavigationService {
		public NavigationService(IMvxViewModelLoader viewModelLoader) : base(null, viewModelLoader) {
		}

		//public NavigationService(IMvxViewModelLoader viewModelLoader, IMvxViewDispatcher viewDispatcher, IMvxIoCProvider iocProvider) : base(viewModelLoader, viewDispatcher/*, iocProvider*/) {
		//}

		public async Task<bool> NavigateFromRoot<TViewModel>(Func<IMvxViewModel, bool> condition = null)
			where TViewModel : IMvxViewModel {
			var hint = new PopToRootPresentationHint() {
				ViewModelType = typeof(TViewModel),
				Condition = condition
			};
			bool result = await ChangePresentation(hint);
			if (hint.PoppedToRoot) {
				result = await Navigate<TViewModel>();
			}
			return result;
		}

		public async Task<bool> NavigateFromRoot<TViewModel, TParameter>(TParameter parameter, Func<IMvxViewModel, bool> condition = null)
			where TViewModel : IMvxViewModel<TParameter>
			where TParameter : notnull {
			var hint = new PopToRootPresentationHint() {
				ViewModelType = typeof(TViewModel),
				Condition = condition
			};
			bool result = await ChangePresentation(hint);
			if (hint.PoppedToRoot) {
				result = await Navigate<TViewModel, TParameter>(parameter);
			}
			return result;
		}
	}
}
using System;
using System.Threading.Tasks;

using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace JKChat.Core.Navigation {
	public interface INavigationService : IMvxNavigationService {
		Task<bool> NavigateFromRoot<TViewModel>(Func<IMvxViewModel, bool> condition = null)
			where TViewModel : IMvxViewModel;
		Task<bool> NavigateFromRoot<TViewModel, TParameter>(TParameter parameter, Func<IMvxViewModel, bool> condition = null)
			where TViewModel : IMvxViewModel<TParameter>
			where TParameter : notnull;
	}
}
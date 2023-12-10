using System.Collections.Generic;
using System.Threading.Tasks;

using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace JKChat.Core.Navigation {
	public interface INavigationService : IMvxNavigationService {
		bool RootIsInitialized { get; set; }
		Task<bool> NavigateFromRoot<TViewModel>(object data = null)
			where TViewModel : IMvxViewModel;
		Task<bool> NavigateFromRoot<TViewModel, TParameter>(TParameter parameter, object data = null)
			where TViewModel : IMvxViewModel<TParameter>
			where TParameter : notnull;
		Task<bool> NavigateFromRoot(string path, object data = null);
		Task<bool> Navigate(IDictionary<string, string> parameters);
		IDictionary<string, string> MakeNavigationParameters(string path, string data = null, bool fromRoot = true);
	}
}
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public interface IBaseViewModel : IMvxViewModel {
		string Title { get; set; }
		bool IsLoading { get; set; }
	}
}
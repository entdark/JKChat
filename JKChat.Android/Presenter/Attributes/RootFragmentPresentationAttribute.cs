using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class RootFragmentPresentationAttribute()
		: BaseFragmentPresentationAttribute(
			typeof(MainActivityViewModel),
			Resource.Id.content_master,
			false
		);
}
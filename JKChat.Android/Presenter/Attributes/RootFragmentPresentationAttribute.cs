using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class RootFragmentPresentationAttribute : BaseFragmentPresentationAttribute {
		public RootFragmentPresentationAttribute() : base(
			typeof(MainActivityViewModel),
			Resource.Id.content_master,
			false
		) {}
	}
}
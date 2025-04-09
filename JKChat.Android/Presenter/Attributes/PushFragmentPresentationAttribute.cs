using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class PushFragmentPresentationAttribute : BaseFragmentPresentationAttribute {
		public PushFragmentPresentationAttribute() : base(
			typeof(MainActivityViewModel),
			Resource.Id.content_detail,
			true,
			Resource.Animation.fragment_push_enter,
			Resource.Animation.fragment_push_exit,
			Resource.Animation.fragment_push_pop_enter,
			Resource.Animation.fragment_push_pop_exit,
			registerBackPressedCallback: true
		) {}
	}
}
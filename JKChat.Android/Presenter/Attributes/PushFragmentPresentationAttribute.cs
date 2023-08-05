using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class PushFragmentPresentationAttribute : BaseFragmentPresentationAttribute {
		public PushFragmentPresentationAttribute() : base(
			typeof(MainActivityViewModel),
			Resource.Id.content_detail,
			true,
			Resource.Animation.fragment_slide_rtl,
			Resource.Animation.fragment_hslide_rtl,
			Resource.Animation.fragment_hslide_ltr,
			Resource.Animation.fragment_slide_ltr,
			registerBackPressedCallback: true
		) {}
	}
}
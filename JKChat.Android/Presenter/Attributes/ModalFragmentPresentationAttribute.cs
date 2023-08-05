using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class ModalFragmentPresentationAttribute : BaseFragmentPresentationAttribute {
		public ModalFragmentPresentationAttribute(bool push = false) : base(
			typeof(MainActivityViewModel),
			Resource.Id.content_modal,
			true,
			push ? Resource.Animation.fragment_slide_rtl : Resource.Animation.modal_fade_in,
			push ? Resource.Animation.fragment_hslide_rtl : Resource.Animation.modal_fade_out,
			push ? Resource.Animation.fragment_hslide_ltr : Resource.Animation.modal_fade_in,
			push ? Resource.Animation.fragment_slide_ltr : Resource.Animation.modal_fade_out,
			registerBackPressedCallback: true
		) {}
	}
}
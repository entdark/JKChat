using JKChat.Android.Views.Main;

namespace JKChat.Android.Presenter.Attributes {
	public class ModalFragmentPresentationAttribute(bool push = false)
		: BaseFragmentPresentationAttribute(
			typeof(MainActivityViewModel),
			Resource.Id.content_modal,
			true,
			push ? Resource.Animation.fragment_push_enter : Resource.Animation.modal_fade_in,
			push ? Resource.Animation.fragment_push_exit : Resource.Animation.modal_fade_out,
			push ? Resource.Animation.fragment_push_pop_enter : Resource.Animation.modal_fade_in,
			push ? Resource.Animation.fragment_push_pop_exit : Resource.Animation.modal_fade_out,
			registerBackPressedCallback: true
		);
}
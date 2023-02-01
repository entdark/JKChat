using JKChat.Core.ViewModels.Main;

using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Presenter.Attributes {
	public class PushFragmentPresentationAttribute : MvxFragmentPresentationAttribute {
		public PushFragmentPresentationAttribute() : base(typeof(MainViewModel),
		Resource.Id.content_detail,
		true,
		Resource.Animation.fragment_slide_rtl,
		Resource.Animation.fragment_hslide_rtl,
		Resource.Animation.fragment_hslide_ltr,
		Resource.Animation.fragment_slide_ltr) {}
	}
}
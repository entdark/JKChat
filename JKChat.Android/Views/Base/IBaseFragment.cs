using AndroidX.AppCompat.App;

namespace JKChat.Android.Views.Base {
	interface IBaseFragment {
		string Title { get; set; }
		ActionBar ActionBar { get; }
		bool OnBackPressed();
	}
}
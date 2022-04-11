namespace JKChat.Android.Views.Base {
	interface IBaseFragment {
		string Title { get; set; }
		bool OnBackPressed();
		int Order { get; set; }
	}
}
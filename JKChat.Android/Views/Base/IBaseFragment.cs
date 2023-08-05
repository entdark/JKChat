namespace JKChat.Android.Views.Base {
	interface IBaseFragment {
		public static bool DisableAnimations { get; set; }
		string Title { get; set; }
		int Order { get; set; }
		bool RegisterBackPressedCallback { get; set; }
		bool OnBackPressed();
	}
}
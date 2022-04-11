using AndroidX.AppCompat.Widget;

namespace JKChat.Android.Views.Base {
	public interface IBaseActivity {
		bool ExpandedWindow { get; }
		Toolbar Toolbar { get; }
		void Exit();
		void PopEnter();
	}
}
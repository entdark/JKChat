using AndroidX.Fragment.App;

namespace JKChat.Android.Views.Base {
	public interface ITabsView {
		void CloseFragments(bool animated, int tab = -1);
		Fragment CurrentTabFragment { get; }
		int CurrentTab { get; }
		void MoveToTab(int tab);
	}
}
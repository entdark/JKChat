using System;

using AndroidX.AppCompat.Widget;

namespace JKChat.Android.Views.Base {
	public interface IBaseActivity {
		LayoutState LayoutState { get; }
		bool Landscape { get; }
		bool ExpandedWindow { get; }
		Toolbar Toolbar { get; }
		void Exit(int order);
		void PopEnter(int order);
		event Action<LayoutState, bool> ConfigurationChanged;
	}
}
﻿using AndroidX.AppCompat.Widget;

namespace JKChat.Android.Views.Base {
	public interface IBaseActivity {
		bool ExpandedWindow { get; }
		Toolbar Toolbar { get; }
		void Exit(int order);
		void PopEnter(int order);
	}
}
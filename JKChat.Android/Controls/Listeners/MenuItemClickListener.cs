using System;

using Android.Views;

namespace JKChat.Android.Controls.Listeners {
	public class MenuItemClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener {
		public Func<bool> Click { get; set; }
		public bool OnMenuItemClick(IMenuItem item) {
			return Click?.Invoke() ?? false;
		}
	}
}
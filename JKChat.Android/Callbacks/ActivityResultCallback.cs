using System;

using AndroidX.Activity.Result;

namespace JKChat.Android.Callbacks {
	public class ActivityResultCallback<T> : Java.Lang.Object, IActivityResultCallback where T : Java.Lang.Object {
		private readonly Action<T> callback;
		public ActivityResultCallback(Action<T> callback) {
			this.callback = callback;
		}
		public void OnActivityResult(Java.Lang.Object p0) {
			callback((T)p0);
		}
	}
}
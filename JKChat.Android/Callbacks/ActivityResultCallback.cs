using System;

using AndroidX.Activity.Result;

namespace JKChat.Android.Callbacks {
	public class ActivityResultCallback<T>(Action<T> callback) : Java.Lang.Object, IActivityResultCallback where T : Java.Lang.Object {
		public void OnActivityResult(Java.Lang.Object p0) {
			callback((T)p0);
		}
	}
}
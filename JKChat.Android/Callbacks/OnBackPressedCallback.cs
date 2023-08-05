using System;

namespace JKChat.Android.Callbacks {
	public class OnBackPressedCallback : AndroidX.Activity.OnBackPressedCallback {
		public Action OnBackPressed { get; init; }

		public OnBackPressedCallback(Action onBackPressed, bool enabled = true) : base(enabled) {
			OnBackPressed = onBackPressed;
		}

		public override void HandleOnBackPressed() {
			OnBackPressed?.Invoke();
		}
	}
}
using System;

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views.Animations;
using Android.Widget;

using Java.Lang;

namespace JKChat.Android.Controls.Toolbar {
	[Register("JKChat.Android.Controls.Toolbar.FadingImageView")]
	public class FadingImageView : ImageView {
		private Action completion;

		public Action Action { get; set; }
		
		public FadingImageView(Context context) : base(context) {
			Initialize();
		}

		public FadingImageView(Context context, IAttributeSet attrs) : base(context, attrs) {
			Initialize();
		}

		public FadingImageView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
			Initialize();
		}

		public FadingImageView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {
			Initialize();
		}

		protected FadingImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
			Initialize();
		}

		private void Initialize() {
			HideShow(false, false);
			Click += FadingImageViewClick;
		}

		private void FadingImageViewClick(object sender, EventArgs ev) {
			Action?.Invoke();
		}

		public void HideShow(bool show, bool animated = true, Action completion = null) {
			float value = show ? 1.0f : 0.0f;
			if (!animated) {
				Alpha = value;
				ScaleY = value;
				completion?.Invoke();
				return;
			}
			var animator = this.Animate()
				.Alpha(value)
				.SetDuration(200)
				.SetInterpolator(new DecelerateInterpolator());
			if (show) {
				this.completion = () => {
					completion?.Invoke();
				};
				animator.ScaleY(value);
			} else {
				this.completion = () => {
					ScaleY = value;
					completion?.Invoke();
				};
			}
			animator
				.WithEndAction(new Runnable(this.completion))
				.Start();
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Click -= FadingImageViewClick;
			}
			base.Dispose(disposing);
		}
	}
}
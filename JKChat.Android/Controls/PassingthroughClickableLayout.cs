using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.PassingthroughClickableLayout")]
	public class PassingthroughClickableLayout : FrameLayout {
		public PassingthroughClickableLayout(Context context) : base(context) {
		}

		public PassingthroughClickableLayout(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		public PassingthroughClickableLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
		}

		public PassingthroughClickableLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {
		}

		protected PassingthroughClickableLayout(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override bool OnTouchEvent(MotionEvent ev) {
			return false;
		}

		public override bool DispatchTouchEvent(MotionEvent ev) {
			bool handled = base.OnTouchEvent(ev);
			_ = base.DispatchTouchEvent(ev);
			return handled;
		}
	}
}
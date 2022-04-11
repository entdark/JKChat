using System;

using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.ViewPager")]
	public class ViewPager : AndroidX.ViewPager.Widget.ViewPager {
		public bool ScrollEnabled { get; set; }

		public ViewPager(Context context) : base(context) {
		}

		public ViewPager(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		protected ViewPager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override bool OnTouchEvent(MotionEvent ev) {
			return ScrollEnabled && base.OnTouchEvent(ev);
		}

		public override bool OnInterceptTouchEvent(MotionEvent ev) {
			return ScrollEnabled && base.OnInterceptTouchEvent(ev);
		}
	}
}
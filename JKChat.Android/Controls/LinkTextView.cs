using System;

using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

using static JKChat.Android.ValueConverters.ColourTextValueConverter;

namespace JKChat.Android.Controls {
	[Register("JKChat.Android.Controls.LinkTextView")]
	public class LinkTextView : TextView {
		public LinkTextView(Context context) : base(context) {
		}

		public LinkTextView(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		public LinkTextView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
		}

		public LinkTextView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {
		}

		protected LinkTextView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override bool DispatchTouchEvent(MotionEvent ev) {
			if (!HasOnClickListeners && !LinksClickable) {
				return false;
			}

			if (ev != null && !HasOnClickListeners && Layout != null) {
				int x = (int)ev.GetX();
				int y = (int)ev.GetY();

				x -= TotalPaddingLeft;
				y -= TotalPaddingTop;

				x += ScrollX;
				y += ScrollY;

				Layout layout = Layout;
				int line = layout.GetLineForVertical(y);
				int offset = layout.GetOffsetForHorizontal(line, x);

				if (TextFormatted is ISpanned spanned) {
					var link = spanned.GetSpans(offset, offset, Java.Lang.Class.FromType(typeof(LinkClickableSpan)));
					if (link.Length == 0) {
						return false;
					}
				}
			}
			return base.DispatchTouchEvent(ev);
		}
	}
}
using System;

using Android.Views;

using JKChat.Android.Helpers;

using MvvmCross.Binding;
using MvvmCross.Platforms.Android.Binding.Target;

namespace JKChat.Android.TargetBindings {
	//based on https://github.com/MvvmCross/MvvmCross/blob/7.1.2/MvvmCross/Platforms/Android/Binding/Target/MvxViewMarginTargetBinding.cs
	//but this version uses float since dp can be in float
	public class FloatMarginTargetBinding : MvxAndroidTargetBinding {
		public const string View_Margin = "FMargin";
		public const string View_MarginLeft = "FMarginLeft";
		public const string View_MarginRight = "FMarginRight";
		public const string View_MarginTop = "FMarginTop";
		public const string View_MarginBottom = "FMarginBottom";
		public const string View_MarginStart = "FMarginStart";
		public const string View_MarginEnd = "FMarginEnd";

		private string _whichMargin;

		public FloatMarginTargetBinding(View target, string whichMargin) : base(target) {
			_whichMargin = whichMargin;
		}

		public override Type TargetType => typeof(float);
		public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;

		protected override void SetValueImpl(object target, object value) {
			var view = target as View;
			if (view == null)
				return;

			var layoutParameters = view.LayoutParameters as ViewGroup.MarginLayoutParams;
			if (layoutParameters == null)
				return;

			float dpMargin = (float)value;
			int pxMargin = dpMargin.DpToPx();

			switch (_whichMargin) {
			case View_Margin:
				layoutParameters.SetMargins(pxMargin, pxMargin, pxMargin, pxMargin);
				break;
			case View_MarginLeft:
				layoutParameters.LeftMargin = pxMargin;
				break;
			case View_MarginRight:
				layoutParameters.RightMargin = pxMargin;
				break;
			case View_MarginTop:
				layoutParameters.TopMargin = pxMargin;
				break;
			case View_MarginBottom:
				layoutParameters.BottomMargin = pxMargin;
				break;
			case View_MarginEnd:
				layoutParameters.MarginEnd = pxMargin;
				break;
			case View_MarginStart:
				layoutParameters.MarginStart = pxMargin;
				break;
			}

			view.LayoutParameters = layoutParameters;
		}
	}
}
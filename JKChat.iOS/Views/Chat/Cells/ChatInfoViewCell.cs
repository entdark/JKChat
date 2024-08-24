using System;

using CoreAnimation;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Helpers;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.Chat.Cells {
	public partial class ChatInfoViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ChatInfoViewCell");
		public static readonly UINib Nib;

		public override CGRect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				CountFadingGradientAlpha();
			}
		}

		public override CGRect Bounds {
			get => base.Bounds;
			set {
				base.Bounds = value;
				CountFadingGradientAlpha();
			}
		}

		static ChatInfoViewCell() {
			Nib = UINib.FromName("ChatInfoViewCell", NSBundle.MainBundle);
		}

		protected ChatInfoViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(() => {
				CountFadingGradientAlpha();

				using var set = this.CreateBindingSet<ChatInfoViewCell, ChatInfoItemVM>();
				set.Bind(TimeLabel).For(v => v.Text).To(vm => vm.Time);
				set.Bind(TextLabel).For(v => v.AttributedText).To("ColourText(Text, ColourTextParameter(true, Shadow))");
			});
		}

		public override void AwakeFromNib() {
			base.AwakeFromNib();

			this.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

			var backgroundGradientView = new GradientView(Theme.Color.ChatInfoGradientStart, Theme.Color.ChatInfoGradientEnd, new(0.0f, 0.5f), new(1.0f, 0.5f));
			backgroundGradientView.Layer.Opacity = 0.8f;
			backgroundGradientView.InsertWithConstraintsInto(BackgroundView, 0);

			var fadingGradientView = new GradientView(UIColor.TertiarySystemBackground.ColorWithAlpha(0.0f), UIColor.TertiarySystemBackground, new(0.0f, 0.5f), new(0.712f, 0.5f));
			fadingGradientView.InsertWithConstraintsInto(FadingGradientView, 0);

			TextLabel.Font = UIFont.GetMonospacedSystemFont(15.0f, UIFontWeight.Regular);
			TimeLabel.Font = UIFont.GetMonospacedSystemFont(12.0f, UIFontWeight.Regular);

			TextScrollView.Scrolled += TextScrollViewScrolled;
		}

		private void TextScrollViewScrolled(object sender, EventArgs ev) {
			CountFadingGradientAlpha();
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews();
			CountFadingGradientAlpha();
		}

		public override void PrepareForReuse() {
			base.PrepareForReuse();
			TextScrollView.ContentOffset = CGPoint.Empty;
		}

		private void CountFadingGradientAlpha() {
			if (FadingGradientView == null) {
				return;
			}
			var scrollView = TextScrollView;
			nfloat dx = (scrollView.ContentSize.Width - (scrollView.ContentOffset.X + scrollView.Frame.Width));
			FadingGradientView.Alpha = NMath.Min(NMath.Max((dx / 60.0f), 0.0f), 1.0f);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				TextScrollView.Scrolled -= TextScrollViewScrolled;
			}
			base.Dispose(disposing);
		}

		private class GradientView : UIView {
			[Export("layerClass")]
			public static Class LayerClass() {
				return new Class(typeof(CAGradientLayer));
			}

			public GradientView(UIColor startColor, UIColor endColor, CGPoint startPoint, CGPoint endPoint, CAGradientLayerType type = CAGradientLayerType.Axial) {
				var gradientLayer = (this.Layer as CAGradientLayer);
				gradientLayer.LayerType = type;
				gradientLayer.Colors = new[] { startColor.CGColor, endColor.CGColor };
				gradientLayer.StartPoint = startPoint;
				gradientLayer.EndPoint = endPoint;
			}
		}
	}
}
using System;

using CoreAnimation;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.Chat.Cells {
	public partial class ChatInfoViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ChatInfoViewCell");
		public static readonly UINib Nib;

		private CAGradientLayer backgroundGradientLayer, fadingGradientLayer;

		public override CGRect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				ResizeGradient();
			}
		}

		public override CGRect Bounds {
			get => base.Bounds;
			set {
				base.Bounds = value;
				ResizeGradient();
			}
		}

		static ChatInfoViewCell() {
			Nib = UINib.FromName("ChatInfoViewCell", NSBundle.MainBundle);
		}

		protected ChatInfoViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(() => {
				ResizeGradient();

				using var set = this.CreateBindingSet<ChatInfoViewCell, ChatInfoItemVM>();
				set.Bind(TimeLabel).For(v => v.Text).To(vm => vm.Time);
				set.Bind(TextLabel).For(v => v.AttributedText).To("ColourText(Text, ColourTextParameter(true, Shadow))");
			});
		}

		public override void AwakeFromNib() {
			base.AwakeFromNib();

			this.ContentView.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

			backgroundGradientLayer = new CAGradientLayer() {
				LayerType = CAGradientLayerType.Axial,
				Colors = new CGColor []{ Theme.Color.ChatInfoGradientStart.CGColor, Theme.Color.ChatInfoGradientEnd.CGColor },
				StartPoint = new CGPoint(0.0f, 0.5f),
				EndPoint = new CGPoint(1.0f, 0.5f),
				Opacity = 0.8f
			};
			this.Layer.InsertSublayer(backgroundGradientLayer, 0);

			fadingGradientLayer = new CAGradientLayer() {
				LayerType = CAGradientLayerType.Axial,
				Colors = new CGColor []{ UIColor.TertiarySystemBackground.ColorWithAlpha(0.0f).CGColor, UIColor.TertiarySystemBackground.CGColor },
				StartPoint = new CGPoint(0.0f, 0.5f),
				EndPoint = new CGPoint(0.712f, 0.5f)
			};
			FadingGradientView.Layer.InsertSublayer(fadingGradientLayer, 0);

			TextLabel.Font = UIFont.GetMonospacedSystemFont(15.0f, UIFontWeight.Regular);
			TimeLabel.Font = UIFont.GetMonospacedSystemFont(12.0f, UIFontWeight.Regular);

			TextScrollView.Scrolled += TextScrollViewScrolled;
		}

		private void TextScrollViewScrolled(object sender, EventArgs ev) {
			CountFadingGradientAlpha();
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews();
			ResizeGradient();
		}

		public override void PrepareForReuse() {
			base.PrepareForReuse();
			TextScrollView.ContentOffset = CGPoint.Empty;
		}

		private void ResizeGradient() {
			if (backgroundGradientLayer == null || BackgroundView == null || fadingGradientLayer == null || FadingGradientView == null) {
				return;
			}
			backgroundGradientLayer.Frame = new CGRect(0.0f, 0.0f, BackgroundView.Bounds.Width, Frame.Height);
			fadingGradientLayer.Frame = new CGRect(CGPoint.Empty, FadingGradientView.Frame.Size);
			CountFadingGradientAlpha();
		}

		private void CountFadingGradientAlpha() {
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
	}
}
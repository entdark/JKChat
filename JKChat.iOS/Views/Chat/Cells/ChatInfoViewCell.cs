using System;

using CoreAnimation;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.Views.Chat.Cells {
    public partial class ChatInfoViewCell : MvxTableViewCell {
        public static readonly NSString Key = new NSString("ChatInfoViewCell");
        public static readonly UINib Nib;

        private CAGradientLayer gradientLayer;

        public override CGRect Frame {
            get => base.Frame;
            set {
                base.Frame = value;
                ResizeGradient();
            }
        }

        static ChatInfoViewCell() {
            Nib = UINib.FromName("ChatInfoViewCell", NSBundle.MainBundle);
        }

        protected ChatInfoViewCell(IntPtr handle) : base(handle) {
            this.DelayBind(() => {
                ResizeGradient();

                var set = this.CreateBindingSet<ChatInfoViewCell, ChatInfoItemVM>();
                set.Bind(TimeLabel).For(v => v.Text).To(vm => vm.Time);
                set.Bind(TextLabel).For(v => v.AttributedText).To(vm => vm.Text).WithConversion("ColourText", true);
                set.Apply();
            });
        }

		public override void AwakeFromNib() {
			base.AwakeFromNib();

            this.ContentView.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

            gradientLayer = new CAGradientLayer {
                LayerType = CAGradientLayerType.Axial,
                Colors = new CGColor[] { Theme.Color.ChatInfoGradientStart.CGColor, Theme.Color.ChatInfoGradientEnd.CGColor },
                StartPoint = new CGPoint(0.0f, 0.5f),
                EndPoint = new CGPoint(1.0f, 0.5f)
            };
            BackgroundView.Layer.AddSublayer(gradientLayer);
        }

		public override void LayoutSubviews() {
            base.LayoutSubviews();
            ResizeGradient();
        }

        private void ResizeGradient() {
            if (gradientLayer == null || BackgroundView == null) {
                return;
            }
            gradientLayer.Frame = new CGRect(0.0f, 0.0f, BackgroundView.Bounds.Width, Frame.Height);
        }
    }
}

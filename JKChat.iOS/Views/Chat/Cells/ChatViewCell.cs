using System;
using CoreGraphics;
using Foundation;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.ValueConverters;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using UIKit;

namespace JKChat.iOS.Views.Chat.Cells {
	public partial class ChatViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ChatViewCell");
		public static readonly UINib Nib;

		static ChatViewCell() {
			Nib = UINib.FromName("ChatViewCell", NSBundle.MainBundle);
		}

		protected ChatViewCell(IntPtr handle) : base(handle) {
			this.DelayBind(BindingControls);
		}

		private void BindingControls() {
//			this.ContentView.BackgroundColor = UIColor.Red;
			this.ContentView.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

//			MessageTextView.TextContainerInset = UIEdgeInsets.Zero;
//			MessageTextView.TextContainer.LineFragmentPadding = 0.0f;
/*			var linkTextAttributes = new NSMutableDictionary(MessageTextView.WeakLinkTextAttributes);
			linkTextAttributes.SetValueForKey(NSNumber.FromInt64((long)NSUnderlineStyle.Single), UIStringAttributeKey.UnderlineStyle);
			MessageTextView.WeakLinkTextAttributes = linkTextAttributes;*/
//			MessageTextView.Selectable = false;
			MessageTextView.ClipsToBounds = false;

			var set = this.CreateBindingSet<ChatViewCell, ChatItemVM>();
			set.Bind(PlayerNameLabel).For(v => v.AttributedText).To(vm => vm.PlayerName).WithConversion("ColourText");
			set.Bind(MessageTextView).For(v => v.AttributedText).To(vm => vm.Message).WithConversion("ColourText", new ColourTextParameter() {
				Font = Theme.Font.OCRAStd(14.0f),
				ParseUri = true
			});
			set.Apply();
		}
	}
}

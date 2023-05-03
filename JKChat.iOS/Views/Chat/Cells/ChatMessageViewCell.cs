using System;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.Chat.Cells {
	public partial class ChatMessageViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ChatMessageViewCell");
		public static readonly UINib Nib;

		private Type topVMType;
		public Type TopVMType {
			get => topVMType;
			set {
				topVMType = value;
				SetConstraint(TopConstraint, value);
			}
		}

		private Type bottomVMType;
		public Type BottomVMType {
			get => bottomVMType;
			set {
				bottomVMType = value;
				SetConstraint(BottomConstraint, value);
			}
		}

		static ChatMessageViewCell() {
			Nib = UINib.FromName("ChatMessageViewCell", NSBundle.MainBundle);
		}

		protected ChatMessageViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(() => {
//				this.ContentView.BackgroundColor = UIColor.Red;
				this.ContentView.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

//				MessageTextView.TextContainerInset = UIEdgeInsets.Zero;
//				MessageTextView.TextContainer.LineFragmentPadding = 0.0f;
/*				var linkTextAttributes = new NSMutableDictionary(MessageTextView.WeakLinkTextAttributes);
				linkTextAttributes.SetValueForKey(NSNumber.FromInt64((long)NSUnderlineStyle.Single), UIStringAttributeKey.UnderlineStyle);
				MessageTextView.WeakLinkTextAttributes = linkTextAttributes;*/
//				MessageTextView.Selectable = false;
				MessageTextView.ClipsToBounds = false;

				using var set = this.CreateBindingSet<ChatMessageViewCell, ChatMessageItemVM>();
				set.Bind(TimeLabel).For(v => v.Text).To(vm => vm.Time);
				set.Bind(this).For(v => v.TopVMType).To(vm => vm.TopVMType);
				set.Bind(this).For(v => v.BottomVMType).To(vm => vm.BottomVMType);

				this.AddBindings(PlayerNameLabel, "AttributedText ColourText(PlayerName, ColourTextParameter(true, Shadow))");
				this.AddBindings(MessageTextView, "AttributedText ColourText(Message, ColourTextParameter(true, Shadow))");
			});
		}

		private void SetConstraint(NSLayoutConstraint constraint, Type value) {
			constraint.Constant = value == typeof(ChatMessageItemVM) ? 7.5f : 15.0f;
			LayoutIfNeeded();
		}
	}
}

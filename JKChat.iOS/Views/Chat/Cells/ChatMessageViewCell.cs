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

		static ChatMessageViewCell() {
			Nib = UINib.FromName("ChatMessageViewCell", NSBundle.MainBundle);
		}

		protected ChatMessageViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(() => {
				using var set = this.CreateBindingSet<ChatMessageViewCell, ChatMessageItemVM>();
				set.Bind(TimeLabel).For(v => v.Text).To(vm => vm.Time);
				set.Bind(PlayerNameLabel).For(v => v.AttributedText).To("ColourText(PlayerName, ColourTextParameter(true, Shadow))");
				set.Bind(MessageTextView).For(v => v.AttributedText).To("ColourText(Message, ColourTextParameter(true, Shadow))");
			});
		}

		public override void AwakeFromNib() {
			base.AwakeFromNib();

//			this.ContentView.BackgroundColor = UIColor.Red;
			this.ContentView.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);

//			MessageTextView.TextContainerInset = UIEdgeInsets.Zero;
//			MessageTextView.TextContainer.LineFragmentPadding = 0.0f;
/*			var linkTextAttributes = new NSMutableDictionary(MessageTextView.WeakLinkTextAttributes);
			linkTextAttributes.SetValueForKey(NSNumber.FromInt64((long)NSUnderlineStyle.Single), UIStringAttributeKey.UnderlineStyle);
			MessageTextView.WeakLinkTextAttributes = linkTextAttributes;*/
//			MessageTextView.Selectable = false;
			MessageTextView.ClipsToBounds = false;

			PlayerNameLabel.Font = UIFont.GetMonospacedSystemFont(17.0f, UIFontWeight.Regular);
			MessageTextView.Font = UIFont.GetMonospacedSystemFont(15.0f, UIFontWeight.Regular);
			TimeLabel.Font = UIFont.GetMonospacedSystemFont(12.0f, UIFontWeight.Regular);
		}
	}
}
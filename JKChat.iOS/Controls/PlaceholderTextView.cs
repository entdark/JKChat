using CoreGraphics;

using Foundation;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls {
	[Register("PlaceholderTextView")]
	public class PlaceholderTextView : UITextView {
		private UILabel placeholderLabel;

		public string Placeholder {
			get => placeholderLabel.Text;
			set => placeholderLabel.Text = value;
		}

		public UIColor PlaceholderColor {
			get => placeholderLabel.TextColor;
			set => placeholderLabel.TextColor = value;
		}

		public UIFont PlaceholderFont {
			get => placeholderLabel.Font;
			set => placeholderLabel.Font = value;
		}

		public int MaxLength { get; set; } = 0;

		public override string Text {
			get => base.Text;
			set {
				base.Text = value;
				TogglePlaceholder(base.Text);
			}
		}

		public PlaceholderTextView() {
			Initialize();
		}

		public PlaceholderTextView(NSCoder coder) : base(coder) {
			Initialize();
		}

		public PlaceholderTextView(CGRect frame) : base(frame) {
			Initialize();
		}

		public PlaceholderTextView(CGRect frame, NSTextContainer textContainer) : base(frame, textContainer) {
			Initialize();
		}

		protected PlaceholderTextView(NSObjectFlag t) : base(t) {
			Initialize();
		}

		protected internal PlaceholderTextView(NativeHandle handle) : base(handle) {
			Initialize();
		}

		private void Initialize() {
			TextContainerInset = UIEdgeInsets.Zero;
			TextContainer.LineFragmentPadding = 0.0f;

			placeholderLabel = new UILabel() {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Lines = 1,
				LineBreakMode = UILineBreakMode.TailTruncation,
			};

			ShouldChangeText = (textView, range, text) => {
				string textViewText = textView.Text ?? string.Empty;
				var newText = textViewText.Substring(0, (int)range.Location) + text + textViewText.Substring((int)(range.Location + range.Length));//new NSString(textView.Text).Replace(range, new NSString(text));
				bool check = CheckMaxLength(ref newText);
				TogglePlaceholder(newText);
				if (check) {
					return true;
				}
				textView.Text = newText;
				return false;
			};

			AddSubview(placeholderLabel);
			placeholderLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 0.0f).Active = true;
			placeholderLabel.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, 0.0f).Active = true;
			placeholderLabel.CenterYAnchor.ConstraintEqualTo(this.CenterYAnchor, 0.0f).Active = true;
		}

		private void TogglePlaceholder(string text) {
			placeholderLabel.Hidden = !string.IsNullOrEmpty(text);
		}

		private bool CheckMaxLength(ref string text) {
			if (MaxLength == 0 || string.IsNullOrEmpty(text)) {
				return true;
			} else {
				bool check = text.Length <= MaxLength;
				if (!check) {
					text = text.Substring(0, MaxLength);
				}
				return check;
			}
		}

/*		public override bool ShouldChangeTextInRange(UITextRange inRange, string replacementText) {
			bool shouldChange = base.ShouldChangeTextInRange(inRange, replacementText);
			TogglePlaceholder();
			var newText = this.Text.Substring(0, inRange)
			return CheckMaxLength();
		}*/
	}
}
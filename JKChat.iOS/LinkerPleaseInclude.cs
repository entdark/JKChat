using Foundation;

using MvvmCross.Plugin.Visibility;

using UIKit;

namespace JKChat.iOS {
	public class LinkerPleaseInclude {
		public void Include(MvxVisibilityValueConverter vvc) {
			vvc = new MvxVisibilityValueConverter();
		}
		public void Include(MvxInvertedVisibilityValueConverter ivvc) {
			ivvc = new MvxInvertedVisibilityValueConverter();
		}
		public void Include(UILabel label) {
			label.AttributedText = new NSAttributedString(label.Text);
		}
		public void Include(UITextView textView) {
			textView.AttributedText = new NSAttributedString(textView.Text);
			textView.TextStorage.DidProcessEditing += (sender, ev) => { textView.Text = string.Empty; };
		}
		public void Include(UIView view) {
			view.BackgroundColor = Theme.Color.Accent;
		}
	}
}
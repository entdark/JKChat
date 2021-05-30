using CoreGraphics;

namespace JKChat.iOS.Views.Base {
	public interface IKeyboardViewController {
		CGRect EndKeyboardFrame { get; }
		CGRect BeginKeyboardFrame { get; }
	}
}
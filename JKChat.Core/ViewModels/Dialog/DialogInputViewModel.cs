using System;

namespace JKChat.Core.ViewModels.Dialog {
	public class DialogInputViewModel {
		public string Text { get; set; }
		public string Hint { get; init; }
		public bool HintAsColourText { get; init; }
		public Action<string> TextChangedAction { get; private init; }

		public DialogInputViewModel() {
			TextChangedAction = text => {
				Text = text;
			};
		}

		public DialogInputViewModel(string input, string hint = null) : this() {
			Text = input;
			Hint = hint;
		}

		public DialogInputViewModel(string input, bool hintAsColourText) : this() {
			Text = input;
			HintAsColourText = hintAsColourText;
			if (HintAsColourText)
				Hint = Text;
		}
	}
}
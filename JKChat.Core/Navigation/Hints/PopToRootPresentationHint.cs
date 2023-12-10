using System;

using MvvmCross.Presenters.Hints;

namespace JKChat.Core.Navigation.Hints {
	public class PopToRootPresentationHint : MvxPopToRootPresentationHint {
		public Type ViewModelType { get; init; }
		public object Data { get; init; }
		public bool PoppedToRoot { get; set; }

		public PopToRootPresentationHint(bool animated = true) : base(animated) {
		}
	}
}
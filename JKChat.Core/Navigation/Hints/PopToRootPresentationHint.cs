using System;

using MvvmCross.Presenters.Hints;
using MvvmCross.ViewModels;

namespace JKChat.Core.Navigation.Hints {
	public class PopToRootPresentationHint : MvxPopToRootPresentationHint {
		public Type ViewModelType { get; set; }
		public Func<IMvxViewModel, bool> Condition { get; set; }
		public bool PoppedToRoot { get; set; }

		public PopToRootPresentationHint(bool animated = true) : base(animated) {
		}
	}
}
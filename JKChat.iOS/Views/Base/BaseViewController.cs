using System.Diagnostics;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Base;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Presenters;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.Base {
	public abstract class BaseViewController<TViewModel> : MvxViewController<TViewModel>, IMvxOverridePresentationAttribute, IKeyboardViewController where TViewModel : class, IMvxViewModel, IBaseViewModel {
		private NSObject keyboardWillShowObserver, keyboardWillHideObserver;
		public virtual CGRect EndKeyboardFrame { get; protected set; }
		public virtual CGRect BeginKeyboardFrame { get; protected set; }

		public bool HandleKeyboard { get; set; } = false;

		public override string Title {
			get => base.Title;
			set => base.Title = value;
		}

		protected CGRect NavigationBarFrame => NavigationController?.NavigationBar?.Frame ?? new CGRect(0.0f, 0.0f, View.Frame.Width, 44.0f);

		protected BaseViewController() {}
		protected BaseViewController(NativeHandle handle) : base(handle) { }
		protected BaseViewController(string nibName, NSBundle bundle) : base(nibName, bundle) { }

		public override void LoadView() {
			base.LoadView();

			Debug.WriteLine("UIFont Family names:");
			foreach (var familyName in UIFont.FamilyNames) {
				Debug.WriteLine(familyName);
			}
//			this.View.BackgroundColor = Theme.Color.Background;

			var loadingView = new UIView() {
//				BackgroundColor = Theme.Color.LoadingBackground
			};
			View.AddSubview(loadingView);
			loadingView.LeadingAnchor.ConstraintEqualTo(this.View.LeadingAnchor, 0.0f).Active = true;
			loadingView.TrailingAnchor.ConstraintEqualTo(this.View.TrailingAnchor, 0.0f).Active = true;
			loadingView.TopAnchor.ConstraintEqualTo(this.View.TopAnchor, 0.0f).Active = true;
			loadingView.BottomAnchor.ConstraintEqualTo(this.View.BottomAnchor, 0.0f).Active = true;
			loadingView.TranslatesAutoresizingMaskIntoConstraints = false;

			var loadingIndicatorView = new UIActivityIndicatorView() {
				ActivityIndicatorViewStyle = UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
					? UIActivityIndicatorViewStyle.Large
					: UIActivityIndicatorViewStyle.WhiteLarge,
				Color = Theme.Color.Accent
			};
			loadingView.AddSubview(loadingIndicatorView);
			loadingIndicatorView.StartAnimating();
			loadingIndicatorView.CenterXAnchor.ConstraintEqualTo(loadingView.CenterXAnchor, 0.0f).Active = true;
			loadingIndicatorView.CenterYAnchor.ConstraintEqualTo(loadingView.CenterYAnchor, 0.0f).Active = true;
			loadingIndicatorView.TranslatesAutoresizingMaskIntoConstraints = false;

			using var set = this.CreateBindingSet<BaseViewController<TViewModel>, TViewModel>();
			set.Bind(loadingView).For("Visibility").To(vm => vm.IsLoading).WithConversion("Visibility");
			set.Bind(this).For(v => v.Title).To(vm => vm.Title);
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			NavigationController.NavigationBarHidden = false;
			SubscribeForKeyboardNotifications();
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
			UnsubscribeForKeyboardNotifications();
		}

		private void SubscribeForKeyboardNotifications() {
			if (!HandleKeyboard) {
				return;
			}
			keyboardWillShowObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, KeyboardWillShowNotification);
			keyboardWillHideObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, KeyboardWillHideNotification);
		}

		private void UnsubscribeForKeyboardNotifications() {
			if (!HandleKeyboard) {
				return;
			}
			if (keyboardWillShowObserver != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver(keyboardWillShowObserver);
				keyboardWillShowObserver = null;
			}
			if (keyboardWillHideObserver != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver(keyboardWillHideObserver);
				keyboardWillHideObserver = null;
			}
			EndKeyboardFrame = CGRect.Empty;
			BeginKeyboardFrame = CGRect.Empty;
		}

		protected virtual void KeyboardWillShowNotification(NSNotification notification) {}

		protected virtual void KeyboardWillHideNotification(NSNotification notification) {}

		public virtual MvxBasePresentationAttribute PresentationAttribute(MvxViewModelRequest request) {
			return new MvxSplitViewPresentationAttribute(MasterDetailPosition.Detail);
		}
	}
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using CoreGraphics;

using Foundation;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.iOS.Helpers;

using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.ViewModels;

using UIKit;

namespace JKChat.iOS.Views.Base {
	public abstract class BaseViewController<TViewModel> : MvxViewController<TViewModel>, IKeyboardViewController where TViewModel : class, IMvxViewModel, IBaseViewModel {
		private NSObject keyboardWillShowObserver, keyboardWillHideObserver;
		private nfloat keyboardAfterHiddenOffset = 0.0f;
		private bool keyboardHidden = true;
		private Stopwatch keyboardHiddenStopwatch;
		public virtual CGRect EndKeyboardFrame { get; protected set; }
		public virtual CGRect BeginKeyboardFrame { get; protected set; }

		public bool HandleKeyboard { get; set; } = false;

		public override string Title {
			get => base.Title;
			set => base.Title = value;
		}

		protected virtual Task<bool> BackButtonClick => Task.FromResult(false);

		protected BaseViewController() {}
		protected BaseViewController(IntPtr handle) : base(handle) {}
		protected BaseViewController(string nibName, NSBundle bundle) : base(nibName, bundle) {}

		public override void LoadView() {
			base.LoadView();

			if (NavigationController != null) {
				var titleTextAttributes = new UIStringAttributes {
					ForegroundColor = Theme.Color.Title,
					Font = Theme.Font.ANewHope(13.0f)
				};
				NavigationController.NavigationBar.TitleTextAttributes = titleTextAttributes;
				NavigationController.NavigationBar.BarTintColor = Theme.Color.NavigationBar;
				NavigationController.NavigationBar.Translucent = false;

				if (NavigationController.ViewControllers.Length > 1) {
					var backImage = UIImage.FromFile("Images/Back.png").ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
					var barButtonItem = new UIBarButtonItem(backImage, UIBarButtonItemStyle.Plain, async (sender, ev) => {
						bool handled =  await BackButtonClick;
						if (!handled && !NavigationController.IsMovingToParentViewController) {
							NavigationController.PopViewController(true);
						}
					}) {
						ImageInsets = new UIEdgeInsets(0.0f, 3.0f, 0.0f, 0.0f)
					};
					NavigationItem.LeftBarButtonItem = barButtonItem;
					NavigationController.InteractivePopGestureRecognizer.Delegate = new UIGestureRecognizerDelegate();
				}
			}

			Debug.WriteLine("UIFont Family names:");
			foreach (var familyName in UIFont.FamilyNames) {
				Debug.WriteLine(familyName);
			}
//			this.View.BackgroundColor = Theme.Color.Background;

			var loadingView = new UIView();
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

			var set = this.CreateBindingSet<BaseViewController<TViewModel>, TViewModel>();
			set.Bind(loadingView).For("Visibility").To(vm => vm.IsLoading).WithConversion("Visibility");
			set.Bind(this).For(v => v.Title).To(vm => vm.Title);
			set.Apply();
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
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

		protected virtual void KeyboardWillShowNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			EndKeyboardFrame = endKeyboardFrame;
			BeginKeyboardFrame = beginKeyboardFrame;
			Debug.WriteLine($"KeyboardWillShowNotification.BeginKeyboardFrame: {BeginKeyboardFrame}");
			Debug.WriteLine($"KeyboardWillShowNotification.EndKeyboardFrame: {EndKeyboardFrame}");
			Action<UIScrollView> action = (scrollView) => {
				scrollView.ContentInset = new UIEdgeInsets(scrollView.ContentInset.Top, scrollView.ContentInset.Left, EndKeyboardFrame.Height - DeviceInfo.SafeAreaInsets.Bottom, scrollView.ContentInset.Right);
				scrollView.ScrollIndicatorInsets = new UIEdgeInsets(scrollView.ScrollIndicatorInsets.Top, scrollView.ScrollIndicatorInsets.Left, EndKeyboardFrame.Height - DeviceInfo.SafeAreaInsets.Bottom, scrollView.ScrollIndicatorInsets.Right);
			};
			var scrollView = this.View.FindView<UIScrollView>();
			nfloat dKeyboardOffset = (BeginKeyboardFrame.Y - EndKeyboardFrame.Y);
			if (/*DeviceInfo.SafeAreaInsets.Bottom <= 0.0f && */keyboardHidden && keyboardHiddenStopwatch?.ElapsedMilliseconds > (duration)*1000L) {
				keyboardHidden = false;
			}
			nfloat offset = !keyboardHidden ? dKeyboardOffset : 0.0f;
			offset += keyboardAfterHiddenOffset;
			if (keyboardHidden) {
				keyboardAfterHiddenOffset = dKeyboardOffset;
			} else {
				keyboardAfterHiddenOffset = 0.0f;
			}
			if (BeginKeyboardFrame.Height < EndKeyboardFrame.Height)
				scrollView?.SetContentOffset(new CGPoint(scrollView.ContentOffset.X, offset - scrollView.ContentOffset.Y), BeginKeyboardFrame.Height == EndKeyboardFrame.Height);
			if (!Mvx.IoCProvider.Resolve<IDialogService>().Showing || (!keyboardHidden/* && (BeginKeyboardFrame.Y != EndKeyboardFrame.Y)*/)) {
				ApplyKeyboardAnimation(duration, animationOptions, scrollView, action);
			}
			keyboardHidden = false;
			keyboardHiddenStopwatch?.Stop();
			keyboardHiddenStopwatch = null;
		}

		protected virtual void KeyboardWillHideNotification(NSNotification notification) {
			keyboardHidden = true;
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			EndKeyboardFrame = endKeyboardFrame;
			BeginKeyboardFrame = beginKeyboardFrame;
			Debug.WriteLine($"KeyboardWillHideNotification.BeginKeyboardFrame: {BeginKeyboardFrame}");
			Debug.WriteLine($"KeyboardWillHideNotification.EndKeyboardFrame: {EndKeyboardFrame}");
			Action<UIScrollView> action = (scrollView) => {
				scrollView.ContentInset = new UIEdgeInsets(scrollView.ContentInset.Top, scrollView.ContentInset.Left, EndKeyboardFrame.Height - DeviceInfo.SafeAreaInsets.Bottom, scrollView.ContentInset.Right);
				scrollView.ScrollIndicatorInsets = new UIEdgeInsets(scrollView.ScrollIndicatorInsets.Top, scrollView.ScrollIndicatorInsets.Left, EndKeyboardFrame.Height - DeviceInfo.SafeAreaInsets.Bottom, scrollView.ScrollIndicatorInsets.Right);
			};
			var scrollView = this.View.FindView<UIScrollView>();
//			scrollView?.SetContentOffset(new CGPoint(scrollView.ContentOffset.X, (BeginKeyboardFrame.Y - EndKeyboardFrame.Y) - scrollView.ContentOffset.Y), BeginKeyboardFrame.Height == EndKeyboardFrame.Height);
			ApplyKeyboardAnimation(duration, animationOptions, scrollView, action, () => {
				keyboardHiddenStopwatch = new Stopwatch();
				keyboardHiddenStopwatch.Start();
			});
		}

		private static void ApplyKeyboardAnimation(double duration, UIViewAnimationOptions animationOptions, UIScrollView scrollView, Action<UIScrollView> action, Action completion = null) {
			if (scrollView == null) {
				return;
			}
			UIView.Animate(duration, 0.0, animationOptions, () => {
				action?.Invoke(scrollView);
			}, () => {
				completion?.Invoke();
			});
		}
	}
}
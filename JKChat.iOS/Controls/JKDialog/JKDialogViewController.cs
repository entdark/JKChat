using System;
using System.Threading.Tasks;

using CoreGraphics;

using Foundation;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.iOS.Controls.JKDialog.Cells;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls.JKDialog {
	public partial class JKDialogViewController : UIViewController {
		private readonly JKDialogConfig config;
		private readonly TaskCompletionSource<object> tcs;
		private readonly bool handleKeyboard = false;
		private readonly ColourTextValueConverter colourTextConverter = new ColourTextValueConverter();
		private NSObject keyboardWillShowObserver, keyboardWillHideObserver;

		private string message => MessageLabel.Text;
		private string input => InputTextField.Text;
		private DialogItemVM selectedItem => config.ListViewModel.Items.Find(item => item.IsSelected);

		public override bool CanBecomeFirstResponder => true;

		public JKDialogViewController(JKDialogConfig config, TaskCompletionSource<object> tcs) : base("JKDialogViewController", null) {
			ModalPresentationStyle = UIModalPresentationStyle.Custom;
			ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve;
			this.config = config;
			this.tcs = tcs;
			if ((config.Type & JKDialogType.Input) != 0) {
				handleKeyboard = true;
			}
		}

		public JKDialogViewController(NativeHandle handle) : base(handle) {
		}

		#region View lifecycle

		public override void ViewDidLoad() {
			base.ViewDidLoad();

			DialogView.Transform = CGAffineTransform.MakeScale(1.337f, 1.337f);
			DialogView.Alpha = 0.0f;

			TitleLabel.Text = config?.Title;

			if (!string.IsNullOrEmpty(config?.Message)) {
				SetMessageText(config?.Message);
			}

			if (!string.IsNullOrEmpty(config?.LeftButton)) {
				LeftButton.SetTitle(config?.LeftButton, UIControlState.Normal);
				LeftButton.TouchUpInside += LeftButtonTouchUpInside;
			} else {
				LeftButton.Hidden = true;
			}

			RightButton.SetTitle(config?.RightButton, UIControlState.Normal);
			RightButton.TouchUpInside += RightButtonTouchUpInside;

			InputTextField.Text = config?.Input;
			InputTextField.EditingChanged += InputTextFieldEditingChanged;

			if (config?.ListViewModel != null) {
				ListTableView.RegisterNibForCellReuse(JKDialogViewCell.Nib, JKDialogViewCell.Key);

				int count = config.ListViewModel.Items.Count;
				ListHeightConstraint.Constant = count > 5 ? 242.0f : (count * 44.0f);

				var source = new MvxSimpleTableViewSource(ListTableView, JKDialogViewCell.Key) {
					ItemsSource = config?.ListViewModel.Items,
					SelectionChangedCommand = config?.ListViewModel.ItemClickCommand
				};

				ListTableView.Source = source;
				ListTableView.ReloadData();
			} else {
				ListTableView.Hidden = true;
				ListView.Hidden = true;
			}

			if (config == null) {
				MessageView.Hidden = true;
				InputView.Hidden = true;
				ListView.Hidden = true;
			} else {
				if ((config.Type & JKDialogType.Title) == 0) {
					TitleView.Hidden = true;
				}
				if ((config.Type & JKDialogType.Message) == 0) {
					MessageView.Hidden = true;
				}
				if ((config.Type & JKDialogType.Input) == 0) {
					InputView.Hidden = true;
				}
				if ((config.Type & JKDialogType.List) == 0) {
					ListView.Hidden = true;
				}
			}

			BackgroundButton.TouchUpInside += BackgroundButtonTouchUpInside;
		}

		private void InputTextFieldEditingChanged(object sender, EventArgs ev) {
			SetMessageText((sender as UITextField)?.Text);
		}

		private void SetMessageText(string text) {
			var message = colourTextConverter.Convert(text);
			MessageLabel.AttributedText = message;

			var helperLabel = new UILabel(new CGRect(0.0f, 0.0f, 230.0f, 0.0f)) {
				AttributedText = message,
				Lines = 0,
				Font = Theme.Font.ErgoeMedium(15.0f),
				TextAlignment = UITextAlignment.Left,
				LineBreakMode = UILineBreakMode.WordWrap
			};
			helperLabel.SizeToFit();

			if (helperLabel.Frame.Height <= 85.0f) {
				MessageLabel.TextAlignment = UITextAlignment.Center;
			}
			nfloat height = helperLabel.Frame.Height + 20.0f;
			if (height >= 242.0f) {
				MessageHeightConstraint.Constant = 242.0f;
			} else {
				MessageHeightConstraint.Constant = height;
				MessageScrollView.ScrollEnabled = false;
			}
		}

#nullable enable
		public override void DismissViewController(bool animated, Action? action) {
			base.DismissViewController(animated, action);
			if (config?.ImmediateResult ?? true) {
				DismissAction();
			}
		}
#nullable disable

		private void BackgroundButtonTouchUpInside(object sender, EventArgs ev) {
			ButtonTouchUpInside(config?.BackgroundClick);
		}

		private void LeftButtonTouchUpInside(object sender, EventArgs ev) {
			ButtonTouchUpInside(config?.LeftClick);
		}

		private void RightButtonTouchUpInside(object sender, EventArgs ev) {
			ButtonTouchUpInside(config?.RightClick);
		}

		private void ButtonTouchUpInside(Action<object> action) {
			object obj;
			if ((config.Type & JKDialogType.Input) != 0) {
				obj = input;
			} else if ((config.Type & JKDialogType.List) != 0) {
				obj = selectedItem;
			} else if ((config.Type & JKDialogType.Message) != 0) {
				obj = message;
			} else {
				obj = null;
			}
			action?.Invoke(obj);
			config?.AnyClick?.Invoke(obj);

			InvokeOnMainThread(() => {
				Action action = null;
				if (!(config?.ImmediateResult ?? true)) {
					action = DismissAction;
				}
				UIView.Animate(0.200, () => {
//					DialogView.Transform = CGAffineTransform.MakeScale(1.337f, 1.337f);
					DialogView.Alpha = 0.0f;
				});
				DismissViewController(true, action);
			});
		}

		private void DismissAction() {
			tcs?.TrySetResult(null);
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			SubscribeForKeyboardNotifications();
			if (DialogView.Alpha != 1.0f) {
				UIView.Animate(0.200, () => {
					DialogView.Transform = CGAffineTransform.MakeScale(1.0f, 1.0f);
					DialogView.Alpha = 1.0f;
				});
			}
		}

		public override void ViewDidAppear(bool animated) {
			base.ViewDidAppear(animated);
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
			UnsubscribeForKeyboardNotifications();
		}

		public override void ViewDidDisappear(bool animated) {
			base.ViewDidDisappear(animated);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				BackgroundButton.TouchUpInside -= BackgroundButtonTouchUpInside;
				InputTextField.EditingChanged -= InputTextFieldEditingChanged;
			}
			base.Dispose(disposing);
		}

		#endregion

		private void SubscribeForKeyboardNotifications() {
			if (!handleKeyboard) {
				return;
			}
			keyboardWillShowObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, KeyboardWillShowNotification);
			keyboardWillHideObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, KeyboardWillHideNotification);
		}

		private void UnsubscribeForKeyboardNotifications() {
			if (!handleKeyboard) {
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
		}

		private void KeyboardWillShowNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			UIView.Animate(duration, 0.0, animationOptions, () => {
				DialogViewCenterYConstraint.Constant = endKeyboardFrame.Height * 0.5f;
				this.View.LayoutIfNeeded();
			}, null);
		}

		private void KeyboardWillHideNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			UIView.Animate(duration, 0.0, animationOptions, () => {
				DialogViewCenterYConstraint.Constant = 0.0f;
				this.View.LayoutIfNeeded();
			}, null);
		}
	}
}
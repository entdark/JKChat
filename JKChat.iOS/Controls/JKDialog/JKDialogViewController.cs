using System;

using CoreGraphics;

using Foundation;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.iOS.Controls.JKDialog.Cells;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;

using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls.JKDialog {
	public partial class JKDialogViewController : UIViewController {
		private readonly JKDialogConfig config;
		private bool handleKeyboard = false;
		private NSObject keyboardWillShowObserver, keyboardWillHideObserver;

		public override bool CanBecomeFirstResponder => true;

		public JKDialogViewController(JKDialogConfig config) : base("JKDialogViewController", null) {
			ModalPresentationStyle = UIModalPresentationStyle.Custom;
			ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve;
			this.config = config;
		}

		public JKDialogViewController(NativeHandle handle) : base(handle) {
		}

		#region View lifecycle

		public override void ViewDidLoad() {
			base.ViewDidLoad();

			DialogView.Transform = CGAffineTransform.MakeScale(1.337f, 1.337f);
			DialogView.Alpha = 0.0f;

			TitleLabel.Text = config.Title;

			string message = (config.HasInput && config.Input.HintAsColourText) ? config.Input.Hint : config.Message;
			SetMessageText(message);

			if (config.HasCancel) {
				LeftButton.SetTitle(config.CancelText, UIControlState.Normal);
				LeftButton.TouchUpInside += LeftButtonTouchUpInside;
			} else {
				LeftButton.Hidden = true;
			}
			if (config.HasOk) {
				RightButton.SetTitle(config.OkText, UIControlState.Normal);
				RightButton.TouchUpInside += RightButtonTouchUpInside;
			} else {
				RightButton.Hidden = true;
			}
			ButtonsSeparatorView.Hidden = !config.HasCancel || !config.HasOk;

			handleKeyboard = config.HasInput;
			InputTextField.Text = config.Input?.Text;
			InputTextField.EditingChanged += InputTextFieldEditingChanged;

			if (config.HasList) {
				var list = config.List;
				ListTableView.RegisterNibForCellReuse(JKDialogViewCell.Nib, JKDialogViewCell.Key);
				ListTableView.AllowsMultipleSelection = list.SelectionType == DialogSelectionType.MultiSelection;

				int count = list.Items.Count;
				ListHeightConstraint.Constant = count > 5 ? 242.0f : (count * 44.0f);

				var source = new MvxSimpleTableViewSource(ListTableView, JKDialogViewCell.Key) {
					ItemsSource = list.Items,
					SelectionChangedCommand = new MvxCommand<DialogItemVM>(item => {
						switch (list.SelectionType) {
							case DialogSelectionType.InstantSelection:
								item.IsSelected = true;
								ButtonTouchUpInside(config.OkAction);
								break;
							case DialogSelectionType.SingleSelection:
								list.ItemClickCommand?.Execute(item);
								break;
							case DialogSelectionType.MultiSelection:
								item.IsSelected = !item.IsSelected;
								break;
						}
					}),
					DeselectAutomatically = true
				};

				ListTableView.Source = source;
				ListTableView.ReloadData();
			} else {
				ListTableView.Hidden = true;
				ListView.Hidden = true;
			}

			TitleView.Hidden = !config.HasTitle;
			MessageView.Hidden = !config.HasMessage && (!config.HasInput || (config.HasInput && !config.Input.HintAsColourText));
			InputView.Hidden = !config.HasInput;
			ListView.Hidden = !config.HasList;

			BackgroundButton.TouchUpInside += BackgroundButtonTouchUpInside;
		}

		private void InputTextFieldEditingChanged(object sender, EventArgs ev) {
			string text = (sender as UITextField)?.Text;
			config.Input.Text = text;
			if (!config.Input.HintAsColourText)
				return;
			SetMessageText(text);
		}

		private void SetMessageText(string text) {
			var message = ColourTextValueConverter.Convert(text);
			MessageLabel.AttributedText = message;

			var helperLabel = new UILabel(new CGRect(0.0f, 0.0f, 230.0f, 0.0f)) {
				AttributedText = message,
				Lines = 0,
				Font = UIFont.PreferredFootnote,
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

		private void BackgroundButtonTouchUpInside(object sender, EventArgs ev) {
//			ButtonTouchUpInside(config?.CancelAction);
		}

		private void LeftButtonTouchUpInside(object sender, EventArgs ev) {
			ButtonTouchUpInside(config.CancelAction);
		}

		private void RightButtonTouchUpInside(object sender, EventArgs ev) {
			ButtonTouchUpInside(config.OkAction);
		}

		private void ButtonTouchUpInside(Action<JKDialogConfig> action) {
			action?.Invoke(config);
			InvokeOnMainThread(() => {
				Action action2 = () => { };
				UIView.Animate(0.200, () => {
//					DialogView.Transform = CGAffineTransform.MakeScale(1.337f, 1.337f);
					DialogView.Alpha = 0.0f;
				});
				DismissViewController(true, action2);
			});
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
			if (config.HasInput) {
				InputTextField.BecomeFirstResponder();
			}
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
			UnsubscribeForKeyboardNotifications();
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				LeftButton.TouchUpInside -= LeftButtonTouchUpInside;
				RightButton.TouchUpInside -= RightButtonTouchUpInside;
				InputTextField.EditingChanged -= InputTextFieldEditingChanged;
				BackgroundButton.TouchUpInside -= BackgroundButtonTouchUpInside;
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
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

using CoreFoundation;

using CoreGraphics;

using Foundation;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Controls;
using JKChat.iOS.Helpers;
using JKChat.iOS.Views.Base;
using JKChat.iOS.Views.Chat.Cells;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

namespace JKChat.iOS.Views.Chat {
	public partial class ChatViewController : BaseViewController<ChatViewModel>, IUIGestureRecognizerDelegate {
		private UIView statusView;
		private UILabel statusLabel, titleLabel;
		private UIStackView titleStackView;
		private readonly InputAccessoryView inputAccessoryView = new InputAccessoryView();
		private bool appeared = false;
		private readonly Stopwatch itemTappedStopwatch = new Stopwatch();
		private readonly Timer itemTappedTimer = new Timer(500.0) {
			AutoReset = false
		};
		private CGPoint lastTappedPoint = CGPoint.Empty;
		private long lastTappedTime = 0L;
		private ChatItemVM lastTappedItem = null;

		private string message;
		public string Message {
			get => message;
			set {
				if (string.IsNullOrEmpty(message) != string.IsNullOrEmpty(value)) {
					AnimateSendButton(!string.IsNullOrEmpty(value));
				}
				message = value;
//				ResizeInputAccessoryView();
			}
		}

		private bool selectingChatType;
		public bool SelectingChatType {
			get => selectingChatType;
			set {
				selectingChatType = value;
//				ResizeInputAccessoryView();
			}
		}

		private ChatType chatType;
		public ChatType ChatType {
			get => chatType;
			set {
				chatType = value;
				SetChatTypeImage();
			}
		}

		public override bool CanBecomeFirstResponder => appeared;
		public override UIView InputAccessoryView => inputAccessoryView;//MessageView;

		protected override Task<bool> BackButtonClick => ViewModel?.OfferDisconnect();

		public ChatViewController() : base("ChatViewController", null) {
			HandleKeyboard = true;
		}

		public override void DidReceiveMemoryWarning() {
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		public override void LoadView() {
			base.LoadView();
			itemTappedStopwatch.Start();

			inputAccessoryView.BackgroundColor = Theme.Color.NavigationBar;
			inputAccessoryView.AutoresizingMask = UIViewAutoresizing.All;
			inputAccessoryView.SetSize(new CGSize(DeviceInfo.ScreenBounds.Width, 44.0f));
//			MessageTextView.Frame = new CGRect(0.0f, 0.0f, DeviceInfo.ScreenBounds.Width, 44.0f);
			MessageView.RemoveFromSuperview();
			inputAccessoryView.AddAccessoryView(MessageView);
//			ChatTypeStackView.Hidden = true;
//			ChatTableView.ExtraContentInset = new UIEdgeInsets(15.0f, 0.0f, 15.0f, 0.0f);
			ChatTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive;
			ChatTableView.KeyboardViewController = this;
			ChatTableView.RowHeight = UITableView.AutomaticDimension;
			ChatTableView.EstimatedRowHeight = UITableView.AutomaticDimension;
//			ChatTableView.ContentInset = new UIEdgeInsets(ChatTableView.ContentInset.Top - DeviceInfo.SafeAreaInsets.Bottom, ChatTableView.ContentInset.Left, ChatTableView.ContentInset.Bottom + ChatTableView.SpecialOffset - DeviceInfo.SafeAreaInsets.Bottom, ChatTableView.ContentInset.Right);
//			ChatTableView.ScrollIndicatorInsets = new UIEdgeInsets(ChatTableView.ScrollIndicatorInsets.Top - DeviceInfo.SafeAreaInsets.Bottom, ChatTableView.ScrollIndicatorInsets.Left, ChatTableView.ScrollIndicatorInsets.Bottom + ChatTableView.SpecialOffset - DeviceInfo.SafeAreaInsets.Bottom, ChatTableView.ScrollIndicatorInsets.Right);
			RecountTableInsets();
const float deltaTappedPos = 5.0f;
			UILongPressGestureRecognizer longPressGestureRecognizer;
			ChatTableView.AddGestureRecognizer(longPressGestureRecognizer = new UILongPressGestureRecognizer((gr) => {
				switch (gr.State) {
				case UIGestureRecognizerState.Began:
					lastTappedTime = itemTappedStopwatch.ElapsedMilliseconds;
					itemTappedTimer.Start();
					lastTappedPoint = this.NavigationController?.View != null ? gr.LocationInView(this.NavigationController.View) : CGPoint.Empty;
					var point = gr.LocationInView(ChatTableView);
					var indexPath = ChatTableView.IndexPathForRowAtPoint(point);
					if (indexPath != null && ViewModel.Items.Count > indexPath.Row) {
						lastTappedItem = ViewModel.Items[indexPath.Row];
					}
					break;
				case UIGestureRecognizerState.Ended:
					long dt = itemTappedStopwatch.ElapsedMilliseconds - lastTappedTime;
					var currentPoint = this.NavigationController?.View != null ? gr.LocationInView(this.NavigationController.View) : CGPoint.Empty;
					nfloat dx = NMath.Abs(lastTappedPoint.X - currentPoint.X), dy = NMath.Abs(lastTappedPoint.Y - currentPoint.Y);
					if (lastTappedItem != null && lastTappedTime != 0L && !ChatTableView.StartedDragging
						&& lastTappedPoint != CGPoint.Empty && currentPoint != CGPoint.Empty && dx < deltaTappedPos && dy < deltaTappedPos) {
						if (dt >= 500L) {
//							ViewModel.LongPressCommand?.Execute(lastTappedItem);
						} else {
							ViewModel.SelectionChangedCommand?.Execute(lastTappedItem);
						}
					}
					goto case UIGestureRecognizerState.Cancelled;
				case UIGestureRecognizerState.Cancelled:
				case UIGestureRecognizerState.Failed:
					lastTappedItem = null;
					lastTappedTime = 0L;
					itemTappedTimer.Stop();
					lastTappedPoint = CGPoint.Empty;
					ChatTableView.StartedDragging = false;
					break;
				}
			}) {
				MinimumPressDuration = 0.0,
				Delegate = this,
//				CancelsTouchesInView = false
			});
			itemTappedTimer.Elapsed += (sender, ev) => {
				itemTappedTimer.Stop();
				InvokeOnMainThread(() => {
					var currentPoint = this.NavigationController?.View != null ? longPressGestureRecognizer.LocationInView(this.NavigationController.View) : CGPoint.Empty;
					nfloat dx = NMath.Abs(lastTappedPoint.X - currentPoint.X), dy = NMath.Abs(lastTappedPoint.Y - currentPoint.Y);
					if (!ChatTableView.Dragging && lastTappedPoint != CGPoint.Empty && currentPoint != CGPoint.Empty && dx < deltaTappedPos && dy < deltaTappedPos) {
						ViewModel.LongPressCommand?.Execute(lastTappedItem);
					}
					lastTappedItem = null;
					lastTappedTime = 0L;
					lastTappedPoint = CGPoint.Empty;
				});
			};
			ViewBottomConstraint.Constant = 44.0f + DeviceInfo.SafeAreaInsets.Bottom - ChatTableView.SpecialOffset;

			ChatTypeCommonButton.ImageEdgeInsets = new UIEdgeInsets(0.0f, 20.0f, 0.0f, 0.0f);
			ChatTypeCommonButton.TitleEdgeInsets = new UIEdgeInsets(0.0f, 26.0f, 0.0f, 0.0f);

			ChatTypeTeamButton.ImageEdgeInsets = new UIEdgeInsets(0.0f, -3.0f, 0.0f, 3.0f);
			ChatTypeTeamButton.TitleEdgeInsets = new UIEdgeInsets(0.0f, 3.0f, 0.0f, -3.0f);

			ChatTypePrivateButton.ImageEdgeInsets = new UIEdgeInsets(0.0f, 0.0f, 0.0f, 26.0f);
			ChatTypePrivateButton.TitleEdgeInsets = new UIEdgeInsets(0.0f, 0.0f, 0.0f, 20.0f);

			ChatTypeButton.ImageEdgeInsets = new UIEdgeInsets(16.0f, 20.0f, 13.0f, 16.0f);

			MessageTextView.Placeholder = "Write a message...";
			MessageTextView.PlaceholderColor = Theme.Color.Placeholder;
			MessageTextView.PlaceholderFont = Theme.Font.Arial(18.0f);
			MessageTextView.TextContainerInset = new UIEdgeInsets(10.0f, 0.0f, 10.0f, 0.0f);
			MessageTextView.MaxLength = 149;

			SendButton.Transform = CGAffineTransform.MakeScale(0.0f, 0.0f);

			titleLabel = new UILabel() {
				TextColor = Theme.Color.Title,
				TextAlignment = UITextAlignment.Center,
				Font = Theme.Font.ANewHope(13.0f)
			};

			statusView = new UIView();
			statusView.Layer.CornerRadius = 4.0f;
			statusView.WidthAnchor.ConstraintEqualTo(8.0f).Active = true;
			statusView.HeightAnchor.ConstraintEqualTo(8.0f).Active = true;

			statusLabel = new UILabel() {
				TextColor = Theme.Color.Subtitle,
				TextAlignment = UITextAlignment.Left,
				Font = Theme.Font.ErgoeBold(14.0f)
			};

			var statusStackView = new UIStackView(new UIView[] { statusView, statusLabel }) {
				Axis = UILayoutConstraintAxis.Horizontal,
				Spacing = 4.0f,
				Alignment = UIStackViewAlignment.Center
			};

			titleStackView = new UIStackView(new UIView[] { titleLabel, statusStackView }) {
				Axis = UILayoutConstraintAxis.Vertical,
				Spacing = 2.0f,
				Alignment = UIStackViewAlignment.Center
			};
			RespaceTitleView();

			NavigationItem.TitleView = titleStackView;
		}

		[Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
		private bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer1, UIGestureRecognizer gestureRecognizer2) {
			return true;
		}

		private void MessageTextViewChanged(object sender, EventArgs ev) {
			ResizeInputAccessoryView();
		}

		#region View lifecycle

		public override void ViewDidLoad() {
			base.ViewDidLoad();
			var source = new ChatTableViewSource(ChatTableView, ChatMessageViewCell.Key) {
				ViewControllerWithKeyboard = this,
				ViewBottomConstraint = ViewBottomConstraint
			};

			var set = this.CreateBindingSet();
			set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
//			set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.SelectionChangedCommand);
			set.Bind(MessageTextView).For(v => v.Text).To(vm => vm.Message).TwoWay();
			set.Bind(this).For(v => v.Message).To(vm => vm.Message);
			set.Bind(SendButton).To(vm => vm.SendMessageCommand);
			set.Bind(ChatTypeButton).To(vm => vm.ChatTypeCommand);
			set.Bind(this).For(v => v.ChatType).To(vm => vm.ChatType);
			set.Bind(ChatTypeCommonButton).To(vm => vm.CommonChatTypeCommand);
			set.Bind(ChatTypeTeamButton).To(vm => vm.TeamChatTypeCommand);
			set.Bind(ChatTypePrivateButton).To(vm => vm.PrivateChatTypeCommand);
			set.Bind(ChatTypeStackView).For("Visibility").To(vm => vm.SelectingChatType).WithConversion("Visibility");
			set.Bind(this).For(v => v.SelectingChatType).To(vm => vm.SelectingChatType);
			set.Bind(titleLabel).For(v => v.AttributedText).To(vm => vm.Title).WithConversion("ColourText");
			set.Bind(statusView).For(v => v.BackgroundColor).To(vm => vm.Status).WithConversion("ConnectionColor");
			set.Bind(statusLabel).For(v => v.Text).To(vm => vm.Status);
			set.Apply();

			ChatTableView.Source = source;
			ChatTableView.ReloadData();
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			if (!appeared) {
				Task.Run(async () => {
					await Task.Delay(64);
					appeared = true;
					InvokeOnMainThread(() => {
						BecomeFirstResponder();
					});
				});
			}
		}

		public override void ViewDidAppear(bool animated) {
			base.ViewDidAppear(animated);
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
		}

		public override void ViewDidDisappear(bool animated) {
			base.ViewDidDisappear(animated);
		}

		#endregion

		private void AnimateSendButton(bool show) {
			if (show) {
				UIView.Animate(0.200, () => {
					SendButton.Transform = CGAffineTransform.MakeScale(1.0f, 1.0f);
				});
			} else {
				UIView.Animate(0.200, () => {
					//setting 0.0f, 0.0f causes the bug where animation happens instantly
					SendButton.Transform = CGAffineTransform.MakeScale(float.Epsilon, float.Epsilon);
				});
			}
		}

		private void SetChatTypeImage() {
			UIImage image;
			switch (ChatType) {
			default:
			case ChatType.Common:
				image = UIImage.FromFile("Images/ChatTypeCommon.png");
				break;
			case ChatType.Team:
				image = UIImage.FromFile("Images/ChatTypeTeam.png");
				break;
			case ChatType.Private:
				image = UIImage.FromFile("Images/ChatTypePrivate.png");
				break;
			}
			ChatTypeButton.SetImage(image, UIControlState.Normal);
		}

		private void ResizeInputAccessoryView() {
			float height = 44.0f;//Math.Max((float)MessageTextView.IntrinsicContentSize.Height, 44.0f);
/*			if (SelectingChatType) {
				height += 44.0f;
			}*/
			inputAccessoryView.SetSize(new CGSize(DeviceInfo.ScreenBounds.Width, height));
		}

		private void RespaceTitleView() {
			titleStackView.Spacing = DeviceInfo.IsPortrait ? 2.0f : 0.0f.iPhoneX(2.0f);
		}

		private void RecountTableInsets() {
			nfloat top = -DeviceInfo.SafeAreaInsets.Bottom,
				bottom = -DeviceInfo.SafeAreaInsets.Bottom;
			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
				top += DeviceInfo.SafeAreaInsets.Top + NavigationController.NavigationBar.Frame.Height;
				bottom += -DeviceInfo.SafeAreaInsets.Top - NavigationController.NavigationBar.Frame.Height;
			}
			ChatTableView.ContentInset = new UIEdgeInsets(0.0f + top, 0.0f, 0.0f + ChatTableView.SpecialOffset + bottom, 0.0f);
			ChatTableView.ScrollIndicatorInsets = new UIEdgeInsets(0.0f + top, 0.0f, 0.0f + ChatTableView.SpecialOffset + bottom, 0.0f);
//			this.View.LayoutIfNeeded();
		}

		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator) {
			//holy shit, Apple, really?
			nfloat? offsetY = null;
			CGPoint? contentOffset = null;
			var safeAreaInsets = DeviceInfo.SafeAreaInsets;
			if (DeviceInfo.iPhoneX && toSize.Width > toSize.Height) {
				offsetY = -safeAreaInsets.Top;//NavigationController.NavigationBar.Frame.Height;
			} else if (!DeviceInfo.iPhoneX && toSize.Width < toSize.Height) {
				offsetY = NavigationController.NavigationBar.Frame.Height;
			}
			base.ViewWillTransitionToSize(toSize, coordinator);
			void setContentOffset() {
				offsetY ??= DeviceInfo.iPhoneX ? DeviceInfo.SafeAreaInsets.Top : -NavigationController.NavigationBar.Frame.Height;
				contentOffset ??= ChatTableView.ContentOffset;//-NavigationController.NavigationBar.Frame.Height;
				ChatTableView.ContentOffset = new CGPoint(contentOffset.Value.X, contentOffset.Value.Y + DeviceInfo.SafeAreaInsets.Bottom - safeAreaInsets.Bottom + offsetY.Value);
			}
            void resize() {
                ResizeInputAccessoryView();
                RespaceTitleView();
                RecountTableInsets();
            }
			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
				coordinator.AnimateAlongsideTransition((_) => {
					setContentOffset();
				}, (_) => {
					setContentOffset();
					resize();
				});
			} else {
				resize();
			}
		}

		public override void ViewSafeAreaInsetsDidChange() {
			base.ViewSafeAreaInsetsDidChange();
			if (!UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
				ResizeInputAccessoryView();
				RecountTableInsets();
			}
		}

		protected override void KeyboardWillShowNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			BeginKeyboardFrame = beginKeyboardFrame;
			EndKeyboardFrame = endKeyboardFrame;
			UIView.Animate(duration, 0.0, animationOptions, () => {
				ViewBottomConstraint.Constant = endKeyboardFrame.Height - ChatTableView.SpecialOffset;
				this.View.LayoutIfNeeded();
			}, null);
		}

		protected override void KeyboardWillHideNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			BeginKeyboardFrame = beginKeyboardFrame;
			EndKeyboardFrame = endKeyboardFrame;
			UIView.Animate(duration, 0.0, animationOptions, () => {
				ViewBottomConstraint.Constant = endKeyboardFrame.Height - ChatTableView.SpecialOffset;
				this.View.LayoutIfNeeded();
			}, null);
		}

		public override void TouchesBegan(NSSet touches, UIEvent evt) {
			base.TouchesBegan(touches, evt);
			this.View.EndEditing(true);
		}
	}

	public class InputAccessoryView : UIView {
		private NSLayoutConstraint leftConstraint, rightConstraint;
		private CGSize intrinsicContentSize = CGSize.Empty;
		public override CGSize IntrinsicContentSize => intrinsicContentSize;
		public void SetSize(CGSize size) {
			if (leftConstraint != null) {
				leftConstraint.Constant = DeviceInfo.SafeAreaInsets.Left;
			}
			if (rightConstraint != null) {
				rightConstraint.Constant = -DeviceInfo.SafeAreaInsets.Right;
			}
			intrinsicContentSize = new CGSize(size.Width, size.Height/* + DeviceInfo.SafeAreaBottom*/);
			InvalidateIntrinsicContentSize();
		}
		public void AddAccessoryView(UIView view) {
			this.AddSubview(view);
			(leftConstraint = view.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 0.0f)).Active = true;
			(rightConstraint = view.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, 0.0f)).Active = true;
			view.TopAnchor.ConstraintEqualTo(this.TopAnchor, 0.0f).Active = true;
			view.BottomAnchor.ConstraintEqualTo(this.LayoutMarginsGuide.BottomAnchor, 0.0f).Active = true;
			view.TranslatesAutoresizingMaskIntoConstraints = false;
		}
	}
}
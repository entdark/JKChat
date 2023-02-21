using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

using CoreGraphics;

using Foundation;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Controls;
using JKChat.iOS.Helpers;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

namespace JKChat.iOS.Views.Chat {
	[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = true)]
	public partial class ChatViewController : BaseViewController<ChatViewModel>, IUIGestureRecognizerDelegate {
		private UIView statusView;
		private UILabel statusLabel, titleLabel;
		private UIStackView titleStackView;
		private readonly InputAccessoryView inputAccessoryView;
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

		public override bool CanBecomeFirstResponder => true;//appeared;
		public override UIView InputAccessoryView => inputAccessoryView;//MessageView;

		protected override Task<bool> BackButtonClick => ViewModel?.OfferDisconnect();

		public ChatViewController() : base("ChatViewController", null) {
			HandleKeyboard = true;
			HidesBottomBarWhenPushed = true;
			inputAccessoryView = new InputAccessoryView(this);
		}

		public override void DidReceiveMemoryWarning() {
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();

			// Release any cached data, images, etc that aren't in use.
		}

		public override void LoadView() {
			base.LoadView();
			itemTappedStopwatch.Start();

			inputAccessoryView.AutoresizingMask = UIViewAutoresizing.All;
			MessageView.RemoveFromSuperview();
			inputAccessoryView.AddAccessoryView(MessageView);
			ChatTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive;
			ChatTableView.KeyboardViewController = this;
			ChatTableView.RowHeight = UITableView.AutomaticDimension;
			ChatTableView.EstimatedRowHeight = UITableView.AutomaticDimension;
			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
			{
				ChatTableView.InsetsLayoutMarginsFromSafeArea = false;
				ChatTableView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				ChatTableView.AutomaticallyAdjustsScrollIndicatorInsets = false;
			}
			EdgesForExtendedLayout = UIRectEdge.None;
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
							ViewModel.ItemClickCommand?.Execute(lastTappedItem);
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
						ViewModel.CopyCommand?.Execute(lastTappedItem);
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
			var source = new ChatTableViewSource(ChatTableView) {
				ViewControllerWithKeyboard = this,
				ViewBottomConstraint = ViewBottomConstraint
			};
			ChatTableView.Source = source;

			using var set = this.CreateBindingSet();
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
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			ResizeInputAccessoryView();
			RespaceTitleView();
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
				image = UIImage.FromBundle("ChatTypeCommon");
				break;
			case ChatType.Team:
				image = UIImage.FromBundle("ChatTypeTeam");
				break;
			case ChatType.Private:
				image = UIImage.FromBundle("ChatTypePrivate");
				break;
			}
			ChatTypeButton.SetImage(image, UIControlState.Normal);
		}

		private void ResizeInputAccessoryView() {
			float height = 44.0f;
			inputAccessoryView.SetSize(new CGSize(DeviceInfo.ScreenBounds.Width, height));
		}

		private void RespaceTitleView() {
			NavigationItem.TitleView = null;
			NavigationItem.TitleView = titleStackView;
			titleStackView.Spacing = DeviceInfo.IsPortrait ? 2.0f : 0.0f;
		}

		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator) {
			//holy shit, Apple, really?
			if (false||UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background) {
				base.ViewWillTransitionToSize(toSize, coordinator);
				return;
			}
			nfloat? offsetY = null;
			CGPoint? contentOffset = null;
			var safeAreaInsets = DeviceInfo.SafeAreaInsets;
			if (DeviceInfo.iPhoneX && toSize.Width > toSize.Height) {
				offsetY = -safeAreaInsets.Top;//NavigationBarFrame.Height;
			} else if (!DeviceInfo.iPhoneX && toSize.Width < toSize.Height) {
				offsetY = NavigationBarFrame.Height;
			}
			base.ViewWillTransitionToSize(toSize, coordinator);
			void setContentOffset() {
				offsetY ??= DeviceInfo.iPhoneX ? DeviceInfo.SafeAreaInsets.Top : -NavigationBarFrame.Height;
				contentOffset ??= ChatTableView.ContentOffset;//-NavigationBarFrame.Frame.Height;
				ChatTableView.ContentOffset = new CGPoint(contentOffset.Value.X, contentOffset.Value.Y + DeviceInfo.SafeAreaInsets.Bottom - safeAreaInsets.Bottom + offsetY.Value);
			}
			void resize() {
				ResizeInputAccessoryView();
				RespaceTitleView();
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
			nfloat endKeyboardHeight = /*DeviceInfo.SafeAreaInsets.Bottom+*/InputAccessoryView.Frame.Height;
			nfloat dy = endKeyboardFrame.Height-endKeyboardHeight;
			endKeyboardFrame = new CGRect(endKeyboardFrame.X, endKeyboardFrame.Y+dy, endKeyboardFrame.Width, endKeyboardHeight);
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
		private readonly UIViewController parentViewController;
		private NSLayoutConstraint leftConstraint, rightConstraint, bgBottomConstraint, bgRightConstraint;
		private CGSize intrinsicContentSize = CGSize.Empty;
		public override CGSize IntrinsicContentSize => intrinsicContentSize;
		public InputAccessoryView(UIViewController parentViewController) {
			this.parentViewController = parentViewController;
		}
		public void SetSize(CGSize size) {
			BackgroundColor = DeviceInfo.IsCollapsed ? Theme.Color.NavigationBar : UIColor.Clear;
			if (leftConstraint != null) {
				if (DeviceInfo.IsCollapsed) {
					leftConstraint.Constant = DeviceInfo.SafeAreaInsets.Left;
				} else {
					leftConstraint.Constant = DeviceInfo.SafeAreaInsets.Left + DeviceInfo.ScreenBounds.Width - parentViewController.View.Frame.Width - DeviceInfo.SafeAreaInsets.Right;
				}
			}
			if (rightConstraint != null) {
				rightConstraint.Constant = -DeviceInfo.SafeAreaInsets.Right;
			}
			if (bgBottomConstraint != null) {
				bgBottomConstraint.Constant = DeviceInfo.SafeAreaInsets.Bottom;
			}
			if (bgRightConstraint != null) {
				bgRightConstraint.Constant = DeviceInfo.SafeAreaInsets.Right;
			}
			intrinsicContentSize = new CGSize(size.Width, size.Height/* + DeviceInfo.SafeAreaBottom*/);
			InvalidateIntrinsicContentSize();
		}
		public void AddAccessoryView(UIView view) {
			BackgroundColor = DeviceInfo.IsCollapsed ? Theme.Color.NavigationBar : UIColor.Clear;

			var backgroundView = new UIView() {
				BackgroundColor = view.BackgroundColor,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			view.ClipsToBounds = false;
			view.InsertSubview(backgroundView, 0);
			backgroundView.LeadingAnchor.ConstraintEqualTo(view.LeadingAnchor, 0.0f).Active = true;
			(bgRightConstraint = backgroundView.TrailingAnchor.ConstraintEqualTo(view.TrailingAnchor, DeviceInfo.SafeAreaInsets.Right)).Active = true;
			backgroundView.TopAnchor.ConstraintEqualTo(view.TopAnchor, 0.0f).Active = true;
			(bgBottomConstraint = backgroundView.BottomAnchor.ConstraintEqualTo(view.BottomAnchor, DeviceInfo.SafeAreaInsets.Bottom)).Active = true;

			this.AddSubview(view);
			(leftConstraint = view.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 0.0f)).Active = true;
			(rightConstraint = view.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, 0.0f)).Active = true;
			view.TopAnchor.ConstraintEqualTo(this.TopAnchor, 0.0f).Active = true;
			view.BottomAnchor.ConstraintEqualTo(this.LayoutMarginsGuide.BottomAnchor, 0.0f).Active = true;
			view.TranslatesAutoresizingMaskIntoConstraints = false;
		}

		public override UIView HitTest(CGPoint point, UIEvent uievent) {
			if (point.X < leftConstraint.Constant) {
				return null;
			}
			return base.HitTest(point, uievent);
		}
	}
}
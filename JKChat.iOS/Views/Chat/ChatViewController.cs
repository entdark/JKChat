using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

using CoreAnimation;

using CoreGraphics;

using Foundation;
using JKChat.Core.Helpers;
using JKChat.Core.Models;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Controls;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using Microsoft.Maui.ApplicationModel;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.Chat {
	[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = true)]
	public partial class ChatViewController : BaseViewController<ChatViewModel>, IUIGestureRecognizerDelegate {
		private UIImageView statusImageView;
		private UILabel statusLabel, titleLabel;
		private UIStackView titleStackView;
		private readonly InputAccessoryView inputAccessoryView;
		private readonly Stopwatch itemTappedStopwatch = new Stopwatch();
		private readonly Timer itemTappedTimer = new Timer(500.0) {
			AutoReset = false
		};
		private CGPoint lastTappedPoint = CGPoint.Empty;
		private long lastTappedTime = 0L;
		private ChatItemVM lastTappedItem = null;
		private UIBarButtonItem moreButtonItem, minimapButtonItem;
		private UIButton mapProgressPercentButton;
		private bool viewAppeared = false;

		public override string Title {
			get => base.Title;
			set => base.Title = null;
		}

		private string message;
		public string Message {
			get => message;
			set {
				if (string.IsNullOrEmpty(message) != string.IsNullOrEmpty(value)) {
					AnimateButton(SendButton, !string.IsNullOrEmpty(value));
					AnimateButton(CommandsButton, string.IsNullOrEmpty(value));
				}
				message = value;
			}
		}

		private bool selectingChatType;
		public bool SelectingChatType {
			get => selectingChatType;
			set {
				selectingChatType = value;
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

		private int commandItemsCount;
		public int CommandItemsCount {
			get => commandItemsCount;
			set {
				commandItemsCount = value;
				UpdateCommandsTableView();
			}
		}

		public bool IsFavourite { get; set; }

		private bool commandSetAutomatically;
		public bool CommandSetAutomatically {
			get => commandSetAutomatically;
			set {
				commandSetAutomatically = value;
				if (commandSetAutomatically) {
					ViewModel.CommandSetAutomatically = false;
					MessageTextView.BecomeFirstResponder();
				}
			}
		}

		private string scores;
		public string Scores {
			get => scores;
			set {
				scores = value;
				ScoresLabel.AttributedText = ColourTextValueConverter.Convert(scores);
				this.View.LayoutIfNeeded();
				ChatTableView.ContentInset = new(InfoView.Frame.Height, 0.0f, ChatTableView.SpecialOffset, 0.0f);
				ChatTableView.ScrollIndicatorInsets = new(InfoView.Frame.Height, 0.0f, ChatTableView.SpecialOffset, 0.0f);
			}
		}

		private MapData mapData;
		public MapData MapData {
			get => mapData;
			set {
				mapData = value;
				UpdateButtonItems();
			}
		}

		private bool mapFocused;
		public bool MapFocused {
			get => mapFocused;
			set {
				if (viewAppeared && mapFocused != value) {
					SwapMapAndChat(value, true);
				}
				mapFocused = value;
				UpdateButtonItems();
			}
		}

		private float mapLoadingProgress;
		public float MapLoadingProgress {
			get => mapLoadingProgress;
			set {
				mapLoadingProgress = value;
				UpdateButtonItems();
			}
		}

		private string centerPrint;
		public string CenterPrint {
			get => centerPrint;
			set {
				if (centerPrint != value) {
					centerPrint = value;
					UpdateCenterPrint();
				}
			}
		}

		private bool showCenterPrint;
		public bool ShowCenterPrint {
			get => showCenterPrint;
			set {
				if (showCenterPrint != value) {
					AnimateCenterPrint(CenterPrintView.Superview, value);
				}
				showCenterPrint = value;
			}
		}

		public override bool CanBecomeFirstResponder => !DeviceInfo.IsRunningOnMacOS;
		public override UIView InputAccessoryView => inputAccessoryView;

		public ChatViewController() : base("ChatViewController", null) {
			HandleKeyboard = true;
			HidesBottomBarWhenPushed = true;
			inputAccessoryView = new InputAccessoryView(this);
		}

		public override void LoadView() {
			base.LoadView();
			itemTappedStopwatch.Start();

			inputAccessoryView.AutoresizingMask = UIViewAutoresizing.All;
			if (!DeviceInfo.IsRunningOnMacOS) {
				MessageView.RemoveFromSuperview();
				inputAccessoryView.AddAccessoryView(MessageView);
				ChatTableViewBottomConstraint.Active = true;
				ChatTableViewBottomToMessageViewTopConstraint.Active = false;
				MessageToolbar.Hidden = true;
				CommandsTableViewBottomConstraint.Constant = -ChatTableView.SpecialOffset;
				ChatTableViewToMinimapViewBottomConstraint.Constant = ChatTableView.SpecialOffset;
			} else {
				ChatTableViewBottomConstraint.Active = false;
				ChatTableViewBottomToMessageViewTopConstraint.Active = true;
				CommandsTableViewBottomConstraint.Constant = 0.0f;
				ChatTableViewToMinimapViewBottomConstraint.Constant = 0.0f;
			}
			ChatTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive;
			ChatTableView.KeyboardViewController = this;
			ChatTableView.RowHeight = UITableView.AutomaticDimension;
			ChatTableView.EstimatedRowHeight = UITableView.AutomaticDimension;
			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0)) {
				ChatTableView.InsetsLayoutMarginsFromSafeArea = false;
				ChatTableView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0)) {
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
					if (!ChatTableView.StartedDragging && dx < deltaTappedPos && dy < deltaTappedPos
						&& lastTappedTime != 0L && lastTappedPoint != CGPoint.Empty && currentPoint != CGPoint.Empty) {
						if (lastTappedItem != null) {
							if (dt >= 500L) {
//								ViewModel.LongPressCommand?.Execute(lastTappedItem);
							} else {
								ViewModel.ItemClickCommand?.Execute(lastTappedItem);
							}
						}
						MessageTextView.ResignFirstResponder();
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
			ChatTableViewBottomConstraint.Constant = 44.0f + DeviceInfo.SafeAreaInsets.Bottom - ChatTableView.SpecialOffset;

			MessageTextView.Placeholder = "Write a message...";
			MessageTextView.PlaceholderColor = UIColor.TertiaryLabel;
			MessageTextView.PlaceholderFont = UIFont.PreferredBody;
			MessageTextView.TextContainerInset = new UIEdgeInsets(11.0f, 0.0f, 11.0f, 0.0f);
			MessageTextView.MaxLength = 149;

			SendButton.Transform = CGAffineTransform.MakeScale(0.0f, 0.0f);

			titleLabel = new UILabel() {
				TextColor = UIColor.Label,
				TextAlignment = UITextAlignment.Center,
				Font = UIFont.FromDescriptor(UIFontDescriptor.GetPreferredDescriptorForTextStyle(UIFontTextStyle.Body).CreateWithTraits(UIFontDescriptorSymbolicTraits.Bold), 0.0f),
			};

			statusImageView = new UIImageView(Theme.Image.CircleFill_Caption1Small);

			statusLabel = new UILabel() {
				TextColor = UIColor.SecondaryLabel,
				TextAlignment = UITextAlignment.Left,
				Font = UIFont.PreferredCaption1
			};

			var statusStackView = new UIStackView(new UIView[] { statusImageView, statusLabel }) {
				Axis = UILayoutConstraintAxis.Horizontal,
				Spacing = 4.0f,
				Alignment = UIStackViewAlignment.Center
			};

			titleStackView = new UIStackView(new UIView[] { titleLabel, statusStackView }) {
				Axis = UILayoutConstraintAxis.Vertical,
				Alignment = UIStackViewAlignment.Center
			};
			titleStackView.AddGestureRecognizer(new UITapGestureRecognizer(() => {
				ViewModel.ServerInfoCommand?.Execute();
			}));

			NavigationItem.TitleView = titleStackView;

			InfoView.AddGestureRecognizer(new UITapGestureRecognizer(() => {
				ViewModel.ServerInfoCommand?.Execute();
			}));

			CenterPrintView.Superview.Hidden = true;
			CenterPrintLabel.Text = string.Empty;

			var tap = new UITapGestureRecognizer(() => {
				MessageTextView.ResignFirstResponder();
			}) {
				CancelsTouchesInView = false
			};
			MinimapView.AddGestureRecognizer(tap);
			
			moreButtonItem = new UIBarButtonItem(Theme.Image.EllipsisCircle, null);
			var uncachedAction = UIDeferredMenuElement.CreateUncached(completion => {
				var disconnectAction = UIAction.Create("Disconnect & exit", Theme.Image.DoorLeftHandOpen, null, action => {
					ViewModel.DisconnectCommand?.Execute();
				});
				disconnectAction.Attributes = UIMenuElementAttributes.Destructive;
				bool downloadMapAction = !MapLoadingProgress.IsProgressActive() && MapData == null;
				var menuElements = new List<UIMenuElement>(4 + (downloadMapAction ? 1 : 0)) {
					UIAction.Create(IsFavourite ? "Remove from favourites" : "Add to favourites", IsFavourite ? Theme.Image.StarFill : Theme.Image.Star, null, action => {
						ViewModel.FavouriteCommand?.Execute();
					}),
					UIAction.Create("Share", Theme.Image.SquareAndArrowUp, null, action => {
						ViewModel.ShareCommand?.Execute();
					}),
					UIAction.Create("Info", Theme.Image.InfoCircle, null, action => {
						ViewModel.ServerInfoCommand?.Execute();
					}),
					disconnectAction
				};
				if (downloadMapAction) {
					menuElements.Insert(3, UIAction.Create("Download & generate minimap", Theme.Image.MapCircle, null, action => {
						ViewModel.MapCommand?.Execute();
					}));
				}
				completion(menuElements.ToArray());
			});
			moreButtonItem.Menu = UIMenu.Create(new[] { uncachedAction });
			mapProgressPercentButton = new UIButton(UIButtonType.System) {
				Frame = new(0.0f, 0.0f, 44.0f, 44.0f)
			};

			minimapButtonItem = new UIBarButtonItem(MapFocused ? Theme.Image.MapCircleFill : Theme.Image.MapCircle, UIBarButtonItemStyle.Plain, (sender, ev) => {
				ViewModel.MapCommand?.Execute();
			});
		}

		[Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
		private bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer1, UIGestureRecognizer gestureRecognizer2) {
			return true;
		}

		private readonly CATransition centerPrintTransition = new() {
			TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.Default),
			Type = CAAnimation.TransitionFade,
			Duration = 0.500
		};
		private readonly UILabel centerPrintLabel = new() {
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.PreferredBody,
			LineBreakMode = UILineBreakMode.WordWrap,
			Lines = 0
		};
		private void UpdateCenterPrint() {
			var text = ColourTextValueConverter.Convert(CenterPrint);
			var parentFrame = CenterPrintView.Superview.Frame;
			centerPrintLabel.Frame = new(0.0f, 0.0f, DeviceInfo.ScreenBounds.Width-DeviceInfo.SafeAreaInsets.Left-DeviceInfo.SafeAreaInsets.Right-64.0f, 0.0f);
			centerPrintLabel.AttributedText = text;
			centerPrintLabel.SizeToFit();
			var countedLabelFrame = centerPrintLabel.Frame;
			CGRect viewFrame = new(parentFrame.GetMidX()-countedLabelFrame.GetMidX()-32.0f, 0.0f, countedLabelFrame.Width+32.0f, countedLabelFrame.Height+24.0f);
			UIView.Animate(0.200, 0.0f, UIViewAnimationOptions.CurveEaseOut, () => {
				CenterPrintView.Frame = viewFrame;
			}, null);
			CenterPrintLabel.Layer.AddAnimation(centerPrintTransition, CAAnimation.TransitionFade.ToString());
			CenterPrintLabel.AttributedText = text;
		}

		private void UpdateButtonItems(bool set = false) {
			if (MapLoadingProgress.IsProgressActive()) {
				minimapButtonItem.Image = null;
				minimapButtonItem.CustomView ??= mapProgressPercentButton;
				UIView.PerformWithoutAnimation(() => {
					mapProgressPercentButton.SetTitle(MapLoadingProgress.ToPercentString(), UIControlState.Normal);
					mapProgressPercentButton.LayoutIfNeeded();
				});
				if (set || NavigationItem.RightBarButtonItems?.Length == 1) {
					NavigationItem.SetRightBarButtonItems(new []{ moreButtonItem, minimapButtonItem }, true);
				}
			} else {
				if (MapData != null) {
					minimapButtonItem.Image = MapFocused ? Theme.Image.MapCircleFill : Theme.Image.MapCircle;
					minimapButtonItem.CustomView = null;
					if (set || NavigationItem.RightBarButtonItems?.Length == 1) {
						NavigationItem.SetRightBarButtonItems(new []{ moreButtonItem, minimapButtonItem }, true);
					}
				} else {
					minimapButtonItem.Image = null;
					if (set || NavigationItem.RightBarButtonItems?.Length == 2) {
						NavigationItem.SetRightBarButtonItems(new []{ moreButtonItem }, true);
					}
				}
			}
		}

		#region View lifecycle

		public override void ViewDidLoad() {
			base.ViewDidLoad();
			var chatSource = new ChatTableViewSource(ChatTableView) {
				ViewControllerWithKeyboard = this
			};
			var commandsSource = new CommandsViewSource(CommandsTableView);

			using var set = this.CreateBindingSet();
			set.Bind(chatSource).For(s => s.ItemsSource).To(vm => vm.Items);
//			set.Bind(chatSource).For(s => s.SelectionChangedCommand).To(vm => vm.SelectionChangedCommand);
			set.Bind(commandsSource).For(s => s.ItemsSource).To(vm => vm.CommandItems);
			set.Bind(commandsSource).For(s => s.SelectionChangedCommand).To(vm => vm.CommandItemClickCommand);
			set.Bind(this).For(v => v.CommandItemsCount).To(vm => vm.CommandItemsCount);
			set.Bind(CommandsButton).To(vm => vm.StartCommandCommand);
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
			set.Bind(statusLabel).For(v => v.Text).To("If(Players, Format('{0}, {1}', Status, Players), Status)");
			set.Bind(statusLabel).For(v => v.TextColor).To(vm => vm.Status).WithDictionaryConversion(new Dictionary<ConnectionStatus, UIColor>() {
				[ConnectionStatus.Connected] = UIColor.Label
			}, UIColor.SecondaryLabel);
			set.Bind(statusImageView).For(v => v.TintColor).To(vm => vm.Status).WithConversion("ConnectionColor");
			set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
			set.Bind(this).For(v => v.CommandSetAutomatically).To(vm => vm.CommandSetAutomatically);
			set.Bind(TimerLabel).For(v => v.AttributedText).To(vm => vm.Timer).WithConversion("ColourText");
			set.Bind(this).For(v => v.Scores).To(vm => vm.Scores);
			set.Bind(this).For(v => v.CenterPrint).To(vm => vm.CenterPrint);
			set.Bind(this).For(v => v.ShowCenterPrint).To(vm => vm.ShowCenterPrint);
			set.Bind(MinimapView).For(v => v.Entities).To(vm => vm.Entities);
			set.Bind(MinimapView).For(v => v.MapData).To(vm => vm.MapData);
			set.Bind(this).For(v => v.MapData).To(vm => vm.MapData);
			set.Bind(this).For(v => v.MapFocused).To(vm => vm.MapFocused);
			set.Bind(MapLoadingProgressView).For(v => v.Progress).To(vm => vm.MapLoadingProgress);
			set.Bind(MapLoadingProgressView).For("Visibility").To(vm => vm.MapLoadingProgress).WithConversion("Visibility");
			set.Bind(this).For(v => v.MapLoadingProgress).To(vm => vm.MapLoadingProgress);
			set.Bind(mapProgressPercentButton).To(vm => vm.MapCommand);

			InfoView.Alpha = 0.0f;
			MinimapView.Alpha = 0.0f;
			UpdateCommandsTableView();
		}

		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			RespaceTitleView();

			var appearance = new UINavigationBarAppearance();
			appearance.ConfigureWithDefaultBackground();
			if (!UIAccessibility.IsReduceTransparencyEnabled)
				appearance.BackgroundEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial);
			else
				appearance.BackgroundColor = UIColor.SystemBackground;
			NavigationController.NavigationBar.BarTintColor = UIColor.SystemBackground;
			NavigationController.NavigationBar.StandardAppearance = appearance;
			NavigationController.NavigationBar.ScrollEdgeAppearance = appearance;
			NavigationController.NavigationBar.CompactAppearance = appearance;
			NavigationController.NavigationBar.CompactScrollEdgeAppearance = appearance;
//HACK: to blur NavigationBar since it starts blurring after scrolling
			ChatTableView.SetContentOffset(new CGPoint(0.0f, ChatTableView.SpecialOffset-1.0f), false);

			UpdateButtonItems(true);
		}

		public override void ViewDidAppear(bool animated) {
			base.ViewDidAppear(animated);
			ChatTableView.SetContentOffset(new CGPoint(0.0f, ChatTableView.SpecialOffset), true);
			BecomeFirstResponder();

			InfoView.SetNeedsUpdateConstraints();
			InfoView.UpdateConstraints();

			Task.Run(async () => {
				await Task.Delay(200);
				await MainThread.InvokeOnMainThreadAsync(() => {
					UIView.Animate(0.200, () => {
						InfoView.Alpha = 1.0f;
					});
					SwapMapAndChat(MapFocused, true);
				});
			});
			viewAppeared = true;
		}

		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
		}

		public override void ViewDidDisappear(bool animated) {
			base.ViewDidDisappear(animated);
			viewAppeared = false;
		}

		#endregion

		private static void AnimateButton(UIButton button, bool show) {
			if (show) {
				button.Hidden = false;
				UIView.Animate(0.200, () => {
					button.Transform = CGAffineTransform.MakeScale(1.0f, 1.0f);
				});
			} else {
				UIView.Animate(0.200, () => {
					//setting 0.0f, 0.0f causes the bug where animation happens instantly
					button.Transform = CGAffineTransform.MakeScale(float.Epsilon, float.Epsilon);
				}, () => {
					button.Hidden = true;
				});
			}
		}

		private void SwapMapAndChat(bool mapFocused, bool animated = true) {
			float minimapAlpha = mapFocused ? 1.0f : 0.3f,
				chatAlpha = mapFocused ? 0.3f : 1.0f;
			int i = Array.IndexOf(MinimapView.Superview.Subviews, MinimapView);
			int j = Array.IndexOf(ChatTableView.Superview.Subviews, ChatTableView);
			if ((i < j && mapFocused) || (i > j && !mapFocused)) {
				MinimapView.Superview.ExchangeSubview(i, j);
			}
			if (!animated) {
				MinimapView.Alpha = minimapAlpha;
				ChatTableView.Alpha = chatAlpha;
				return;
			}
			UIView.Animate(0.200, () => {
				MinimapView.Alpha = minimapAlpha;
				ChatTableView.Alpha = chatAlpha;
			});
		}

		private static void AnimateCenterPrint(UIView view, bool show) {
			if (show) {
				view.Hidden = false;
				UIView.Animate(0.200, () => {
					view.Alpha = 1.0f;
				});
			} else {
				UIView.Animate(0.200, () => {
					//setting 0.0f, 0.0f causes the bug where animation happens instantly
					view.Alpha = 0.0f;
				}, () => {
					view.Hidden = true;
				});
			}
		}

		private void SetChatTypeImage() {
			UIImage image;
			UIColor tintColor;
			switch (ChatType) {
			default:
			case ChatType.Common:
				image = Theme.Image.Person3Fill_Small;
				tintColor = UIColor.FromRGB(0, 255, 0);
				break;
			case ChatType.Team:
				image = Theme.Image.Person2Fill_Small;
				tintColor = UIColor.FromRGB(0, 255, 255);
				break;
			case ChatType.Private:
				image = Theme.Image.PersonFill_Small;
				tintColor = UIColor.FromRGB(255, 0, 255);
				break;
			}
			ChatTypeButton.SetImage(image, UIControlState.Normal);
			ChatTypeButton.TintColor = tintColor;
		}

		private void UpdateCommandsTableView() {
			if (CommandItemsCount > 0) {
				CommandsTableView.Hidden = false;
				CommandsTableViewHeightConstraint.Constant = Math.Min(CommandItemsCount*44.0f, 242.0f);
				this.View.LayoutIfNeeded();
			} else {
				CommandsTableView.Hidden = true;
			}
		}

		private void RespaceTitleView() {
			NavigationItem.TitleView = null;
			NavigationItem.TitleView = titleStackView;
		}

		private void RecountAllCellHeights(CGSize newSize) {
			ChatTableView.RecountAllCellHeights(newSize);
		}

		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator) {
			base.ViewWillTransitionToSize(toSize, coordinator);
			coordinator.AnimateAlongsideTransition(animateContext => {
//				RecountAllCellHeights(toSize);
				UpdateCenterPrint();
			},
			completionContext => {
				RecountAllCellHeights(toSize);
			});
			RespaceTitleView();
		}

		protected override void KeyboardWillShowNotification(NSNotification notification) {
			notification.GetKeyboardUserInfo(out double duration, out UIViewAnimationOptions animationOptions, out CGRect endKeyboardFrame, out CGRect beginKeyboardFrame);
			BeginKeyboardFrame = beginKeyboardFrame;
			EndKeyboardFrame = endKeyboardFrame;
			UIView.Animate(duration, 0.0, animationOptions, () => {
				ChatTableViewBottomConstraint.Constant = endKeyboardFrame.Height - ChatTableView.SpecialOffset;
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
				ChatTableViewBottomConstraint.Constant = endKeyboardFrame.Height - ChatTableView.SpecialOffset;
				this.View.LayoutIfNeeded();
			}, null);
		}

		public override void TouchesBegan(NSSet touches, UIEvent evt) {
			base.TouchesBegan(touches, evt);
			this.View.EndEditing(true);
		}

		private class CommandsViewSource : MvxStandardTableViewSource {
			public CommandsViewSource(UITableView tableView) : base(tableView, CommandViewCell.Key) {
				tableView.RegisterClassForCellReuse(typeof(CommandViewCell), CommandViewCell.Key);
				tableView.Source = this;
				DeselectAutomatically = true;
			}

			public override void ReloadTableData() {
				base.ReloadTableData();
				if (ItemsSource is IList listSource && listSource.Count > 0) {
					TableView.ScrollToRow(NSIndexPath.FromRowSection(listSource.Count - 1, 0), UITableViewScrollPosition.Bottom, false);
				}
			}

			private class CommandViewCell : MvxTableViewCell {
				public static NSString Key = new(nameof(CommandViewCell));

				public CommandViewCell(NativeHandle handle) : base(handle) {
					SeparatorInset = new UIEdgeInsets(0.0f, 56.0f, 0.0f, 0.0f);
					this.DelayBind(() => {
						using var set = this.CreateBindingSet<CommandViewCell, string>();
						set.Bind(TextLabel).For(v => v.Text).To(".");
					});
				}
			}
		}
	}

	public class InputAccessoryView : UIView {
		private readonly UIViewController parentViewController;
		private NSLayoutConstraint leftConstraint, rightConstraint, bgBottomConstraint, bgRightConstraint, toolbarLeftConstraint, toolbarRightConstraint;
		private CGSize intrinsicContentSize = CGSize.Empty;
		public override CGSize IntrinsicContentSize => intrinsicContentSize;
		public InputAccessoryView(UIViewController parentViewController) {
			this.parentViewController = parentViewController;
		}
		public void SetSize(CGSize size) {
			intrinsicContentSize = new CGSize(size.Width, size.Height/* + DeviceInfo.SafeAreaBottom*/);
			InvalidateIntrinsicContentSize();
		}
		public override CGRect Bounds {
			get => base.Bounds;
			set {
				base.Bounds = value;
				UpdateInsets();
			}
		}
		private void UpdateInsets() {
//			BackgroundColor = DeviceInfo.IsCollapsed ? Theme.Color.Bar : UIColor.Clear;
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
			if (toolbarLeftConstraint != null) {
				if (DeviceInfo.IsCollapsed) {
					toolbarLeftConstraint.Constant = -DeviceInfo.SafeAreaInsets.Left;
				} else {
					toolbarLeftConstraint.Constant = DeviceInfo.SafeAreaInsets.Left + DeviceInfo.ScreenBounds.Width - parentViewController.View.Frame.Width - DeviceInfo.SafeAreaInsets.Right;
				}
			}
			if (toolbarRightConstraint != null) {
				toolbarRightConstraint.Constant = 0.0f;
			}
		}
		public void AddAccessoryView(UIView view) {
//			BackgroundColor = DeviceInfo.IsCollapsed ? Theme.Color.Bar : UIColor.Clear;
			ClipsToBounds = false;

			var backgroundView = new UIView() {
//				BackgroundColor = Theme.Color.Bar,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			view.ClipsToBounds = false;
			view.InsertSubview(backgroundView, 0);
			backgroundView.LeadingAnchor.ConstraintEqualTo(view.LeadingAnchor, 0.0f).Active = true;
			(bgRightConstraint = backgroundView.TrailingAnchor.ConstraintEqualTo(view.TrailingAnchor, DeviceInfo.SafeAreaInsets.Right)).Active = true;
			backgroundView.TopAnchor.ConstraintEqualTo(view.TopAnchor, 0.0f).Active = true;
			(bgBottomConstraint = backgroundView.BottomAnchor.ConstraintEqualTo(view.BottomAnchor, DeviceInfo.SafeAreaInsets.Bottom)).Active = true;

			var backgroundToolbar = new UIToolbar() {
				BarTintColor = UIColor.SystemBackground,
				BarStyle = UIBarStyle.Default,
				Translucent = true,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			this.AddSubview(backgroundToolbar);
			this.AddSubview(view);
			(toolbarLeftConstraint = backgroundToolbar.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, -DeviceInfo.SafeAreaInsets.Left)).Active = true;
			(toolbarRightConstraint = backgroundToolbar.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, 0.0f)).Active = true;
			backgroundToolbar.TopAnchor.ConstraintEqualTo(this.TopAnchor, 0.0f).Active = true;
			backgroundToolbar.HeightAnchor.ConstraintEqualTo(500.0f).Active = true;
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
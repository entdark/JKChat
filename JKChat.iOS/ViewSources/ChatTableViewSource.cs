﻿using System;
using System.Diagnostics;

using CoreGraphics;

using Foundation;

using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Controls;
using JKChat.iOS.Helpers;
using JKChat.iOS.Views.Base;
using JKChat.iOS.Views.Chat.Cells;

using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.ViewSources {
	public class ChatTableViewSource : MvxStandardTableViewSource {
		private CGRect initialKeyboardFrame;
		private CGPoint initialContentOffset;
		private nfloat dinsetY = nfloat.MinValue;
		public IKeyboardViewController ViewControllerWithKeyboard { get; set; }
		public NSLayoutConstraint ViewBottomConstraint { get; set; }
		protected bool NewDragging { get; set; }

		public ChatTableViewSource(UITableView tableView) : base(tableView) {
			Initialize(tableView);
		}

		private void Initialize(UITableView tableView) {
			tableView.RegisterNibForCellReuse(ChatMessageViewCell.Nib, ChatMessageViewCell.Key);
			tableView.RegisterNibForCellReuse(ChatInfoViewCell.Nib, ChatInfoViewCell.Key);
			this.UseAnimations = true;
			this.AddAnimation = UITableViewRowAnimation.Top;
			this.RemoveAnimation = UITableViewRowAnimation.Bottom;
		}
		private nfloat dyLast;
		public override void Scrolled(UIScrollView scrollView) {
//			Debug.WriteLine("Scrolled");
			bool dragging = NewDragging;
			var chatTableView = scrollView as ChatTableView;
			if (dragging && ViewControllerWithKeyboard.EndKeyboardFrame.Height > (ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Frame.Height) {
				if ((ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame.Y > initialKeyboardFrame.Y) {
					if (dinsetY == nfloat.MinValue) {
						dinsetY = chatTableView.ContentOffset.Y + chatTableView.ContentInset.Bottom + chatTableView.ExtraContentInset.Bottom;
						if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
							dinsetY += ((ViewControllerWithKeyboard as UIViewController)?.NavigationController?.NavigationBar?.Frame.Height ?? 44.0f) + DeviceInfo.SafeAreaInsets.Top;
						}
						initialContentOffset = scrollView.ContentOffset;
					}
					nfloat dy = dyLast = ((ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame ?? ViewControllerWithKeyboard.EndKeyboardFrame).Y - initialKeyboardFrame.Y/* + DeviceInfo.SafeAreaInsets.Bottom*/;
//					scrollView.SetContentOffset(new CGPoint(initialContentOffset.X, -initialContentOffset.Y-dinsetY), true);
//					scrollView.ContentInset = new UIEdgeInsets(scrollView.ContentInset.Top, scrollView.ContentInset.Left, initialKeyboardFrame.Height - dy - dinsetY, scrollView.ContentInset.Right);
//					scrollView.ScrollIndicatorInsets = new UIEdgeInsets(scrollView.ScrollIndicatorInsets.Top, scrollView.ScrollIndicatorInsets.Left, initialKeyboardFrame.Height - dy, scrollView.ScrollIndicatorInsets.Right);
//					ViewBottomConstraint.Constant = initialKeyboardFrame.Height - dy - dinsetY;
//					(ViewControllerWithKeyboard as UIViewController).View.LayoutIfNeeded();
				}
			}
		}

		public override void DraggingStarted(UIScrollView scrollView) {
			Debug.WriteLine("DraggingStarted");
			initialKeyboardFrame = (ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame ?? ViewControllerWithKeyboard.EndKeyboardFrame;
			NewDragging = true;
			if (scrollView is ChatTableView chatTableView) {
				chatTableView.StartedDragging = true;
			}
		}

		public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate) {
			Debug.WriteLine("DraggingEnded");
			NewDragging = false;
			initialKeyboardFrame = CGRect.Empty;
			dinsetY = nfloat.MinValue;
			if (!willDecelerate && scrollView is ChatTableView chatTableView) {
				chatTableView.ScrolledToBottom = ScrolledToBottom(chatTableView);
			}
		}

		public override void WillEndDragging(UIScrollView scrollView, CGPoint velocity, ref CGPoint targetContentOffset) {
			Debug.WriteLine("WillEndDragging");
			nfloat y;
			if (dinsetY >= 0.0f && dinsetY <= 176.0f && velocity.Y >= 0.0f) {
				nfloat height = DeviceInfo.ScreenBounds.Height - ViewControllerWithKeyboard.EndKeyboardFrame.Y;
				y = height + (scrollView as ChatTableView).ExtraContentInset.Bottom/* - DeviceInfo.SafeAreaInsets.Bottom*/;
				targetContentOffset = new CGPoint(scrollView.ContentOffset.X, -y-ChatTableView.SpecialOffset);
			} else if (dinsetY >= 0.0f && velocity.Y >= 0.0f) {
				y = targetContentOffset.Y;
				Debug.WriteLine($"y1: {y}");
				CGRect currentKeyboardFrame = (ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame ?? ViewControllerWithKeyboard.EndKeyboardFrame;
				Debug.WriteLine($"current: {currentKeyboardFrame}");
				Debug.WriteLine($"initial: {initialKeyboardFrame}");
				y -= (dyLast/* + DeviceInfo.SafeAreaInsets.Bottom*/);
				Debug.WriteLine($"y2: {y}");
				targetContentOffset = new CGPoint(scrollView.ContentOffset.X, y);
			} else {
				return;
			}
		}

		public override void DecelerationEnded(UIScrollView scrollView) {
			if (scrollView is ChatTableView chatTableView) {
				chatTableView.ScrolledToBottom = ScrolledToBottom(chatTableView);
			}
		}

		private bool ScrolledToBottom(ChatTableView chatTableView) {
			bool scrolledToBottom = (chatTableView.ContentOffset.Y + ChatTableView.SpecialOffset + chatTableView.ExtraContentInset.Bottom - DeviceInfo.SafeAreaInsets.Bottom) <= 0;
//			this.UseAnimations = scrolledToBottom;
			return scrolledToBottom;
		}

/*		public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath) {
			var item = ItemsSource.ElementAt(indexPath.Row) as ChatItemVM;
			var label = new UILabel(new CGRect(20.0f, 0.0f, DeviceInfo.ScreenBounds.Width - 40.0f, 0.0f)) {
				Font = Theme.Font.OCRAStd(14.0f),
				AttributedText = (NSAttributedString)new ColourTextValueConverter().Convert(item.PlayerName, typeof(NSAttributedString), null, CultureInfo.InvariantCulture),
				LineBreakMode = UILineBreakMode.TailTruncation,
				Lines = 1
			};
			label.SizeToFit();
			var height = label.Frame.Height;
			label = new UILabel(new CGRect(20.0f, 0.0f, DeviceInfo.ScreenBounds.Width - 40.0f, 0.0f)) {
				Font = Theme.Font.OCRAStd(14.0f),
				AttributedText = (NSAttributedString)new ColourTextValueConverter().Convert(item.PlayerName, typeof(NSAttributedString), null, CultureInfo.InvariantCulture),
				LineBreakMode = UILineBreakMode.TailTruncation,
				Lines = 0
			};
			label.SizeToFit();
			height += label.Frame.Height;
			height += 15.0f;
			return height;
		}

		public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) {
			return EstimatedHeight(tableView, indexPath);
		}*/

		protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item) {
			if (item is ChatMessageItemVM) {
				return tableView.DequeueReusableCell(ChatMessageViewCell.Key);
			} else if (item is ChatInfoItemVM) {
				return tableView.DequeueReusableCell(ChatInfoViewCell.Key);
			}
			return base.GetOrCreateCellFor(tableView, indexPath, item);
		}
	}
}
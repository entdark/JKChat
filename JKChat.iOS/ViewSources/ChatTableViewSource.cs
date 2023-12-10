using System.Collections.Specialized;
using System.Diagnostics;

using CoreGraphics;

using Foundation;

using JKChat.Core.ValueCombiners;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.iOS.Controls;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;
using JKChat.iOS.Views.Base;
using JKChat.iOS.Views.Chat.Cells;

using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.ViewSources {
	public class ChatTableViewSource : MvxStandardTableViewSource {
		private CGRect initialKeyboardFrame;
		private CGPoint initialContentOffset;
		private nfloat dinsetY = nfloat.MinValue;
		private bool dragging = false;

		public IKeyboardViewController ViewControllerWithKeyboard { get; set; }
		public NSLayoutConstraint ViewBottomConstraint { get; set; }

		public ChatTableViewSource(UITableView tableView) : base(tableView) {
			tableView.Source = this;
			tableView.RegisterNibForCellReuse(ChatMessageViewCell.Nib, ChatMessageViewCell.Key);
			tableView.RegisterNibForCellReuse(ChatInfoViewCell.Nib, ChatInfoViewCell.Key);
			this.UseAnimations = true;
			this.AddAnimation = UITableViewRowAnimation.Top;
			this.RemoveAnimation = UITableViewRowAnimation.Bottom;
		}
		private nfloat dyLast;
		public override void Scrolled(UIScrollView scrollView) {
//			Debug.WriteLine("Scrolled");
			var chatTableView = scrollView as ChatTableView;
			if (dragging && ViewControllerWithKeyboard.EndKeyboardFrame.Height > (ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Frame.Height) {
				if ((ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame.Y > initialKeyboardFrame.Y) {
					if (dinsetY == nfloat.MinValue) {
						dinsetY = chatTableView.ContentOffset.Y + chatTableView.ContentInset.Bottom;
						if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
							dinsetY += ((ViewControllerWithKeyboard as UIViewController)?.NavigationController?.NavigationBar?.Frame.Height ?? 44.0f) + DeviceInfo.SafeAreaInsets.Top;
						}
						initialContentOffset = scrollView.ContentOffset;
					}
					nfloat dy = dyLast = ((ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame ?? ViewControllerWithKeyboard.EndKeyboardFrame).Y - initialKeyboardFrame.Y;
				}
			}
		}

		public override void DraggingStarted(UIScrollView scrollView) {
			Debug.WriteLine("DraggingStarted");
			initialKeyboardFrame = (ViewControllerWithKeyboard as UIViewController)?.InputAccessoryView?.Superview?.Frame ?? ViewControllerWithKeyboard.EndKeyboardFrame;
			dragging = true;
			if (scrollView is ChatTableView chatTableView) {
				chatTableView.StartedDragging = true;
			}
		}

		public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate) {
			Debug.WriteLine("DraggingEnded");
			dragging = false;
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
				targetContentOffset = new CGPoint(scrollView.ContentOffset.X, -ChatTableView.SpecialOffset);
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
			bool scrolledToBottom = (chatTableView.ContentOffset.Y + ChatTableView.SpecialOffset - DeviceInfo.SafeAreaInsets.Bottom) <= 0;
//			this.UseAnimations = scrolledToBottom;
			return scrolledToBottom;
		}

		protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item) {
			if (item is ChatMessageItemVM) {
				return tableView.DequeueReusableCell(ChatMessageViewCell.Key);
			} else if (item is ChatInfoItemVM) {
				return tableView.DequeueReusableCell(ChatInfoViewCell.Key);
			}
			return base.GetOrCreateCellFor(tableView, indexPath, item);
		}

		public override void ReloadTableData() {
			base.ReloadTableData();
			RecountAllCellHeights();
		}

		protected override void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
			switch (args.Action) {
				case NotifyCollectionChangedAction.Add:
					for (int i = args.NewStartingIndex, count = args.NewItems.Count; i < count; i++) {
						CountHeightForRow(i);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
			}

			base.CollectionChangedOnCollectionChanged(sender, args);
		}

		public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath) {
			var item = ItemsSource.ElementAt(indexPath.Row) as ChatItemVM;
			if (item.EstimatedHeight != 0.0)
				return (nfloat)item.EstimatedHeight;
			return UITableView.AutomaticDimension;
		}

		public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) {
			var item = ItemsSource.ElementAt(indexPath.Row) as ChatItemVM;
			if (item.EstimatedHeight != 0.0)
				return (nfloat)item.EstimatedHeight;
			return UITableView.AutomaticDimension;
		}

		public void RecountAllCellHeights(CGSize customSize) {
			if (ItemsSource == null)
				return;
			int i = 0;
			TableView.BeginUpdates();
			foreach (var item in ItemsSource) {
				CountHeightForRow(i++, customSize, true);
			}
			TableView.EndUpdates();
		}

		public void RecountAllCellHeights() {
			RecountAllCellHeights(DeviceInfo.ScreenBounds.Size);
		}

		private readonly UILabel
			timeLabel = new() {
				TextAlignment = UITextAlignment.Right,
				Font = UIFont.GetMonospacedSystemFont(12.0f, UIFontWeight.Medium),
				LineBreakMode = UILineBreakMode.TailTruncation,
				Lines = 1
			},
			nameLabel = new() {
				TextAlignment = UITextAlignment.Left,
				Font = UIFont.GetMonospacedSystemFont(17.0f, UIFontWeight.Regular),
				LineBreakMode = UILineBreakMode.TailTruncation,
				Lines = 1
			},
			messageLabel = new() {
				TextAlignment = UITextAlignment.Left,
				Font = UIFont.GetMonospacedSystemFont(15.0f, UIFontWeight.Regular),
				LineBreakMode = UILineBreakMode.WordWrap,
				Lines = 0
			},
			textLabel = new() {
				TextAlignment = UITextAlignment.Left,
				Font = UIFont.GetMonospacedSystemFont(15.0f, UIFontWeight.Regular),
				LineBreakMode = UILineBreakMode.WordWrap,
				Lines = 0
			};
		private nfloat CountHeightForRow(int row, bool recount = false) {
			return CountHeightForRow(row, DeviceInfo.ScreenBounds.Size, recount);
		}
		private nfloat CountHeightForRow(int row, CGSize size, bool recount = false) {
			var item = ItemsSource.ElementAt(row) as ChatItemVM;
			if (!recount && item.EstimatedHeight != 0.0)
				return (nfloat)item.EstimatedHeight;

			nfloat height = 0.0f;
			nfloat leftMargin = 16.0f + (DeviceInfo.IsCollapsed ? DeviceInfo.SafeAreaInsets.Left : 0.0f),
				rightMargin = 16.0f + DeviceInfo.SafeAreaInsets.Right;
			timeLabel.Frame = new CGRect(0.0f, 0.0f, 0.0f, 0.0f);
			timeLabel.Text = item.Time;
			timeLabel.SizeToFit();
			if (item is ChatMessageItemVM messageItem) {
				nameLabel.Frame = new CGRect(leftMargin, 0.0f, size.Width - leftMargin - rightMargin - timeLabel.Frame.Width - 16.0f, 0.0f);
				nameLabel.AttributedText = ColourTextValueConverter.Convert(messageItem.PlayerName, new ColourTextParameter() {
					ParseUri = true,
					ParseShadow = messageItem.Shadow
				});
				nameLabel.SizeToFit();
				messageLabel.Frame = new CGRect(leftMargin, 0.0f, size.Width - leftMargin - rightMargin, 0.0f);
				messageLabel.AttributedText = ColourTextValueConverter.Convert(messageItem.Message, new ColourTextParameter() {
					ParseUri = true,
					ParseShadow = messageItem.Shadow
				});
				messageLabel.SizeToFit();
				height += 8.0f;//messageItem.TopVMType == typeof(ChatMessageItemVM) ? 7.5f : 15.0f;
				height += nameLabel.Frame.Height;
				height += 0.0f;
				height += messageLabel.Frame.Height;
				height += 8.0f;//messageItem.BottomVMType == typeof(ChatMessageItemVM) ? 7.5f : 15.0f;
			} else if (item is ChatInfoItemVM infoItem) {
				textLabel.Frame = new CGRect(0.0f, 0.0f, 0.0f, 0.0f);
				textLabel.AttributedText = ColourTextValueConverter.Convert(infoItem.Text, new ColourTextParameter() {
					ParseUri = true,
					ParseShadow = infoItem.Shadow
				});
				textLabel.SizeToFit();
				height += 8.0f;
				height += textLabel.Frame.Height;
				height += 8.0f;
			}
			item.EstimatedHeight = height;
			return height;
		}
	}
}
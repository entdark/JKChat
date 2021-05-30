using System;
using System.Diagnostics;
using System.Threading.Tasks;

using CoreGraphics;

using Foundation;

using JKChat.iOS.Helpers;
using JKChat.iOS.Views.Base;

using UIKit;

namespace JKChat.iOS.Controls {
	[Register("ChatTableView")]
	public class ChatTableView : UITableView {
		public const float SpecialOffset = 500.0f;
		public IKeyboardViewController KeyboardViewController { get; set; }
		public override UIEdgeInsets ContentInset {
			get => new UIEdgeInsets(base.ContentInset.Bottom - ExtraContentInset.Bottom, base.ContentInset.Left - ExtraContentInset.Left, base.ContentInset.Top - ExtraContentInset.Top, base.ContentInset.Right - ExtraContentInset.Right);
			set => base.ContentInset = new UIEdgeInsets(value.Bottom + ExtraContentInset.Bottom, value.Left + ExtraContentInset.Left, value.Top + ExtraContentInset.Top, value.Right + ExtraContentInset.Right);
		}
		public override UIEdgeInsets ScrollIndicatorInsets {
			get => new UIEdgeInsets(base.ScrollIndicatorInsets.Bottom, base.ScrollIndicatorInsets.Left, base.ScrollIndicatorInsets.Top, base.ScrollIndicatorInsets.Right);
			set => base.ScrollIndicatorInsets = new UIEdgeInsets(value.Bottom, value.Left, value.Top, value.Right);
		}
		private UIEdgeInsets extraContentInset = UIEdgeInsets.Zero;
		public UIEdgeInsets ExtraContentInset {
			get => extraContentInset;
			set {
				extraContentInset = value;
				base.ContentInset = new UIEdgeInsets(base.ContentInset.Bottom + value.Bottom, base.ContentInset.Left + value.Left, base.ContentInset.Top + value.Top, base.ContentInset.Right + value.Right);
			}
		}
		private bool scrolledToBottom = true;
		public bool ScrolledToBottom {
			get => scrolledToBottom;
			set {
				scrolledToBottom = value;
				if (value) {

				}
			}
		}
		public bool StartedDragging { get; set; }
		public ChatTableView() : base() {
			Initialize();
		}
		public ChatTableView(NSCoder coder) : base(coder) {
			Initialize();
		}
		public ChatTableView(CGRect frame) : base(frame) {
			Initialize();
		}
		public ChatTableView(CGRect frame, UITableViewStyle style) : base(frame, style) {
			Initialize();
		}
		protected ChatTableView(NSObjectFlag t) : base(t) {
			Initialize();
		}
		protected internal ChatTableView(IntPtr handle) : base(handle) {
			Initialize();
		}

		private void Initialize() {
			this.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);
			this.ClipsToBounds = false;
			this.ContentInset = UIEdgeInsets.Zero;
			this.ScrollsToTop = false;
		}

		public override void ReloadData() {
/*			var beforeContentSize = this.ContentSize;
			Debug.WriteLine($"beforeContentSize: {beforeContentSize}");
			var beforeContentOffset = this.ContentOffset;
			Debug.WriteLine($"beforeContentOffset: {beforeContentOffset}");*/
			base.ReloadData();
			return;
/*			var afterContentSize = this.ContentSize;
			Debug.WriteLine($"afterContentSize: {afterContentSize}");
			var afterContentOffset = this.ContentOffset;
			Debug.WriteLine($"afterContentOffset: {afterContentOffset}");*/
			if (this.ScrolledToBottom) {
				return;
			}
			//very dirty
			Task.Run(async () => {
				await Task.Delay(64);
				InvokeOnMainThread(() => {
//					this.SetContentOffset(new CGPoint(afterContentOffset.X, -(afterContentOffset.Y + KeyboardViewController.BeginKeyboardFrame.Height/*ContentInset.Bottom*/)/* + (KeyboardViewController.EndKeyboardFrame.Height - DeviceInfo.SafeAreaInsets.Bottom)*//* + ExtraContentInset.Bottom*/), false);
				});
			});
			//this.ScrollToBottom();
		}

		public override void InsertRows(NSIndexPath []atIndexPaths, UITableViewRowAnimation withRowAnimation) {
			bool animationsEnabled = ScrolledToBottom || ((this.ContentOffset.Y + ChatTableView.SpecialOffset + this.ExtraContentInset.Bottom) >= (DeviceInfo.ScreenBounds.Height) && (this.ContentSize.Height >= DeviceInfo.ScreenBounds.Height * 2.0f));
//			UIView.AnimationsEnabled = true;
			if (!animationsEnabled) {
				UIView.PerformWithoutAnimation(() => {
					base.InsertRows(atIndexPaths, withRowAnimation);
				});
				var beforeContentSize = this.ContentSize;
				Debug.WriteLine($"beforeContentSize: {beforeContentSize}");
				var beforeContentOffset = this.ContentOffset;
				Debug.WriteLine($"beforeContentOffset: {beforeContentOffset}");

				var afterContentSize = this.ContentSize;
				Debug.WriteLine($"afterContentSize: {afterContentSize}");
				var afterContentOffset = this.ContentOffset;
				Debug.WriteLine($"afterContentOffset: {afterContentOffset}");

				CGPoint newContentOffset = new CGPoint(afterContentOffset.X, afterContentOffset.Y + afterContentSize.Height - beforeContentSize.Height);
				this.SetContentOffset(newContentOffset, false);
			} else {
				base.InsertRows(atIndexPaths, withRowAnimation);
			}
			Debug.WriteLine($"animationsEnabled: {animationsEnabled}");
//			UIView.AnimationsEnabled = this.ContentSize.Height > this.Frame.Height;
//			this.ScrollToBottom();
/*if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0)) {
				this.PerformBatchUpdates(() => {
					this.SetContentOffset(this.ContentOffset, false);
					base.InsertRows(atIndexPaths, withRowAnimation);
				}, null);
	} else {
				// Fallback on earlier versions
				this.BeginUpdates();
				this.SetContentOffset(this.ContentOffset, false);
				base.InsertRows(atIndexPaths, withRowAnimation);
				this.EndUpdates();
	}*/
//				base.InsertRows(atIndexPaths, withRowAnimation);
		}

		public override void SetContentOffset(CGPoint contentOffset, bool animated) {
			base.SetContentOffset(new CGPoint(contentOffset.X, -(contentOffset.Y)), animated);
		}

		public void ScrollToBottom() {
			if (!ScrolledToBottom) {
				return;
			}
			var frame = this.Frame;
			if (frame.Height > this.ContentSize.Height) {
				return;
			}
			nint rows = NumberOfRowsInSection(0);
			if (rows <= 0) {
				return;
			}
/*			if (this.Dragging) {
				return;
			}*/
			var indexPath = NSIndexPath.FromRowSection(0, 0);
			this.ScrollToRow(indexPath, UITableViewScrollPosition.Top, true);
		}
	}
}
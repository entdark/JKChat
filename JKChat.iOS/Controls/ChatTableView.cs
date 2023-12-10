using CoreGraphics;

using Foundation;

using JKChat.iOS.Helpers;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls {
	[Register("ChatTableView")]
	public class ChatTableView : UITableView {
		public static float SpecialOffset => !DeviceInfo.IsRunningOnMacOS ? 1337.0f : 0.0f;
		public IKeyboardViewController KeyboardViewController { get; set; }
		public override UIEdgeInsets ContentInset {
			get => new(base.ContentInset.Bottom, base.ContentInset.Left, base.ContentInset.Top, base.ContentInset.Right);
			set => base.ContentInset = new(value.Bottom, value.Left, value.Top, value.Right);
		}
		public override UIEdgeInsets VerticalScrollIndicatorInsets {
			get => new(base.VerticalScrollIndicatorInsets.Bottom, base.VerticalScrollIndicatorInsets.Left, base.VerticalScrollIndicatorInsets.Top, base.VerticalScrollIndicatorInsets.Right);
			set => base.VerticalScrollIndicatorInsets = new(value.Bottom, value.Left, value.Top, value.Right);
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
		protected internal ChatTableView(NativeHandle handle) : base(handle) {
			Initialize();
		}

		private void Initialize() {
			this.Transform = CGAffineTransform.MakeScale(1.0f, -1.0f);
			this.ClipsToBounds = false;
			this.ContentInset = new UIEdgeInsets(0.0f, 0.0f, ChatTableView.SpecialOffset, 0.0f);
			this.ScrollIndicatorInsets = new UIEdgeInsets(0.0f, 0.0f, ChatTableView.SpecialOffset, 0.0f);
			this.ScrollsToTop = false;
		}

		public override void ReloadData() {
			base.ReloadData();
			this.SetContentOffset(new CGPoint(0.0f, ChatTableView.SpecialOffset), false);
		}

		public override void SetContentOffset(CGPoint contentOffset, bool animated) {
			base.SetContentOffset(new CGPoint(contentOffset.X, -(contentOffset.Y)), animated);
		}

		public void ScrollToBottom() {
			if (!ScrolledToBottom) {
				return;
			}
			nint rows = NumberOfRowsInSection(0);
			if (rows <= 0) {
				return;
			}
			var indexPath = NSIndexPath.FromRowSection(0, 0);
			this.ScrollToRow(indexPath, UITableViewScrollPosition.Top, true);
		}

		public void RecountAllCellHeights(CGSize newSize) {
			var source = (this.Source as ChatTableViewSource) ?? (this.DataSource as ChatTableViewSource);
			source?.RecountAllCellHeights(newSize);
		}
	}
}
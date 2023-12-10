// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.Chat
{
	[Register ("ServerInfoViewController")]
	partial class ServerInfoViewController
	{
		[Outlet]
		UIKit.UIButton ConnectButton { get; set; }

		[Outlet]
		UIKit.UIImageView PreviewImageView { get; set; }

		[Outlet]
		UIKit.UIView PreviewView { get; set; }

		[Outlet]
		UIKit.UITableView ServerInfoTableView { get; set; }

		[Outlet]
		UIKit.UIImageView StatusImageView { get; set; }

		[Outlet]
		UIKit.UILabel StatusLabel { get; set; }

		[Outlet]
		UIKit.UILabel TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ConnectButton != null) {
				ConnectButton.Dispose ();
				ConnectButton = null;
			}

			if (PreviewImageView != null) {
				PreviewImageView.Dispose ();
				PreviewImageView = null;
			}

			if (PreviewView != null) {
				PreviewView.Dispose ();
				PreviewView = null;
			}

			if (ServerInfoTableView != null) {
				ServerInfoTableView.Dispose ();
				ServerInfoTableView = null;
			}

			if (StatusImageView != null) {
				StatusImageView.Dispose ();
				StatusImageView = null;
			}

			if (StatusLabel != null) {
				StatusLabel.Dispose ();
				StatusLabel = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}
		}
	}
}

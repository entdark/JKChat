// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.ServerList.Cells
{
	[Register ("ServerListViewCell")]
	partial class ServerListViewCell
	{
		[Outlet]
		UIKit.UIButton ConnectButton { get; set; }

		[Outlet]
		UIKit.UIButton FavouriteButton { get; set; }

		[Outlet]
		UIKit.UILabel GameLabel { get; set; }

		[Outlet]
		UIKit.UILabel MapNameLabel { get; set; }

		[Outlet]
		UIKit.UILabel PlayersLabel { get; set; }

		[Outlet]
		UIKit.UIImageView PreviewImageView { get; set; }

		[Outlet]
		UIKit.UIView PreviewView { get; set; }

		[Outlet]
		UIKit.UILabel ServerNameLabel { get; set; }

		[Outlet]
		UIKit.UIImageView StatusImageView { get; set; }

		[Outlet]
		UIKit.UILabel StatusLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ConnectButton != null) {
				ConnectButton.Dispose ();
				ConnectButton = null;
			}

			if (FavouriteButton != null) {
				FavouriteButton.Dispose ();
				FavouriteButton = null;
			}

			if (GameLabel != null) {
				GameLabel.Dispose ();
				GameLabel = null;
			}

			if (MapNameLabel != null) {
				MapNameLabel.Dispose ();
				MapNameLabel = null;
			}

			if (PlayersLabel != null) {
				PlayersLabel.Dispose ();
				PlayersLabel = null;
			}

			if (ServerNameLabel != null) {
				ServerNameLabel.Dispose ();
				ServerNameLabel = null;
			}

			if (StatusImageView != null) {
				StatusImageView.Dispose ();
				StatusImageView = null;
			}

			if (StatusLabel != null) {
				StatusLabel.Dispose ();
				StatusLabel = null;
			}

			if (PreviewImageView != null) {
				PreviewImageView.Dispose ();
				PreviewImageView = null;
			}

			if (PreviewView != null) {
				PreviewView.Dispose ();
				PreviewView = null;
			}
		}
	}
}

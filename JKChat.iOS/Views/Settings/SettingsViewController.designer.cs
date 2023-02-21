// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.Settings
{
	[Register ("SettingsViewController")]
	partial class SettingsViewController
	{
		[Outlet]
		UIKit.NSLayoutConstraint ContentLeftConstraint { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ContentRightConstraint { get; set; }

		[Outlet]
		UIKit.UIView EncodingBackgroundView { get; set; }

		[Outlet]
		UIKit.UIButton EncodingButton { get; set; }

		[Outlet]
		UIKit.UILabel EncodingHeaderLabel { get; set; }

		[Outlet]
		UIKit.UILabel EncodingLabel { get; set; }

		[Outlet]
		UIKit.UIView LocationUpdateBackgroundView { get; set; }

		[Outlet]
		UIKit.UIButton LocationUpdateButton { get; set; }

		[Outlet]
		UIKit.UILabel LocationUpdateLabel { get; set; }

		[Outlet]
		UIKit.UISwitch LocationUpdateSwitch { get; set; }

		[Outlet]
		UIKit.UIView PlayerNameBackgroundView { get; set; }

		[Outlet]
		UIKit.UIButton PlayerNameButton { get; set; }

		[Outlet]
		UIKit.UILabel PlayerNameHeaderLabel { get; set; }

		[Outlet]
		UIKit.UILabel PlayerNameLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ContentLeftConstraint != null) {
				ContentLeftConstraint.Dispose ();
				ContentLeftConstraint = null;
			}

			if (ContentRightConstraint != null) {
				ContentRightConstraint.Dispose ();
				ContentRightConstraint = null;
			}

			if (LocationUpdateBackgroundView != null) {
				LocationUpdateBackgroundView.Dispose ();
				LocationUpdateBackgroundView = null;
			}

			if (LocationUpdateButton != null) {
				LocationUpdateButton.Dispose ();
				LocationUpdateButton = null;
			}

			if (LocationUpdateLabel != null) {
				LocationUpdateLabel.Dispose ();
				LocationUpdateLabel = null;
			}

			if (LocationUpdateSwitch != null) {
				LocationUpdateSwitch.Dispose ();
				LocationUpdateSwitch = null;
			}

			if (PlayerNameHeaderLabel != null) {
				PlayerNameHeaderLabel.Dispose ();
				PlayerNameHeaderLabel = null;
			}

			if (PlayerNameBackgroundView != null) {
				PlayerNameBackgroundView.Dispose ();
				PlayerNameBackgroundView = null;
			}

			if (PlayerNameButton != null) {
				PlayerNameButton.Dispose ();
				PlayerNameButton = null;
			}

			if (PlayerNameLabel != null) {
				PlayerNameLabel.Dispose ();
				PlayerNameLabel = null;
			}

			if (EncodingHeaderLabel != null) {
				EncodingHeaderLabel.Dispose ();
				EncodingHeaderLabel = null;
			}

			if (EncodingLabel != null) {
				EncodingLabel.Dispose ();
				EncodingLabel = null;
			}

			if (EncodingBackgroundView != null) {
				EncodingBackgroundView.Dispose ();
				EncodingBackgroundView = null;
			}

			if (EncodingButton != null) {
				EncodingButton.Dispose ();
				EncodingButton = null;
			}
		}
	}
}

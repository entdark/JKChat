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
	[Register ("MinimapSettingsViewController")]
	partial class MinimapSettingsViewController
	{
		[Outlet]
		UIKit.UITableView MinimapSettingsTableView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (MinimapSettingsTableView != null) {
				MinimapSettingsTableView.Dispose ();
				MinimapSettingsTableView = null;
			}
		}
	}
}

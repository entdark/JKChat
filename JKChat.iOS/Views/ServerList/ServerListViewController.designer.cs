// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.ServerList
{
	[Register ("ServerListViewController")]
	partial class ServerListViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UITableView ServerListTableView { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (ServerListTableView != null) {
				ServerListTableView.Dispose ();
				ServerListTableView = null;
			}
		}
	}
}
// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.ServerList.Cells
{
    [Register ("ServerListViewCell")]
    partial class ServerListViewCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView ContainerView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel GameTypeLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel MapNameLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView PasswordImageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel PingLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel PlayersLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel ServerNameLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel StatusLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView StatusView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ContainerView != null) {
                ContainerView.Dispose ();
                ContainerView = null;
            }

            if (GameTypeLabel != null) {
                GameTypeLabel.Dispose ();
                GameTypeLabel = null;
            }

            if (MapNameLabel != null) {
                MapNameLabel.Dispose ();
                MapNameLabel = null;
            }

            if (PasswordImageView != null) {
                PasswordImageView.Dispose ();
                PasswordImageView = null;
            }

            if (PingLabel != null) {
                PingLabel.Dispose ();
                PingLabel = null;
            }

            if (PlayersLabel != null) {
                PlayersLabel.Dispose ();
                PlayersLabel = null;
            }

            if (ServerNameLabel != null) {
                ServerNameLabel.Dispose ();
                ServerNameLabel = null;
            }

            if (StatusLabel != null) {
                StatusLabel.Dispose ();
                StatusLabel = null;
            }

            if (StatusView != null) {
                StatusView.Dispose ();
                StatusView = null;
            }
        }
    }
}
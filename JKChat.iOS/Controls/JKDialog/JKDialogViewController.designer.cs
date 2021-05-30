// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Controls.JKDialog
{
    [Register ("JKDialogViewController")]
    partial class JKDialogViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton BackgroundButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField InputTextField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView InputView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton LeftButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint ListHeightConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITableView ListTableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView ListView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint MessageHeightConstraint { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel MessageLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIScrollView MessageScrollView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView MessageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton RightButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel TitleLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView TitleView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView DialogView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.NSLayoutConstraint DialogViewCenterYConstraint { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (BackgroundButton != null) {
                BackgroundButton.Dispose ();
                BackgroundButton = null;
            }

            if (InputTextField != null) {
                InputTextField.Dispose ();
                InputTextField = null;
            }

            if (InputView != null) {
                InputView.Dispose ();
                InputView = null;
            }

            if (LeftButton != null) {
                LeftButton.Dispose ();
                LeftButton = null;
            }

            if (ListHeightConstraint != null) {
                ListHeightConstraint.Dispose ();
                ListHeightConstraint = null;
            }

            if (ListTableView != null) {
                ListTableView.Dispose ();
                ListTableView = null;
            }

            if (ListView != null) {
                ListView.Dispose ();
                ListView = null;
            }

            if (MessageHeightConstraint != null) {
                MessageHeightConstraint.Dispose ();
                MessageHeightConstraint = null;
            }

            if (MessageLabel != null) {
                MessageLabel.Dispose ();
                MessageLabel = null;
            }

            if (MessageScrollView != null) {
                MessageScrollView.Dispose ();
                MessageScrollView = null;
            }

            if (MessageView != null) {
                MessageView.Dispose ();
                MessageView = null;
            }

            if (RightButton != null) {
                RightButton.Dispose ();
                RightButton = null;
            }

            if (TitleLabel != null) {
                TitleLabel.Dispose ();
                TitleLabel = null;
            }

            if (TitleView != null) {
                TitleView.Dispose ();
                TitleView = null;
            }

            if (DialogView != null) {
                DialogView.Dispose ();
                DialogView = null;
            }

            if (DialogViewCenterYConstraint != null) {
                DialogViewCenterYConstraint.Dispose ();
                DialogViewCenterYConstraint = null;
            }
        }
    }
}
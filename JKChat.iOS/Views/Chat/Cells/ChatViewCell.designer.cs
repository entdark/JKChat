// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace JKChat.iOS.Views.Chat.Cells
{
    [Register ("ChatViewCell")]
    partial class ChatViewCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel MessageTextView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel PlayerNameLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (MessageTextView != null) {
                MessageTextView.Dispose ();
                MessageTextView = null;
            }

            if (PlayerNameLabel != null) {
                PlayerNameLabel.Dispose ();
                PlayerNameLabel = null;
            }
        }
    }
}
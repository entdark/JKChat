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
	[Register ("ChatViewController")]
	partial class ChatViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		JKChat.iOS.Controls.ChatTableView ChatTableView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ChatTypeButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ChatTypeCommonButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ChatTypePrivateButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIStackView ChatTypeStackView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ChatTypeTeamButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		JKChat.iOS.Controls.PlaceholderTextView MessageTextView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIView MessageView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton SendButton { get; set; }

		[Outlet]
		[GeneratedCode("iOS Designer", "1.0")]
		UIKit.NSLayoutConstraint ViewBottomConstraint { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ChatTableView != null) {
				ChatTableView.Dispose ();
				ChatTableView = null;
			}

			if (ChatTypeButton != null) {
				ChatTypeButton.Dispose ();
				ChatTypeButton = null;
			}

			if (ChatTypeCommonButton != null) {
				ChatTypeCommonButton.Dispose ();
				ChatTypeCommonButton = null;
			}

			if (ChatTypePrivateButton != null) {
				ChatTypePrivateButton.Dispose ();
				ChatTypePrivateButton = null;
			}

			if (ChatTypeStackView != null) {
				ChatTypeStackView.Dispose ();
				ChatTypeStackView = null;
			}

			if (ChatTypeTeamButton != null) {
				ChatTypeTeamButton.Dispose ();
				ChatTypeTeamButton = null;
			}

			if (MessageTextView != null) {
				MessageTextView.Dispose ();
				MessageTextView = null;
			}

			if (MessageView != null) {
				MessageView.Dispose ();
				MessageView = null;
			}

			if (SendButton != null) {
				SendButton.Dispose ();
				SendButton = null;
			}

			if (ViewBottomConstraint != null) {
				ViewBottomConstraint.Dispose ();
				ViewBottomConstraint = null;
			}
		}
	}
}

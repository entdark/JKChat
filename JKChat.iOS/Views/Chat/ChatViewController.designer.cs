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
		UIKit.UILabel CenterPrintLabel { get; set; }

		[Outlet]
		UIKit.UIVisualEffectView CenterPrintView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		JKChat.iOS.Controls.ChatTableView ChatTableView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ChatTableViewBottomConstraint { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ChatTableViewBottomToMessageViewTopConstraint { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ChatTableViewToMinimapViewBottomConstraint { get; set; }

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
		UIKit.UIButton CommandsButton { get; set; }

		[Outlet]
		UIKit.UITableView CommandsTableView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint CommandsTableViewBottomConstraint { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint CommandsTableViewHeightConstraint { get; set; }

		[Outlet]
		UIKit.UIView InfoView { get; set; }

		[Outlet]
		UIKit.UIProgressView MapLoadingProgressView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		JKChat.iOS.Controls.PlaceholderTextView MessageTextView { get; set; }

		[Outlet]
		UIKit.UIToolbar MessageToolbar { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIView MessageView { get; set; }

		[Outlet]
		JKChat.iOS.Controls.MinimapView MinimapView { get; set; }

		[Outlet]
		UIKit.UILabel ScoresLabel { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton SendButton { get; set; }

		[Outlet]
		UIKit.UILabel TimerLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CenterPrintLabel != null) {
				CenterPrintLabel.Dispose ();
				CenterPrintLabel = null;
			}

			if (CenterPrintView != null) {
				CenterPrintView.Dispose ();
				CenterPrintView = null;
			}

			if (ChatTableView != null) {
				ChatTableView.Dispose ();
				ChatTableView = null;
			}

			if (ChatTableViewBottomConstraint != null) {
				ChatTableViewBottomConstraint.Dispose ();
				ChatTableViewBottomConstraint = null;
			}

			if (ChatTableViewBottomToMessageViewTopConstraint != null) {
				ChatTableViewBottomToMessageViewTopConstraint.Dispose ();
				ChatTableViewBottomToMessageViewTopConstraint = null;
			}

			if (ChatTableViewToMinimapViewBottomConstraint != null) {
				ChatTableViewToMinimapViewBottomConstraint.Dispose ();
				ChatTableViewToMinimapViewBottomConstraint = null;
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

			if (CommandsButton != null) {
				CommandsButton.Dispose ();
				CommandsButton = null;
			}

			if (CommandsTableView != null) {
				CommandsTableView.Dispose ();
				CommandsTableView = null;
			}

			if (CommandsTableViewBottomConstraint != null) {
				CommandsTableViewBottomConstraint.Dispose ();
				CommandsTableViewBottomConstraint = null;
			}

			if (CommandsTableViewHeightConstraint != null) {
				CommandsTableViewHeightConstraint.Dispose ();
				CommandsTableViewHeightConstraint = null;
			}

			if (InfoView != null) {
				InfoView.Dispose ();
				InfoView = null;
			}

			if (MessageTextView != null) {
				MessageTextView.Dispose ();
				MessageTextView = null;
			}

			if (MessageToolbar != null) {
				MessageToolbar.Dispose ();
				MessageToolbar = null;
			}

			if (MessageView != null) {
				MessageView.Dispose ();
				MessageView = null;
			}

			if (MinimapView != null) {
				MinimapView.Dispose ();
				MinimapView = null;
			}

			if (ScoresLabel != null) {
				ScoresLabel.Dispose ();
				ScoresLabel = null;
			}

			if (SendButton != null) {
				SendButton.Dispose ();
				SendButton = null;
			}

			if (TimerLabel != null) {
				TimerLabel.Dispose ();
				TimerLabel = null;
			}

			if (MapLoadingProgressView != null) {
				MapLoadingProgressView.Dispose ();
				MapLoadingProgressView = null;
			}
		}
	}
}

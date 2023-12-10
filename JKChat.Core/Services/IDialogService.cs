using System;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.Dialog;

namespace JKChat.Core.Services {
	public interface IDialogService {
		Task ShowAsync(JKDialogConfig config);
		public Task ShowAsync(string message) {
			return ShowAsync(new JKDialogConfig(message));
		}
		public Task ShowAsync(string message, string title) {
			return ShowAsync(new JKDialogConfig(message, title));
		}
		public Task ShowAsync(DialogListViewModel list, Action<JKDialogConfig> okAction, string title = null) {
			return ShowAsync(new JKDialogConfig(list, okAction, title));
		}
	}

	public class JKDialogConfig {
		public string Title { get; init; }
		public string Message { get; init; }
		public string OkText { get; init; }
		public Action<JKDialogConfig> OkAction { get; init; }
		public string CancelText { get; init; }
		public Action<JKDialogConfig> CancelAction { get; init; }
		public bool HasTitle => !string.IsNullOrEmpty(Title);
		public bool HasMessage => !string.IsNullOrEmpty(Message);
		public bool HasOk => !string.IsNullOrEmpty(OkText) || !HasCancel;
		public bool HasCancel => !string.IsNullOrEmpty(CancelText);
		public bool HasList => List?.HasItems ?? false;
		public bool HasInput => Input != null;
		public bool IsDestructive { get; init; }
		public DialogListViewModel List { get; init; }
		public DialogInputViewModel Input { get; init; }

		public JKDialogConfig() {}
		public JKDialogConfig(string message) {
			Message = message;
			OkText = "OK";
		}
		public JKDialogConfig(string message, string title) {
			Title = title;
			Message = message;
			OkText = "OK";
		}
		public JKDialogConfig(DialogListViewModel list, Action<JKDialogConfig> okAction, string title = null) {
			List = list;
			if (list.SelectionType != DialogSelectionType.NoSelection)
				CancelText = "Cancel";
			if (list.SelectionType != DialogSelectionType.InstantSelection)
				OkText = "OK";
			OkAction = okAction;
			Title = title;
		}
		public JKDialogConfig(DialogInputViewModel input, Action<JKDialogConfig> okAction, string title = null) {
			Input = input;
			CancelText = "Cancel";
			OkText = "OK";
			OkAction = okAction;
			Title = title;
		}
		public JKDialogConfig(string message = null, string title = null, string okText = null, Action<JKDialogConfig> okAction = null, string cancelText = null, Action<JKDialogConfig> cancelAction = null, DialogListViewModel list = null, DialogInputViewModel input = null) {
			Title = title;
			Message = message;
			OkText = okText;
			OkAction = okAction;
			CancelText = cancelText;
			CancelAction = cancelAction;
			List = list;
			Input = input;
			if (!HasCancel && string.IsNullOrEmpty(OkText))
				OkText = "OK";
		}
	}
}
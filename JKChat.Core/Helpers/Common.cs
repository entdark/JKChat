using System;
using System.Threading.Tasks;

using JKChat.Core.Services;

using MvvmCross;

using Xamarin.Essentials;

namespace JKChat.Core.Helpers {
	public static class Common {
		public static async Task ExceptionCallback(Exception exception) {
			Exception realException;
			if (exception.InnerException is AggregateException aggregateException) {
				realException = aggregateException.InnerExceptions != null ? aggregateException.InnerExceptions[0] : aggregateException;
			} else if (exception.InnerException != null) {
				realException = exception.InnerException;
			} else {
				realException = exception;
			}
			string message = realException.Message + (!string.IsNullOrEmpty(realException.StackTrace) ? ("\n\n" + realException.StackTrace) : string.Empty);

			await Mvx.IoCProvider.Resolve<IDialogService>().ShowAsync(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				LeftButton = "Copy",
				LeftClick = (_) => {
					Clipboard.SetTextAsync(message);
				},
				RightButton = "OK",
				Type = JKDialogType.Title | JKDialogType.Message
			});
		}

		public static async Task ExceptionalTaskRun(Action action) {
			await Task.Run(action)
				.ContinueWith(async t => {
					if (t.IsFaulted)
						await ExceptionCallback(t.Exception);
				});
		}
	}
}

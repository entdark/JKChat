using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Services;

using MvvmCross;

using Xamarin.Essentials;

namespace JKChat.Core.Helpers {
	public static class Common {
		public static async Task ExceptionCallback(Exception exception) {
			string message = GetExceptionMessage(exception);

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

		public static string GetExceptionMessage(Exception exception) {
			if (exception == null) {
				return string.Empty;
			}
			Exception realException;
			if (exception.InnerException is AggregateException aggregateException) {
				realException = aggregateException.InnerExceptions != null ? aggregateException.InnerExceptions[0] : aggregateException;
			} else if (exception.InnerException != null) {
				realException = exception.InnerException;
			} else {
				realException = exception;
			}
			return realException.Message + (!string.IsNullOrEmpty(realException.StackTrace) ? ("\n\n" + realException.StackTrace) : string.Empty);
		}

		public static async Task<bool> ExceptionalTaskRun(Action action) {
			bool result = true;
			await Task.Run(action)
				.ContinueWith(async t => {
					Debug.WriteLine("t: " + t.Status);
					if (t.IsFaulted) {
						result = false;
						await ExceptionCallback(t.Exception);
					}
				});
			return result;
		}

		public static async Task<Task<TResult>> WhenAny<TResult>(this IEnumerable<Task<TResult>> tasks, Predicate<Task<TResult>> condition) {
			var tasklist = tasks.ToList();
			while (tasklist.Count > 0) {
				var task = await Task.WhenAny(tasklist);
				if (condition(task))
					return task;
				tasklist.Remove(task);
			}
			return null;
		}
	}
}

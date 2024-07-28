using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using JKChat.Core.Services;

using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;

using MvvmCross;

namespace JKChat.Core.Helpers {
	public static class Common {
		public static async Task ExceptionCallback(Exception exception) {
			string message = GetExceptionMessage(exception);

			await Mvx.IoCProvider.Resolve<IDialogService>().ShowAsync(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				OkText = "Copy",
				OkAction = (_) => {
					Clipboard.SetTextAsync(message);
				},
				CancelText = "OK"
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
			return realException.Message + (!string.IsNullOrEmpty(realException.StackTrace) ? (Environment.NewLine + Environment.NewLine + realException.StackTrace) : string.Empty);
		}

		public static Task<bool> ExceptionalTaskRun(Action action) {
			return ExceptionalTaskRun(Task.Run(action));
		}

		public static Task<bool> ExceptionalTaskRun(Func<Task> func) {
			return ExceptionalTaskRun(func());
		}

		public static async Task<bool> ExceptionalTaskRun(Task task) {
			bool result = true;
			await task
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

		public static async Task<TResult> ExecuteWithin<TResult>(this Task<TResult> task, int withinMilliseconds) {
			if (withinMilliseconds <= 0)
				return await task;
			var delayTask = Task.Delay(withinMilliseconds);
			await Task.WhenAll(task, delayTask);
			return task.Result;
		}

		public static bool IsApple(this DevicePlatform platform) =>
			platform == DevicePlatform.iOS
			|| platform == DevicePlatform.MacCatalyst
			|| platform == DevicePlatform.macOS;

		public static string Serialize(this object value, Func<string> defaultValueFunc = null) {
			try {
				return JsonSerializer.Serialize(value);
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			return defaultValueFunc?.Invoke();
		}

		public static T Deserialize<T>(this string json, Func<T> defaultValueFunc = default) {
			try {
				return JsonSerializer.Deserialize<T>(json);
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			return defaultValueFunc != null ? defaultValueFunc() : default;
		}
	}
}
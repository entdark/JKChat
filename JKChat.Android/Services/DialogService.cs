using System;
using System.Threading.Tasks;

using AndroidX.Lifecycle;

using JKChat.Android.Controls;
using JKChat.Android.Views.Main;
using JKChat.Core.Services;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.Android.Services {
	public class DialogService : IDialogService {
		private TaskCompletionSource<object> tcs;
		private JKDialogConfig config;
		private Action completion;
		private bool restore = false;

		public bool Showing { get; private set; }

		public async Task ShowAsync(JKDialogConfig config, Action completion = null) {
			if (tcs == null || tcs.Task.IsCompleted || tcs.Task.IsCanceled || tcs.Task.IsFaulted) {
			} else {
				return;
			}
			Showing = true;
			this.config = config;
			this.completion = completion;
			tcs = new TaskCompletionSource<object>();
			bool canceled = false;

			try {
				await ShowAsync();
			} catch {
//				this.config = null;
				Showing = false;
				if (/*throwOnCancel && */(tcs.Task.IsCanceled || tcs.Task.IsFaulted)) {
//					throw;
					canceled = true;
				}
			} finally {
//				this.config = null;
				Showing = false;
			}

			if (!canceled) {
				this.completion?.Invoke();
			}
		}

		private async Task ShowAsync() {
			if (this.config == null) {
				return;
			}
			do {
				var mainActivity = Platform.CurrentActivity as MainActivity;
				//happens in the very first launch
				if (mainActivity != null && mainActivity.Lifecycle.CurrentState == Lifecycle.State.Initialized) {
					break;
				} else if (mainActivity != null && mainActivity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed)) {
					break;
				} else if (mainActivity == null) {
					break;
				}
			} while (true);

			Platform.CurrentActivity.RunOnUiThread(() => {
				var dialog = new JKDialog(Platform.CurrentActivity, Resource.Style.Dialog, this.config, tcs);
				dialog.Show();
			});

			await tcs.Task;

			this.config = null;
			Showing = false;
		}

		public void SaveState() {
			restore = true;
		}

		public void RestoreState() {
			if (restore) {
				restore = false;
				Task.Run(ShowAsync).ContinueWith(t => {
					this.config = null;
					Showing = false;
				}, TaskContinuationOptions.NotOnRanToCompletion);
			}
		}

		public void Stop(bool force = false) {
			if (!restore || force) {
				tcs?.TrySetCanceled();
			}
		}
	}
}
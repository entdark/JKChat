using System;
using System.Linq;
using System.Threading.Tasks;

using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

using AndroidX.AppCompat.App;

using Google.Android.Material.TextField;

using Java.Lang;

using JKChat.Android.Helpers;
using JKChat.Android.ValueConverters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog;

using Microsoft.Maui.ApplicationModel;

#if true
using Builder = Google.Android.Material.Dialog.MaterialAlertDialogBuilder;
#else
using Builder = AndroidX.AppCompat.App.AlertDialog.Builder;
#endif

namespace JKChat.Android.Services {
	public class DialogService : IDialogService {
		public async Task ShowAsync(JKDialogConfig config) {
			await MainThread.InvokeOnMainThreadAsync(() => {
				Show(config);
			});
		}

		public static void Show(JKDialogConfig config) {
			AlertDialog alert = null;
			var context = Platform.CurrentActivity;
			var builder = new Builder(context)
				.SetMessage(ColourTextValueConverter.Convert(config.Message))
				.SetTitle(ColourTextValueConverter.Convert(config.Title));
			if (config.HasList) {
				var list = config.List;
				var items = list.Items.Select(item => ColourTextValueConverter.Convert(item.Name)).ToArray();
				switch (list.SelectionType) {
					case DialogSelectionType.NoSelection:
						builder
							.SetItems(items, (IDialogInterfaceOnClickListener)null);
						break;
					case DialogSelectionType.InstantSelection:
						builder
							.SetItems(items, (sender, ev) => {
								list.Items[ev.Which].IsSelected = true;
								config.OkAction?.Invoke(config);
							});
						break;
					case DialogSelectionType.SingleSelection:
						builder
							.SetSingleChoiceItems(items, list.Items.FindIndex(item => item.IsSelected), (sender, ev) => list.ItemClickCommand?.Execute(list.Items[ev.Which]));
						break;
					case DialogSelectionType.MultiSelection:
						builder
							.SetMultiChoiceItems(items, list.Items.Select(item => item.IsSelected).ToArray(), (sender, ev) => list.Items[ev.Which].IsSelected = ev.IsChecked);
						break;
				}
			}
			if (config.HasOk) {
				builder
					.SetPositiveButton(config.OkText, (sender, ev) => config.OkAction?.Invoke(config));
			}
			if (config.HasCancel) {
				builder
					.SetCancelable(true)
					.SetNegativeButton(config.CancelText, (sender, ev) => config.CancelAction?.Invoke(config))
					.SetOnCancelListener(new OnCancelListener(_ => {
						config.CancelAction?.Invoke(config);
					}));
			} else {
				builder
					.SetCancelable(false);
			}
			if (config.HasInput) {
				var input = config.Input;
				var dialogInputView = LayoutInflater.From(context).Inflate(Resource.Layout.dialog_input, null, false);
				var textInputLayout = dialogInputView.FindViewById<TextInputLayout>(Resource.Id.dialog_input);
				textInputLayout.HintFormatted = ColourTextValueConverter.Convert(input.Hint);
				var textInputEditText = dialogInputView.FindViewById<TextInputEditText>(Resource.Id.dialog_input_edittext);
				textInputEditText.Text = input.Text;
				textInputEditText.AddTextChangedListener(new TextWatcher(s => {
					if (input.HintAsColourText)
						textInputLayout.HintFormatted = ColourTextValueConverter.Convert(s);
					input.TextChangedAction?.Invoke(s);
				}));
				textInputEditText.SetOnEditorActionListener(new OnEditorActionListener(actionId => {
					if (actionId == ImeAction.Done) {
						alert.GetButton((int)DialogButtonType.Positive)?.PerformClick();
						return true;
					}
					return false;
				}));
				builder
					.SetView(dialogInputView)
					.SetOnDismissListener(new OnDismissListener(_ => {
						context.HideKeyboard(textInputEditText, true);
					}));
				var handler = new Handler(Looper.MainLooper);
				handler.PostDelayed(() => {
					context.ShowKeyboard(textInputEditText);
					textInputEditText.SetSelection(input.Text?.Length ?? 0);
				}, 200);
			}
			alert = builder
				.Create();

			alert.Show();
		}

		private class OnDismissListener : Java.Lang.Object, IDialogInterfaceOnDismissListener {
			private readonly Action<IDialogInterface> onDismissAction;

			public OnDismissListener(Action<IDialogInterface> onDismissAction) {
				this.onDismissAction = onDismissAction;
			}

			public void OnDismiss(IDialogInterface dialog) {
				onDismissAction?.Invoke(dialog);
			}
		}

		private class TextWatcher : Java.Lang.Object, ITextWatcher {
			private readonly Action<string> afterTextChangedAction;

			public TextWatcher(Action<string> afterTextChangedAction) {
				this.afterTextChangedAction = afterTextChangedAction;
			}

			public void AfterTextChanged(IEditable s) {
				afterTextChangedAction?.Invoke(s?.ToString());
			}

			public void BeforeTextChanged(ICharSequence s, int start, int count, int after) {}

			public void OnTextChanged(ICharSequence s, int start, int before, int count) {}
		}

		private class OnEditorActionListener : Java.Lang.Object, TextView.IOnEditorActionListener {
			private readonly Func<ImeAction, bool> onEditorAction;

			public OnEditorActionListener(Func<ImeAction, bool> onEditorAction) {
				this.onEditorAction = onEditorAction;
			}

			public bool OnEditorAction(TextView tv, ImeAction actionId, KeyEvent ev) {
				return onEditorAction?.Invoke(actionId) ?? true;
			}
		}

		private class OnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener {
			private readonly Action<IDialogInterface> onCancelAction;

			public OnCancelListener(Action<IDialogInterface> onCancelAction) {
				this.onCancelAction = onCancelAction;
			}
			public void OnCancel(IDialogInterface dialog) {
				onCancelAction?.Invoke(dialog);
			}
		}
	}
}
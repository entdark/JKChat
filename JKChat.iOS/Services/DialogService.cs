using System.Threading.Tasks;

using JKChat.Core;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.iOS.Controls.JKDialog;
using JKChat.iOS.Controls.JKDialog.Cells;
using JKChat.iOS.Helpers;
using JKChat.iOS.ValueConverters;

using Microsoft.Maui.ApplicationModel;

using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.Services {
	public class DialogService : IDialogService {
		void IDialogService.Show(JKDialogConfig config) {
			Show(config);
		}
		public async Task ShowAsync(JKDialogConfig config) {
			await MainThread.InvokeOnMainThreadAsync(() => Show(config));
		}

		public static void Show(JKDialogConfig config) {
			MainThread.BeginInvokeOnMainThread(() => {
				var viewController = Platform.GetCurrentUIViewController();
				if (AppSettings.NativeAlertController) {
					var alert = UIAlertController.Create(config.Title, (config.HasInput && config.Input.HintAsColourText) ? " " : config.Message, UIAlertControllerStyle.Alert);
					if (config.HasCancel) {
						var cancelAction = UIAlertAction.Create(config.CancelText, UIAlertActionStyle.Default, action => {
							config.CancelAction?.Invoke(config);
						});
						alert.AddAction(cancelAction);
					}
					UIAlertAction okAction = null;
					if (config.HasOk) {
						okAction = UIAlertAction.Create(config.OkText, UIAlertActionStyle.Default, action => {
							config.OkAction?.Invoke(config);
						});
						alert.AddAction(okAction);
					}
					if (config.HasInput) {
						UILabel messageLabel = null;
						alert.AddTextField(textField => {
							textField.Text = config.Input.Text;
							textField.AddTarget((sender, ev) => {
								string text = (sender as UITextField)?.Text;
								config.Input.TextChangedAction?.Invoke(text);
								if (config.Input.HintAsColourText) {
									messageLabel.AttributedText = ColourTextValueConverter.Convert(text);
								}
							}, UIControlEvent.EditingChanged);
//							textField.BecomeFirstResponder();
						});
						if (config.Input.HintAsColourText) {
							messageLabel = new UILabel() {
								AttributedText = ColourTextValueConverter.Convert(config.Input.Hint),
								Font = UIFont.PreferredFootnote,
								TextAlignment = UITextAlignment.Center,
								TranslatesAutoresizingMaskIntoConstraints = false
							};
							alert.View.AddSubview(messageLabel);
							messageLabel.LeadingAnchor.ConstraintEqualTo(alert.View.LeadingAnchor, 0.0f).Active = true;
							messageLabel.TrailingAnchor.ConstraintEqualTo(alert.View.TrailingAnchor, 0.0f).Active = true;
							messageLabel.TopAnchor.ConstraintEqualTo(alert.View.TopAnchor, 44.0f).Active = true;
							messageLabel.HeightAnchor.ConstraintEqualTo(22.0f).Active = true;
							alert.View.TranslatesAutoresizingMaskIntoConstraints = false;
							alert.View.HeightAnchor.ConstraintEqualTo(44.0f + 32.0f + 44.0f + 44.0f).Active = true;
						}
					}
					if (config.HasList) {
						var list = config.List;
						var listTableView = new UITableView() {
							TintColor = Theme.Color.Accent,
							BackgroundColor = UIColor.Clear,
							TranslatesAutoresizingMaskIntoConstraints = false
						};
						listTableView.RegisterNibForCellReuse(JKDialogViewCell.Nib, JKDialogViewCell.Key);
						listTableView.AllowsMultipleSelection = list.SelectionType == DialogSelectionType.MultiSelection;

						int count = list.Items.Count;
						float height = count > 5 ? (5.5f * 44.0f) : (count * 44.0f);

						var source = new MvxSimpleTableViewSource(listTableView, JKDialogViewCell.Key) {
							ItemsSource = list.Items,
							SelectionChangedCommand = new MvxCommand<DialogItemVM>(item => {
								switch (list.SelectionType) {
									case DialogSelectionType.NoSelection:
										break;
									case DialogSelectionType.InstantSelection:
										item.IsSelected = true;
										config.OkAction?.Invoke(config);
										alert.DismissViewController(true, null);
										break;
									case DialogSelectionType.SingleSelection:
										list.ItemClickCommand?.Execute(item);
										break;
									case DialogSelectionType.MultiSelection:
										item.IsSelected = !item.IsSelected;
										break;
								}
							}),
							DeselectAutomatically = true
						};

						listTableView.Source = source;
						listTableView.ReloadData();
						alert.View.AddSubview(listTableView);
						listTableView.LeadingAnchor.ConstraintEqualTo(alert.View.LeadingAnchor, 0.0f).Active = true;
						listTableView.TrailingAnchor.ConstraintEqualTo(alert.View.TrailingAnchor, 0.0f).Active = true;
						listTableView.TopAnchor.ConstraintEqualTo(alert.View.TopAnchor, 64.0f).Active = true;
						listTableView.HeightAnchor.ConstraintEqualTo(height).Active = true;
						alert.View.TranslatesAutoresizingMaskIntoConstraints = false;
						alert.View.HeightAnchor.ConstraintEqualTo(height + 64.0f + 44.0f).Active = true;

						var separatorView = new UIView() {
							BackgroundColor = UIColor.Separator,
							TranslatesAutoresizingMaskIntoConstraints = false
						};
						alert.View.AddSubview(separatorView);
						separatorView.LeadingAnchor.ConstraintEqualTo(listTableView.LeadingAnchor, 0.0f).Active = true;
						separatorView.TrailingAnchor.ConstraintEqualTo(listTableView.TrailingAnchor, 0.0f).Active = true;
						separatorView.TopAnchor.ConstraintEqualTo(listTableView.TopAnchor, 0.0f).Active = true;
						separatorView.HeightAnchor.ConstraintEqualTo(!DeviceInfo.IsRunningOnMacOS ? 0.5f : 1.0f).Active = true;
					}
					if (viewController is UIAlertController && viewController.PresentingViewController != null) {
						viewController = viewController.PresentingViewController;
					}
					viewController.PresentViewController(alert, true, null);
				} else {
					var dialog = new JKDialogViewController(config);
					if (viewController is JKDialogViewController && viewController.PresentingViewController != null) {
						viewController = viewController.PresentingViewController;
					}
					viewController.PresentViewController(dialog, true, null);
				}
			});
		}
	}
}
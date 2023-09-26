using System;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public abstract class ReportViewModel<TItem> : BaseServerViewModel where TItem : class, ISelectableItemVM {
		private static readonly string []reportReasons = { "Spam", "Violence", "Child abuse", "Pornography", "Other" };
		private static readonly Random reportDelayerRandom = new Random();
		
		public virtual IMvxCommand ReportCommand { get; init; }
		public virtual IMvxCommand SelectCommand { get; init; }

		protected virtual string ReportTitle => "Report";
		protected virtual string ReportMessage => "Do you want to report this?";
		protected virtual string ReportedTitle => "Reported";
		protected virtual string ReportedMessage => "Thank you for reporting";

		public override string Title {
			get => base.Title;
			set { base.Title = SelectedItem != null ? "Selected" : value; }
		}

		private MvxObservableCollection<TItem> items;
		public virtual MvxObservableCollection<TItem> Items {
			get => items;
			set => SetProperty(ref items, value);
		}

		private TItem selectedItem;
		public virtual TItem SelectedItem {
			get => selectedItem;
			set => SetProperty(ref selectedItem, value);
		}

		public ReportViewModel() {
			ReportCommand = new MvxAsyncCommand<TItem>(ReportExecute);
			SelectCommand = new MvxCommand<TItem>(SelectExecute);
			Items = new MvxObservableCollection<TItem>();
		}

		private async Task ReportExecute(TItem item) {
			await ReportExecute(item, null);
		}

		protected virtual async Task ReportExecute(TItem item, Action<bool> reported = null) {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = ReportTitle,
				List = new DialogListViewModel(reportReasons.Select(s => new DialogItemVM() {
					Name = s,
					IsSelected = s == reportReasons[0]
				}), DialogSelectionType.SingleSelection),
				CancelText = "Cancel",
				OkText = "Yes",
				OkAction = config => {
					if (config?.List?.SelectedIndex is int id && id >= 0) {
						int reportReasonId = id;
						if (reportReasonId == reportReasons.Length-1) {
							Task.Run(reportExecuteContinue);
						} else {
							Task.Run(reportExecuteDone);
						}
					}
				},
				CancelAction = _ => {
					reported?.Invoke(false);
				}
			});
			async Task reportExecuteContinue() {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Report Reason",
					CancelText = "Cancel",
					CancelAction = _ => {
						reported?.Invoke(false);
					},
					OkText = "Report",
					OkAction = _ => {
						Task.Run(reportExecuteDone);
					},
					Input = new DialogInputViewModel()
				});
			}
			async Task reportExecuteDone() {
				IsLoading = true;
				//emulate reporting
				await Task.Delay(reportDelayerRandom.Next(512, 2048));
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = ReportedTitle,
					Message = ReportedMessage,
					OkText = "OK",
					OkAction = _ => {
						reported?.Invoke(true);
					}
				});
			}
		}

		protected virtual void SelectExecute(TItem item) {
			if (SelectedItem == item) {
				SelectedItem = null;
			} else {
				SelectedItem = item;
			}
			lock (Items) {
				foreach (var it in Items) {
					it.IsSelected = it == SelectedItem;
				}
			}
			if (SelectedItem != null) {
				Title = "Selected";
			}
		}
	}

	public abstract class ReportViewModel<TItem, TParameter> : ReportViewModel<TItem>, IMvxViewModel<TParameter> where TItem : class, ISelectableItemVM {
		public abstract void Prepare(TParameter parameter);
	}
}
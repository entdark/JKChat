using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base {
	public abstract class ReportViewModel<TItem> : BaseViewModel where TItem : class, ISelectableItemVM {
		private static readonly string []reportReasons = { "Spam", "Violence", "Child abuse", "Pornography", "Other" };
		private static readonly Random reportDelayerRandom = new Random();
		
		public virtual IMvxCommand ReportCommand { get; protected set; }
		public virtual IMvxCommand SelectCommand { get; protected set; }

		protected virtual string ReportTitle => "Report";
		protected virtual string ReportMessage => "Do you want to report this?";
		protected virtual string ReportedTitle => "Reported";
		protected virtual string ReportedMessage => "Thank you for reporting";

		public override string Title {
			get => base.Title;
			set { base.Title = GetSelectedItem() != null ? "Selected" : value; }
		}

		public abstract MvxObservableCollection<TItem> Items { get; set; }

		public ReportViewModel() {
			ReportCommand = new MvxAsyncCommand<TItem>(ReportExecute);
			SelectCommand = new MvxCommand<TItem>(SelectExecute);
		}

		protected virtual async Task<bool> ReportExecute(TItem item) {
			bool report = false;
			int reportReasonIndex = 0;
			int reportReasonId = -1;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = ReportTitle,
				//Message = ReportMessage,
				ListViewModel = new DialogListViewModel() {
					Items = reportReasons.Select(s => new DialogItemVM() { Id = reportReasonIndex++, Name = s, IsSelected = reportReasonIndex == 1 }).ToList(),
				},
				LeftButton = "Cancel",
				RightButton = "Yes",
				RightClick = (input) => {
					if (input is DialogItemVM dialogItem) {
						reportReasonId = dialogItem.Id;
					}
					report = true;
				},
				Type = JKDialogType.Title/* | JKDialogType.Message*/ | JKDialogType.List
			});
			if (report && reportReasonId == reportReasons.Length-1) {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Report reason",
					LeftButton = "Cancel",
					LeftClick = (_) => {
						report = false;
					},
					RightButton = "Report",
					Type = JKDialogType.Title | JKDialogType.Input
				});
			}
			if (report) {
				IsLoading = true;
				//emulate reporting
				await Task.Delay(reportDelayerRandom.Next(512, 2048));
				IsLoading = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = ReportedTitle,
					Message = ReportedMessage,
					RightButton = "OK",
					Type = JKDialogType.Title | JKDialogType.Message
				});
			}
			return report;
		}

		protected virtual void SelectExecute(TItem item) {
			foreach (var it in Items) {
				it.IsSelected = it == item;
			}
			if (GetSelectedItem() != null) {
				Title = "Selected";
			}
		}

		protected virtual TItem GetSelectedItem() {
			if (Items != null) {
				lock (Items) {
					return Items.FirstOrDefault(item => item.IsSelected);
				}
			} else {
				return null;
			}
		}
	}

	public abstract class ReportViewModel<TItem, TParameter> : ReportViewModel<TItem>, IMvxViewModel<TParameter> where TItem : class, ISelectableItemVM {
		public abstract void Prepare(TParameter parameter);
	}
}

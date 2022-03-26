using System;

using Android.Animation;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using AndroidX.Core.Content;

using JKChat.Android.Controls.Toolbar;
using JKChat.Android.Helpers;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class ReportFragment<TViewModel, TItem> : BaseFragment<TViewModel> where TItem : class, ISelectableItemVM where TViewModel : ReportViewModel<TItem>, IMvxViewModel, IBaseViewModel {
		protected virtual TItem SelectedItem { get; set; }
		protected virtual IMenuItem ReportItem { get; set; }

		public ReportFragment(int layoutId) : base(layoutId) {
			HasOptionsMenu = true;
		}

		public override void OnResume() {
			base.OnResume();
			CheckSelection();
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			ReportItem = menu.FindItem(Resource.Id.report_item);
			ReportItem.SetClickAction(() => {
				this.OnOptionsItemSelected(ReportItem);
			});
			CheckSelection();

			base.OnCreateOptionsMenu(menu, inflater);
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			if (SelectedItem != null) {
				if (item == ReportItem) {
					ViewModel.ReportCommand?.Execute(SelectedItem);
				}
				CloseSelection();
				return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		public override bool OnBackPressed() {
			if (SelectedItem != null) {
				CloseSelection();
				return true;
			}
			return base.OnBackPressed();
		}

		protected virtual void ToggleSelection(TItem item) {
			if (item == null || item == SelectedItem) {
				CloseSelection();
			} else {
				ShowSelection(item);
			}
		}

		protected virtual void CheckSelection() {
			if (SelectedItem == null) {
				CloseSelection();
			} else {
				ShowSelection(SelectedItem);
			}
		}

		protected virtual void ShowSelection(TItem item) {
			ViewModel.SelectCommand?.Execute(item);
			SelectedItem = item;
			ReportItem?.SetVisible(true, false);
			BackArrow?.SetRotation(1.0f, true);
		}

		protected virtual void CloseSelection() {
			ViewModel.SelectCommand?.Execute(null);
			SelectedItem = null;
			ReportItem?.SetVisible(false, false);
			BackArrow?.SetRotation(0.0f, true);
		}
	}
}
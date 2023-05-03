using System;
using System.Collections.Specialized;

using Android.OS;
using Android.Views;
using Android.Widget;

using AndroidX.RecyclerView.Widget;

using Google.Android.Material.FloatingActionButton;

using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.DroidX;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.ServerList {
	[BottomNavigationViewPresentation(
		"Server List",
		Resource.Id.content_viewpager,
		Resource.Id.navigationview,
		Resource.Drawable.ic_server_list,
		typeof(MainViewModel)
	)]
	public class ServerListFragment : ReportFragment<ServerListViewModel, ServerListItemVM> {
		//private IMenuItem copyItem;
		private IMenuItem searchItem;
		private MvxRecyclerView recyclerView;
		private FloatingActionButton addButton;
		private EditText searchView;
		private bool searching = false;

		public ServerListFragment() : base(Resource.Layout.server_list_page, Resource.Menu.server_list_toolbar_item) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			BackArrow.AlwaysClose = true;

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView);

			var refreshLayout = view.FindViewById<MvxSwipeRefreshLayout>(Resource.Id.mvxswiperefreshlayout);
			refreshLayout.SetColorSchemeResources(Resource.Color.accent);
			refreshLayout.SetProgressBackgroundColorSchemeResource(Resource.Color.primary);
			addButton = view.FindViewById<FloatingActionButton>(Resource.Id.add_button);
			recyclerView.AddOnScrollListener(new OnScrollListener((dx, dy) => {
				if (SelectedItem != null) {
					return;
				}
				if (dy > 0) {
					addButton.Hide();
				} else if (dy < 0) {
					addButton.Show();
				}
			}));

			searchView = this.BindingInflate(Resource.Layout.search_title, null, false) as EditText;
			searchView.SetTextAppearance(Resource.Style.MessageText_15_Regular);
			searchView.EditorAction += Searched;
			searchView.Background = null;
			var lp = new ViewGroup.MarginLayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			lp.RightMargin = 48.0f.DpToPx();
			searchView.LayoutParameters = lp;
			SetCustomTitleView(searchView);
		}

		private void Searched(object sender, TextView.EditorActionEventArgs ev) {
			if (ev.ActionId == global::Android.Views.InputMethods.ImeAction.Search) {
				if (sender is View view) {
					HideKeyboard();
				}
			}
		}

		public override void OnDestroyView() {
			if (searchView != null) {
				searchView.EditorAction -= Searched;
				searchView = null;
			}
			base.OnDestroyView();
		}

		public override void OnResume() {
			base.OnResume();
		}

		public override void OnPause() {
			base.OnPause();
			HideKeyboard();
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			inflater.Inflate(MenuId, menu);
			//copyItem = menu.FindItem(Resource.Id.copy_item);
			base.OnCreateOptionsMenu(menu, inflater);
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			if (item == searchItem) {
				ShowSearch();
				return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();

			searchItem = Menu.FindItem(Resource.Id.search_item);
			searchItem.SetClickAction(() => {
				this.OnOptionsItemSelected(searchItem);
			});
			searchItem?.SetVisible(false, false);
		}

		protected override void ActivityExit() {
			HideKeyboard();
		}

		protected override void ActivityPopEnter() {
			HideKeyboard();
		}

		protected override void CheckSelection(bool animated = true) {
			if (SelectedItem != null) {
				addButton?.Hide();
			} else {
				addButton?.Show();
			}
			base.CheckSelection(false);
		}

		protected override void ShowSelection(bool animated = true) {
			base.ShowSelection(false);
			HideSearch(true);
		}

		protected override void CloseSelection(bool animated = true) {
			if (!searching && !string.IsNullOrEmpty(ViewModel.SearchText)) {
				ShowSearch();
			} else {
				base.CloseSelection(false);
				SetUpNavigation(false);
				searchItem?.SetVisible(true, false);
			}
		}

		public override bool OnBackPressed() {
			if (searching) {
				HideSearch();
				return true;
			}
			return base.OnBackPressed();
		}

		private void ShowSearch() {
			searching = true;
			base.CloseSelection();
			SetUpNavigation(true);
			searchItem?.SetVisible(false, false);
			DisplayCustomTitle(true);
			searchView?.RequestFocus();
			ShowKeyboard(searchView);
		}

		private void HideSearch(bool showSelection = false) {
			searching = false;
			SetUpNavigation(showSelection);
			searchItem?.SetVisible(!showSelection, false);
			DisplayCustomTitle(false);
			if (!showSelection) {
				ViewModel.SearchText = string.Empty;
			}
			HideKeyboard();
		}

		public class RestoreStateRecyclerAdapter : MvxRecyclerAdapter {
			private readonly MvxRecyclerView recyclerView;
			private IParcelable recyclerViewSavedState;

			public RestoreStateRecyclerAdapter(IMvxAndroidBindingContext bindingContext, MvxRecyclerView recyclerView) : base(bindingContext) {
				this.recyclerView = recyclerView;
			}

			public override void NotifyDataSetChanged(NotifyCollectionChangedEventArgs ev) {
				bool moved = ev.Action == NotifyCollectionChangedAction.Move;
				if (moved) {
					recyclerViewSavedState = recyclerView?.GetLayoutManager()?.OnSaveInstanceState();
				}
				base.NotifyDataSetChanged(ev);
				if (moved) {
					recyclerView?.GetLayoutManager()?.OnRestoreInstanceState(recyclerViewSavedState);
				}
				if (ev.Action == NotifyCollectionChangedAction.Add) {
					recyclerView?.ScrollToPosition(0);
				}
			}
		}

		private class OnScrollListener : RecyclerView.OnScrollListener {
			private readonly Action<int, int> onScrolled;
			public OnScrollListener(Action<int, int> onScrolled) {
				this.onScrolled = onScrolled;
			}
			public override void OnScrolled(RecyclerView recyclerView, int dx, int dy) {
				base.OnScrolled(recyclerView, dx, dy);
				onScrolled?.Invoke(dx, dy);
			}
		}
	}
}
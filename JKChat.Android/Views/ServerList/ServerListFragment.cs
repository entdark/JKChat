using System;

using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;

using Google.Android.Material.Badge;
using Google.Android.Material.Button;
using Google.Android.Material.FloatingActionButton;

using JKChat.Android.Adapters;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.ServerList {
	[TabFragmentPresentation("Server List", Resource.Drawable.ic_server_list_states)]
	public class ServerListFragment : ReportFragment<ServerListViewModel, ServerListItemVM> {
		//private IMenuItem copyItem;
		private IMenuItem searchItem, filterItem;
		private BadgeDrawable filterBadgeDrawable;
		private MvxRecyclerView recyclerView;
		private FloatingActionButton addButton;
		private EditText searchView;
		private bool searching = false;

		private bool filterApplied;
		public bool FilterApplied {
			get => filterApplied;
			set {
				filterApplied = value;
				filterBadgeDrawable.SetVisible(value);
			}
		}

		public ServerListFragment() : base(Resource.Layout.server_list_page, Resource.Menu.server_list_toolbar_items) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			BackArrow.AlwaysClose = true;

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not RestoreStateRecyclerAdapter)
				recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView) {
					AdjustHolderOnBind = (viewHolder, position) => {
						if (viewHolder is IMvxRecyclerViewHolder { DataContext : ServerListItemVM item }) {
							var connectButton = viewHolder.ItemView.FindViewById<MaterialButton>(Resource.Id.connect_button);
							connectButton.ToggleIconButton(Resource.Drawable.ic_lock, item.NeedPassword);
						}
					}
				};

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
			TextViewCompat.SetTextAppearance(searchView, Resource.Style.OnSurfaceText_BodyLarge);
			searchView.EditorAction += Searched;
			var lp = new ViewGroup.MarginLayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) {
				RightMargin = 48.0f.DpToPx()
			};
			searchView.LayoutParameters = lp;
			SetCustomTitleView(searchView);

			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.FilterApplied).To(vm => vm.FilterApplied);
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

		public override void OnPause() {
			base.OnPause();
			HideKeyboard();
		}

		public override void OnResume() {
			base.OnResume();
			filterBadgeDrawable.SetVisible(FilterApplied);
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();

			searchItem = Menu.FindItem(Resource.Id.search_item);
			searchItem.SetClickAction(ShowSearch);
			searchItem?.SetVisible(false, false);

			filterItem = Menu.FindItem(Resource.Id.filter_item);
			filterItem.SetClickAction(() => {
				ViewModel?.FilterCommand?.Execute();
			});
			filterItem?.SetVisible(false, false);
			filterBadgeDrawable = BadgeDrawable.Create(Context);
			filterBadgeDrawable.BadgeGravity = BadgeDrawable.TopStart;
			filterBadgeDrawable.HorizontalOffset = 36.DpToPx();
			filterBadgeDrawable.VerticalOffset = 18.DpToPx();
			BadgeUtils.AttachBadgeDrawable(filterBadgeDrawable, (filterItem.ActionView as ViewGroup).FindViewById(Resource.Id.toolbar_menu_item), filterItem.ActionView as FrameLayout);
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
			if (!searching && !string.IsNullOrEmpty(ViewModel?.SearchText)) {
				ShowSearch();
			} else {
				base.CloseSelection(false);
				SetUpNavigation(false);
				searchItem?.SetVisible(true, false);
				filterItem?.SetVisible(true, false);
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
			filterItem?.SetVisible(false, false);
			DisplayCustomTitle(true);
			searchView?.RequestFocus();
			ShowKeyboard(searchView);
		}

		private void HideSearch(bool showSelection = false) {
			searching = false;
			SetUpNavigation(showSelection);
			searchItem?.SetVisible(!showSelection, false);
			filterItem?.SetVisible(!showSelection, false);
			DisplayCustomTitle(false);
			if (!showSelection && ViewModel != null) {
				ViewModel.SearchText = string.Empty;
			}
			HideKeyboard(searchView, true);
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
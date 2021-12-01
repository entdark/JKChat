using System.Collections.Specialized;

using Android.App;
using Android.OS;
using Android.Views;

using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList;

using MvvmCross.DroidX;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Views.ServerList {
	[MvxFragmentPresentation(typeof(MainViewModel), Resource.Id.content_frame, false)]
	public class ServerListFragment : BaseFragment<ServerListViewModel> {
		private MvxRecyclerView recyclerView;
		private IParcelable recyclerViewSavedState;

		public ServerListFragment() : base(Resource.Layout.server_list_page) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView);

			var refreshLayout = view.FindViewById<MvxSwipeRefreshLayout>(Resource.Id.mvxswiperefreshlayout);
			refreshLayout.SetColorSchemeResources(Resource.Color.accent);
			refreshLayout.SetProgressBackgroundColorSchemeResource(Resource.Color.primary);
		}

		public override void OnResume() {
			base.OnResume();
			ActionBar.SetDisplayHomeAsUpEnabled(false);
		}

		public override void OnPause() {
			base.OnPause();
		}

		private class RestoreStateRecyclerAdapter : MvxRecyclerAdapter {
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
			}
		}
	}
}
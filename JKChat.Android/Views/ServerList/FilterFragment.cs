using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

using Google.Android.Material.Divider;

using JKChat.Android.Adapters;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.TemplateSelectors;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.ServerList;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.ServerList {
	[PushFragmentPresentation]
	public class FilterFragment : BaseFragment<FilterViewModel> {
		public FilterFragment() : base(Resource.Layout.filter_page, Resource.Menu.reset_item) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not TableGroupedRecyclerViewAdapter)
				recyclerView.Adapter = new TableGroupedRecyclerViewAdapter((IMvxAndroidBindingContext)BindingContext);
			if (recyclerView.ItemTemplateSelector is not TableGroupedItemTemplateSelector)
				recyclerView.ItemTemplateSelector = new TableGroupedItemTemplateSelector(true);
			recyclerView.AddItemDecoration(new MaterialDividerItemDecoration(Context, LinearLayoutManager.Vertical));
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();

			var resetLayout = this.BindingInflate(Resource.Layout.reset_menu_item, new FrameLayout(Context));
			var resetItem = Menu.FindItem(Resource.Id.reset_item);
			resetItem.SetActionView(resetLayout);
		}
	}
}
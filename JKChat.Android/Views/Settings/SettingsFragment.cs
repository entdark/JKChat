using Android.OS;
using Android.Views;

using AndroidX.RecyclerView.Widget;

using Google.Android.Material.Divider;

using JKChat.Android.Adapters;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.TemplateSelectors;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Settings;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.Settings {
	[TabFragmentPresentation("Settings", Resource.Drawable.ic_settings_states)]
	public class SettingsFragment : BaseFragment<SettingsViewModel> {
		public SettingsFragment() : base(Resource.Layout.settings_page) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not TableGroupedRecyclerViewAdapter)
				recyclerView.Adapter = new TableGroupedRecyclerViewAdapter((IMvxAndroidBindingContext)BindingContext);
			if (recyclerView.ItemTemplateSelector is not TableGroupedItemTemplateSelector)
				recyclerView.ItemTemplateSelector = new TableGroupedItemTemplateSelector(false);
			recyclerView.AddItemDecoration(new MaterialDividerItemDecoration(Context, LinearLayoutManager.Vertical));

			SetUpNavigation(false);
		}
	}
}
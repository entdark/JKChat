//#define PROGRAMMATICAL_VIEW

using System;
using System.Collections;

using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;

using AndroidX.AppCompat.View.Menu;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.ViewPager2.Widget;

using Google.Android.Material.AppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Tabs;
using Google.Android.Material.TextView;

using JKChat.Android.Adapters;
using JKChat.Android.Controls.Listeners;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Chat;

using MvvmCross.Binding.BindingContext;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.DroidX.RecyclerView.ItemTemplates;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Binding.Views;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.Fragments;

using ContextThemeWrapper = AndroidX.AppCompat.View.ContextThemeWrapper;

namespace JKChat.Android.Views.Chat {
	[PushFragmentPresentation]
	public class ServerInfoFragment : BaseFragment<ServerInfoViewModel> {
		private IMenuItem favouriteItem;
		private MaterialButton connectButton, disconnectButton;

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set {
				needPassword = value;
				UpdateConnectButton();
			}
		}

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set {
				isFavourite = value;
				UpdateFavouriteItem();
			}
		}

		public ServerInfoFragment() : base(Resource.Layout.server_info_page, Resource.Menu.server_info_toolbar_items) {
			PostponeTransition = true;
		}
		
		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
#if PROGRAMMATICAL_VIEW
			return CreateView(inflater);
#else
			return base.OnCreateView(inflater, container, savedInstanceState);
#endif
		}
		
		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

//HACK: CollapsingToolbarLayout uses its own insets listener and does not pass insets changes to its children, so disable it
			var collapsingToolbarLayout = view.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingtoolbarlayout);
			ViewCompat.SetOnApplyWindowInsetsListener(collapsingToolbarLayout, null);

			connectButton = view.FindViewById<MaterialButton>(Resource.Id.connect_button);
			disconnectButton = view.FindViewById<MaterialButton>(Resource.Id.disconnect_button);

			var tabLayout = view.FindViewById<TabLayout>(Resource.Id.tablayout);
			var viewPager = view.FindViewById<ViewPager2>(Resource.Id.viewpager);
			if (viewPager.Adapter is not BaseRecyclerViewAdapter adapter)
				viewPager.Adapter = adapter = new BaseRecyclerViewAdapter((IMvxAndroidBindingContext)this.BindingContext) {
					ItemTemplateSelector = new ServerInfoTemplateSelector(),
#if PROGRAMMATICAL_VIEW
					ViewHolderCreator = CreateViewPagerHolder
#endif
				};
			viewPager.OffscreenPageLimit = 2;
			var mediator = new TabLayoutMediator(tabLayout, viewPager, new TabConfigurationStrategy((tab, position) => {
				string tabText = ViewModel.AllSecondaryItems[position].TabTitle;
				tab.SetText(tabText);
			}));
			mediator.Attach();

			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
			set.Bind(adapter).For(a => a.ItemsSource).To(vm => vm.AllSecondaryItems);
			set.Bind(this).For(v => v.NeedPassword).To(vm => vm.NeedPassword);
		}

		protected override void BindTitle(MvxFluentBindingDescriptionSet<IMvxFragmentView<ServerInfoViewModel>, ServerInfoViewModel> set) {}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			if (Menu is MenuBuilder menuBuilder) {
				menuBuilder.SetOptionalIconsVisible(true);
			}
			favouriteItem = Menu.FindItem(Resource.Id.favourite_item);
			favouriteItem.SetClickAction(() => {
				ViewModel?.FavouriteCommand?.Execute();
			});
			UpdateFavouriteItem();
			var shareItem = Menu.FindItem(Resource.Id.share_item);
			shareItem.SetClickAction(() => {
				ViewModel?.ShareCommand?.Execute();
			});
			var reportServerItem = Menu.FindItem(Resource.Id.report_server_item);
			reportServerItem.AdjustIconInsets();
			reportServerItem.SetClickAction(() => {
				ViewModel?.ServerReportCommand?.Execute();
			});
		}

		private void UpdateFavouriteItem() {
			favouriteItem?.SetIcon(IsFavourite ? Resource.Drawable.ic_star_filled : Resource.Drawable.ic_star_outlined);
		}

		private void UpdateConnectButton() {
			connectButton?.ToggleIconButton(Resource.Drawable.ic_lock, NeedPassword);
			disconnectButton?.ToggleIconButton(Resource.Drawable.ic_lock, NeedPassword);
		}

		private class TabConfigurationStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy {
			private readonly Action<TabLayout.Tab, int> configureTabAction;

			public TabConfigurationStrategy(Action<TabLayout.Tab, int> configureTabAction) {
				this.configureTabAction = configureTabAction;
			}

			public void OnConfigureTab(TabLayout.Tab tab, int position) {
				configureTabAction?.Invoke(tab, position);
			}
		}

		private class ServerInfoTemplateSelector : MvxTemplateSelector<ServerInfoViewModel.TabItems> {
			private const int PlayersViewType = 0;
			private const int FullInfoViewType = 1;

			public override int GetItemLayoutId(int fromViewType) {
				return fromViewType switch {
					PlayersViewType => Resource.Layout.server_info_players,
					FullInfoViewType => Resource.Layout.server_info_full,
					_ => throw new Exception("View type is invalid")
				};
			}

			protected override int SelectItemViewType(ServerInfoViewModel.TabItems forItemObject) {
				return forItemObject.TabIndex switch {
					0 => PlayersViewType,
					1 => FullInfoViewType,
					_ => throw new Exception("Item for view type is invalid")
				};
			}
		}
#region PROGRAMMATICAL_VIEW
		private View CreateView(LayoutInflater inflater) {
			this.EnsureBindingContextIsSet(inflater);
			using var _ = new MvxBindingContextStackRegistration<IMvxAndroidBindingContext>((IMvxAndroidBindingContext)BindingContext);
			var context = Context;
			var view = new FrameLayoutX(context) {
				layout => {
					layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
				},
				new LinearLayoutX(context) {
					layout => {
						layout.Orientation = Orientation.Vertical;
						layout.SetAttributeBackgroundColor(global::Android.Resource.Attribute.ColorBackground);
					},
					new RelativeLayoutX(context) {
						layout => {
							layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
							layout.SetAttributeBackgroundColor(Resource.Attribute.colorSurfaceContainerHighest);
						},
						new AppCompatImageView(context)
							.Adjust(imageView => {
								imageView.LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
									.Adjust(lp => {
										lp.AddRule(LayoutRules.AlignLeft, Resource.Id.appbarlayout);
										lp.AddRule(LayoutRules.AlignRight, Resource.Id.appbarlayout);
										lp.AddRule(LayoutRules.AlignTop, Resource.Id.appbarlayout);
										lp.AddRule(LayoutRules.AlignBottom, Resource.Id.appbarlayout);
									});
								imageView.SetScaleType(ImageView.ScaleType.CenterCrop);
								imageView.ImageTintList =
									ContextCompat.GetColorStateList(context, Resource.Color.bg_preview_tint);
								imageView.ImageTintMode = PorterDuff.Mode.SrcOver;
							})
							.Bind(this, "DrawableName ServerPreview(Game)"),
						
						new LinearLayoutX(context) {
							layout => {
								layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
								layout.Id = Resource.Id.appbarlayout;
								layout.Orientation = Orientation.Vertical;
								layout.SetWindowInsetsFlags(WindowInsetsFlags.PaddingLeftButExpanded | WindowInsetsFlags.PaddingRight | WindowInsetsFlags.PaddingTop);
							},
							new MaterialToolbar(new ContextThemeWrapper(context, Resource.Style.ToolbarTitle), null, 0) {
								LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
								Id = Resource.Id.toolbar
							},
							new MaterialTextView(new ContextThemeWrapper(context, Resource.Style.OnSurfaceText_HeadlineSmall), null, 0)
								.Adjust(textView => {
									textView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
									textView.SetPadding(16.DpToPx(), 0, 16.DpToPx(), 0);
								})
								.Bind(this, "TextFormatted ColourText(Title)"),
							new LinearLayoutX(context) {
								layout => {
									layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
									layout.Orientation = Orientation.Horizontal;
									layout.SetPadding(16.DpToPx(), 4.DpToPx(), 16.DpToPx(), 12.DpToPx());
									layout.SetGravity(GravityFlags.CenterVertical);
								},
								new Space(context) {
									LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) {
										Weight = 1.0f
									}
								},
								new MaterialButton(context, null, Resource.Attribute.materialButtonTonalStyle) {
									LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
										Weight = 0.0f
									},
									Id = Resource.Id.connect_button,
									Text = "Connect",
									IconPadding = 8.DpToPx()
								}
								.Bind(this, "Click ConnectCommand; Visibility EnumVisibility(Status, 'Disconnected')"),
								new MaterialButton(context, null, Resource.Attribute.materialButtonOutlinedStyle) {
									LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
										Weight = 0.0f
									},
									Id = Resource.Id.disconnect_button,
									Text = "Disconnect",
									IconPadding = 8.DpToPx()
								}
								.Bind(this, "Click ConnectCommand; Visibility EnumInvertedVisibility(Status, 'Disconnected')")
							}

						}
					},
					new CoordinatorLayoutX(context) {
						layout => {
							layout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) {
								Weight = 1.0f
							};
						},
						new AppBarLayoutX(context) {
							layout => {
								layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
								layout.Background = null;
							},
							new CollapsingToolbarLayoutX(context) {
								layout => {
									layout.LayoutParameters = new AppBarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) {
										BottomMargin = 12.DpToPx(),
										ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed | AppBarLayout.LayoutParams.ScrollFlagSnap | AppBarLayout.LayoutParams.ScrollFlagSnapMargins
									};
									layout.Id = Resource.Id.collapsingtoolbarlayout;
									layout.Background = null;
								},
								new MvxLinearLayout(context, null)
									.Adjust(layout => {
										layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
										layout.Orientation = Orientation.Vertical;
										layout.SetPadding(0, 8.DpToPx(), 0, 8.DpToPx());
										layout.SetAttributeBackgroundColor(Resource.Attribute.colorSurface);
										layout.ItemTemplateId = Resource.Layout.server_info_primary_item;
									})
									.Bind(this, "ItemsSource PrimaryInfoItems")
							},
							new TabLayout(context)
								.Adjust(layout => {
									layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
									layout.Id = Resource.Id.tablayout;
									layout.SetAttributeBackgroundColor(Resource.Attribute.colorSurface);
									layout.SetWindowInsetsFlags(WindowInsetsFlags.PaddingLeftButExpanded | WindowInsetsFlags.PaddingRight);
								})
						},
						new ViewPager2(context) {
							LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) {
								Behavior = new AppBarLayout.ScrollingViewBehavior()
							},
							Id = Resource.Id.viewpager
						}
					}
				}
			};
			return view;
		}

		private MvxRecyclerViewHolder CreateViewPagerHolder(ViewGroup parent, int viewType) {
			View view;
			var context = parent.Context;
			if (viewType == Resource.Layout.server_info_players) {
				view = new LinearLayoutX(context) {
					layout => {
						layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
						layout.Orientation = Orientation.Vertical;
					},
					new LinearLayoutX(context) {
						layout => {
							layout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 40.DpToPx()) {
								Weight = 0.0f
							};
							layout.Orientation = Orientation.Horizontal;
							layout.SetPadding(16.DpToPx(), 0, 24.DpToPx(), 0);
							layout.SetGravity(GravityFlags.CenterVertical);
							layout.SetAttributeBackgroundColor(global::Android.Resource.Attribute.ColorBackground);
						},
						new MaterialTextView(new ContextThemeWrapper(context, Resource.Style.OnSurfaceText_LabelMedium), null, 0)
							.Adjust(textView => {
								textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) {
									Weight = 1.0f
								};
								textView.Text = "Player";
								textView.SetLines(1);
								textView.Ellipsize = TextUtils.TruncateAt.End;
								textView.Gravity = GravityFlags.Start;
							}),
						new MaterialTextView(new ContextThemeWrapper(context, Resource.Style.OnSurfaceText_LabelMedium), null, 0)
							.Adjust(textView => {
								textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
									Weight = 0.0f
								};
								textView.Text = "Score/Death";
								textView.SetLines(1);
								textView.Ellipsize = null;
								textView.Gravity = GravityFlags.End;
							})
					},
					new MvxRecyclerView(context, null, 0, new BaseRecyclerViewAdapter((IMvxAndroidBindingContext)this.BindingContext) {
							ViewHolderCreator = (parent, viewType) => {
								var view = new FrameLayout(parent.Context) {
									LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
								};
								
								var bindigContext = new MvxAndroidBindingContext(parent.Context, (BindingContext as IMvxAndroidBindingContext)?.LayoutInflaterHolder);
								var viewHolder = new MvxRecyclerViewHolder(view, bindigContext);
								
								view
									.Bind(viewHolder, "Background PlayerTeamBackground(Team)")
									.AddView(
										height: 40.DpToPx(),
										viewCreator: context => new LinearLayout(context)
											.Adjust(layout => {
												layout.Orientation = Orientation.Horizontal;
												layout.SetPadding(16.DpToPx(), 0, 24.DpToPx(), 0);
												layout.SetGravity(GravityFlags.CenterVertical);
												layout.SetAttributeBackgroundResource(Resource.Attribute.selectableItemBackground);
												layout.SetWindowInsetsFlags(WindowInsetsFlags.PaddingLeftButExpanded | WindowInsetsFlags.PaddingRight);

												addServerInfoItem(layout, viewHolder);
											})
									);

								return viewHolder;
							}
						}
					)
					.Adjust(recyclerView => {
						recyclerView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 40.DpToPx()) {
							Weight = 1.0f
						};
						recyclerView.Id = Resource.Id.mvxrecyclerview;
						recyclerView.SetClipToPadding(false);
						recyclerView.SetWindowInsetsFlags(WindowInsetsFlags.PaddingBottom);
					})
				};
			} else {
				view = new MvxRecyclerView(parent.Context, null, 0, new BaseRecyclerViewAdapter((IMvxAndroidBindingContext)this.BindingContext) {
						ViewHolderCreator = (parent, viewType) => {
							var layout = new LinearLayout(parent.Context) {
								LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 40.DpToPx()),
								Orientation = Orientation.Horizontal,
							};
							layout.SetPadding(16.DpToPx(), 0, 24.DpToPx(), 0);
							layout.SetGravity(GravityFlags.CenterVertical);
							layout.SetAttributeBackgroundResource(Resource.Attribute.selectableItemBackground);
							layout.SetWindowInsetsFlags(WindowInsetsFlags.PaddingLeftButExpanded | WindowInsetsFlags.PaddingRight);
							
							var bindigContext = new MvxAndroidBindingContext(parent.Context, (BindingContext as IMvxAndroidBindingContext)?.LayoutInflaterHolder);
							var viewHolder = new MvxRecyclerViewHolder(layout, bindigContext);
							
							addServerInfoItem(layout, viewHolder);

							return viewHolder;
						}
					}
				)
				.Adjust(recyclerView => {
					recyclerView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
					recyclerView.Id = Resource.Id.mvxrecyclerview;
					recyclerView.SetClipToPadding(false);
					recyclerView.SetWindowInsetsFlags(WindowInsetsFlags.PaddingBottom);
				});
			}

			void addServerInfoItem(LinearLayout layout, IMvxBindingContextOwner bindingContextOwner) {
				layout
					.AddView(
						new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) {
							Weight = 1.0f
						},
						context => new MaterialTextView(new ContextThemeWrapper(context, Resource.Style.OnSurfaceText_BodyLarge), null, 0)
							.Adjust(textView => {
								textView.Id = Resource.Id.players_textview;
								textView.SetLines(1);
								textView.Ellipsize = TextUtils.TruncateAt.End;
								textView.Gravity = GravityFlags.Start;
							})
							.Bind(bindingContextOwner, "TextFormatted ColourText(Key)")
					)
					.AddView(
						new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
							Weight = 0.0f
						},
						context => new MaterialTextView(new ContextThemeWrapper(context, Resource.Style.OnSurfaceText_BodyLarge), null, 0)
							.Adjust(textView => {
								textView.Id = Resource.Id.server_name_textview;
								textView.SetLines(1);
								textView.Ellipsize = null;
								textView.Gravity = GravityFlags.End;
							})
							.Bind(bindingContextOwner, "TextFormatted ColourText(Value)")
					);
			}

			var bindingContext = new MvxAndroidBindingContext(parent.Context, (BindingContext as IMvxAndroidBindingContext)?.LayoutInflaterHolder);
			var viewHolder = new MvxRecyclerViewHolder(view, bindingContext);
			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			viewHolder.AddBindings(recyclerView, "ItemsSource Items");
			return viewHolder;
		}
#endregion
	}

	public class FrameLayoutX(Context context) : LayoutX<FrameLayout>(context);
	public class LinearLayoutX(Context context) : LayoutX<LinearLayout>(context);
	public class RelativeLayoutX(Context context) : LayoutX<RelativeLayout>(context);
	public class CoordinatorLayoutX(Context context) : LayoutX<CoordinatorLayout>(context);
	public class AppBarLayoutX(Context context) : LayoutX<AppBarLayout>(context);
	public class CollapsingToolbarLayoutX(Context context) : LayoutX<CollapsingToolbarLayout>(context);

	public abstract class LayoutX<TViewGroup> : IEnumerable where TViewGroup : ViewGroup {
		private readonly ViewGroup viewGroup;
		
		public LayoutX(Context context) {
			viewGroup = (TViewGroup)Activator.CreateInstance(typeof(TViewGroup), context);
		}
		
		public void Add(View view) {
			viewGroup.AddView(view);
		}
		public void Add(Action<TViewGroup> adjust) {
			adjust?.Invoke(viewGroup as TViewGroup);
		}

		public static implicit operator TViewGroup(LayoutX<TViewGroup> layout) => layout.viewGroup as TViewGroup;
		
		public IEnumerator GetEnumerator() => new ViewGroupEnumerator(this);
	}

	public class ViewGroupEnumerator(ViewGroup vg) : IEnumerator {
		private int index = -1;
		public bool MoveNext() {
			int i = index + 1;
			int length = vg.ChildCount;
			if (i >= length) {
				index = length;
				return false;
			}
			index = i;
			return true;
		}
		public void Reset() => index = -1;
		public object Current => vg.GetChildAt(index);
	}
}
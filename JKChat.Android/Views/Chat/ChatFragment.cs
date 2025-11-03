using System;
using System.Collections.Specialized;

using Android.Animation;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using AndroidX.AppCompat.View.Menu;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;

using Google.Android.Material.Color;

using Java.Lang;
using Java.Util.Concurrent;

using JKChat.Android.Adapters;
using JKChat.Android.Controls;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Android.Views.Main;
using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;

using Microsoft.Maui.ApplicationModel;

using MvvmCross;
using MvvmCross.Binding.Extensions;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Plugin.Messenger;

using static JKChat.Android.ValueConverters.ColourTextValueConverter;

namespace JKChat.Android.Views.Chat {
	[PushFragmentPresentation]
	public class ChatFragment : ReportFragment<ChatViewModel, ChatItemVM> {
		private IMenuItem copyItem, favouriteItem, downloadMapItem, minimapItem;
		private ImageButton sendButton, commandButton, chatTypeButton;
		private EditText messageEditText;
		private ScrollToBottomRecyclerAdapter scrollToBottomRecyclerAdapter;
		private MvxRecyclerView recyclerView;
		private MinimapView minimapView;
		private TextSwitcher centerPrintTextSwitcher;

		private string message;
		public string Message {
			get => message;
			set {
				if (string.IsNullOrEmpty(message) != string.IsNullOrEmpty(value)) {
					ScaleButton(sendButton, !string.IsNullOrEmpty(value));
					ScaleButton(commandButton, string.IsNullOrEmpty(value));
				}
				message = value;
			}
		}

		private ChatType chatType;
		public ChatType ChatType {
			get => chatType;
			set {
				if (chatType != value) {
					int tintColor = value switch {
						ChatType.Team => Resource.Color.chat_type_team,
						ChatType.Private => Resource.Color.chat_type_private,
						_ => Resource.Color.chat_type_common,
					};
					chatTypeButton.ImageTintList = global::Android.Content.Res.ColorStateList.ValueOf(new(ContextCompat.GetColor(Context, tintColor)));
				}
				chatType = value;
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

		private MapData mapData;
		public MapData MapData {
			get => mapData;
			set {
				mapData = value;
				UpdateMinimapItem();
			}
		}

		private bool mapFocused;
		public bool MapFocused {
			get => mapFocused;
			set {
				if (mapFocused != value) {
					SwapMapAndChat(value, true);
				}
				mapFocused = value;
				UpdateMinimapItem();
			}
		}

		private float mapLoadingProgress;
		public float MapLoadingProgress {
			get => mapLoadingProgress;
			set {
				int oldPercent = mapLoadingProgress.ToPercent(),
					newPercent = value.ToPercent();
				bool update = oldPercent != newPercent;
				mapLoadingProgress = value;
				UpdateMinimapItem(update);
			}
		}

		private bool showCenterPrint;
		public bool ShowCenterPrint {
			get => showCenterPrint;
			set {
				if (showCenterPrint != value) {
					FadeCenterPrint(centerPrintTextSwitcher, value);
				}
				showCenterPrint = value;
			}
		}

		public ChatFragment() : base(Resource.Layout.chat_page, Resource.Menu.chat_toolbar_items) {
			PostponeTransition = true;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var view = base.OnCreateView(inflater, container, savedInstanceState);
			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.Message).To(vm => vm.Message);
			set.Bind(this).For(v => v.ChatType).To(vm => vm.ChatType);
			set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
			set.Bind(this).For(v => v.ShowCenterPrint).To(vm => vm.ShowCenterPrint);
			set.Bind(this).For(v => v.MapData).To(vm => vm.MapData);
			set.Bind(this).For(v => v.MapFocused).To(vm => vm.MapFocused);
			set.Bind(this).For(v => v.MapLoadingProgress).To(vm => vm.MapLoadingProgress);
			return view;
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not ScrollToBottomRecyclerAdapter) {
				recyclerView.Adapter = new ScrollToBottomRecyclerAdapter((IMvxAndroidBindingContext)BindingContext) {
					AdjustHolderOnCreate = (viewHolder) => {
						var textView = viewHolder.ItemView.FindViewById<LinkTextView>(Resource.Id.message);
						textView.MovementMethod = LongClickLinkMovementMethod.Instance;

						var scrollView = viewHolder.ItemView.FindViewById<HorizontalScrollView>(Resource.Id.message_scrollview);
						scrollView?.LayoutTransition?.EnableTransitionType(LayoutTransitionType.Changing);
						scrollView?.SetOnTouchListener(new MessageScrollListener());
					},
				};
			}
			scrollToBottomRecyclerAdapter = recyclerView.Adapter as ScrollToBottomRecyclerAdapter;

			minimapView = view.FindViewById<MinimapView>(Resource.Id.minimap_view);
			SwapMapAndChat(ViewModel.MapFocused, false);

			sendButton = view.FindViewById<ImageButton>(Resource.Id.send_button);
			ScaleButton(sendButton, !string.IsNullOrEmpty(ViewModel.Message), false);

			commandButton = view.FindViewById<ImageButton>(Resource.Id.command_button);
			ScaleButton(commandButton, string.IsNullOrEmpty(ViewModel.Message), false);

			chatTypeButton = view.FindViewById<ImageButton>(Resource.Id.chat_type_button);

			messageEditText = view.FindViewById<EditText>(Resource.Id.message_edittext);
			messageEditText.AfterTextChanged += AfterMessageTextChanged;
			
			centerPrintTextSwitcher = view.FindViewById<TextSwitcher>(Resource.Id.center_print_textswitcher);
			centerPrintTextSwitcher.LayoutTransition?.EnableTransitionType(LayoutTransitionType.Changing);

			var titleView = this.BindingInflate(Resource.Layout.chat_title, null, false);
			SetCustomTitleView(titleView);
		}

		private class MessageScrollListener : Java.Lang.Object, View.IOnTouchListener {
			public bool OnTouch(View view, MotionEvent ev) {
				if (ev == null)
					return false;
				MotionEvent newEv;
				if (ev.Action != MotionEventActions.Move) {
					newEv = MotionEvent.Obtain(ev);
				} else {
					newEv = MotionEvent.Obtain(ev.DownTime, ev.EventTime, MotionEventActions.Cancel, ev.GetX(), ev.GetY(), ev.MetaState);
				}
				(view.Parent?.Parent as View)?.OnTouchEvent(newEv);
				return false;
			}
		}

		private void AfterMessageTextChanged(object sender, AfterTextChangedEventArgs ev) {
			if (ViewModel.CommandSetAutomatically) {
				ViewModel.CommandSetAutomatically = false;
				Selection.SetSelection(ev.Editable, ev.Editable.Length());
				ShowKeyboard(sender as View);
			}
		}

		public override void OnDestroyView() {
			if (messageEditText != null) {
				messageEditText.AfterTextChanged -= AfterMessageTextChanged;
				messageEditText = null;
			}
			if (recyclerView != null) {
				recyclerView.Adapter = null;
				recyclerView = null;
			}
			base.OnDestroyView();
		}

		public override void OnResume() {
			base.OnResume();
			scrollToBottomRecyclerAdapter?.ScrollToBottom();
		}

		private ActionMenuPresenter menuPresenter;
		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			if (Menu is MenuBuilder menuBuilder) {
				menuBuilder.SetOptionalIconsVisible(true);
#if false
				try {
					var field = menuBuilder.Class.GetDeclaredField("mPresenters");
					field.Accessible = true;
					var presenters = field.Get(menuBuilder);
					System.Diagnostics.Debug.WriteLine(presenters);
					var presentersList = presenters as CopyOnWriteArrayList;
					var presentersArray = presentersList?.ToArray();
					if (presentersArray != null) {
						System.Diagnostics.Debug.WriteLine(presentersArray);
						foreach (var presenterRef in presentersArray) {
							var presenter = (presenterRef as Java.Lang.Ref.WeakReference)?.Get();
							if (presenter is ActionMenuPresenter actionMenuPresenter)
								menuPresenter = actionMenuPresenter;
							System.Diagnostics.Debug.WriteLine(presenter?.GetType());
						}
					}
				} catch (System.Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
				}
#endif
			}
			favouriteItem = Menu.FindItem(Resource.Id.favourite_item);
			favouriteItem.AdjustIconInsets();
			favouriteItem.SetClickAction(() => {
				ViewModel?.FavouriteCommand?.Execute();
			});
			UpdateFavouriteItem();
			var shareItem = Menu.FindItem(Resource.Id.share_item);
			shareItem.AdjustIconInsets();
			shareItem.SetClickAction(() => {
				ViewModel?.ShareCommand?.Execute();
			});
			var infoItem = Menu.FindItem(Resource.Id.info_item);
			infoItem.AdjustIconInsets();
			infoItem.SetClickAction(() => {
				ViewModel?.ServerInfoCommand?.Execute();
			});
			downloadMapItem = Menu.FindItem(Resource.Id.download_map_item);
			downloadMapItem.AdjustIconInsets();
			downloadMapItem.SetClickAction(() => {
				ViewModel?.MapCommand?.Execute();
			});
			var reportServerItem = Menu.FindItem(Resource.Id.report_server_item);
			reportServerItem.AdjustIconInsets();
			reportServerItem.SetClickAction(() => {
				ViewModel?.ServerReportCommand?.Execute();
			});
			var disconnectItem = Menu.FindItem(Resource.Id.disconnect_item);
			disconnectItem.AdjustIconInsets();
			disconnectItem.SetClickAction(() => {
				ViewModel?.DisconnectCommand?.Execute();
			});
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
				var title = disconnectItem.TitleFormatted;
				var newTitle = new SpannableString(title);
				var errorColour = new Color(MaterialColors.GetColor(Context, Resource.Attribute.colorError, Color.Transparent));
				newTitle.SetSpan(new ForegroundColorSpan(errorColour), 0, newTitle.Length(), SpanTypes.ExclusiveExclusive);
				disconnectItem.SetTitle(newTitle);
				disconnectItem.SetIconTintList(ColorStateList.ValueOf(errorColour));
			}
			copyItem = Menu.FindItem(Resource.Id.copy_item);
			copyItem.SetClickAction(() => {
				ViewModel?.CopyCommand?.Execute(SelectedItem);
				CloseSelection();
			});
			minimapItem = Menu.FindItem(Resource.Id.minimap_item);
			minimapItem.SetClickAction(() => {
				ViewModel?.MapCommand?.Execute();
			});
			UpdateMinimapItem();
		}

		protected override void ShowSelection(bool animated = true) {
			base.ShowSelection(animated);
			copyItem?.SetVisible(true, animated);
			DisplayCustomTitle(false);
		}

		protected override void CloseSelection(bool animated = true) {
			base.CloseSelection(animated);
			copyItem?.SetVisible(false, animated);
			DisplayCustomTitle(true);
		}

		protected override void DisplayCustomTitle(bool show) {
			base.DisplayCustomTitle(SelectedItem == null);
		}

		private void UpdateFavouriteItem() {
			if (favouriteItem == null)
				return;
			favouriteItem.SetIcon(IsFavourite ? Resource.Drawable.ic_star_filled : Resource.Drawable.ic_star_outlined);
			favouriteItem.SetTitle(IsFavourite ? "Remove from favourites" : "Add to favourites");
			favouriteItem.AdjustIconInsets();
		}

		private void UpdateMinimapItem(bool updateProgress = false) {
			if (minimapItem == null || downloadMapItem == null)
				return;
			if (MapLoadingProgress.IsProgressActive()) {
				updateProgress |= !minimapItem.IsVisible;
				updateProgress |= minimapItem.Icon != null;
				updateProgress |= minimapItem.TitleFormatted == null;
				updateProgress |= downloadMapItem.IsVisible;
				if (updateProgress) {
					minimapItem.SetVisible(true);
					minimapItem.SetIcon(null);
					minimapItem.SetTitle(MapLoadingProgress.ToPercentString());
					downloadMapItem.SetVisible(false);
				}
			} else {
				minimapItem.SetVisible(MapData != null);
				minimapItem.SetIcon(MapFocused ? Resource.Drawable.ic_map_filled : Resource.Drawable.ic_map_outlined);
				minimapItem.SetTitle(null);
				downloadMapItem.SetVisible(MapData == null && AppSettings.MinimapOptions.HasFlag(MinimapOptions.Enabled));
			}
		}

		private static void ScaleButton(ImageButton view, bool show, bool animated = true) {
			float scale = show ? 1.0f : 0.0f;
			if (show)
				view.Visibility = ViewStates.Visible;
			if (!animated) {
				view.ScaleX = scale;
				view.ScaleY = scale;
				view.Alpha = scale;
				if (!show)
					view.Visibility = ViewStates.Invisible;
				return;
			}
			view.Animate()
				.ScaleX(scale)
				.ScaleY(scale)
				.Alpha(scale)
				.SetDuration(200)
				.SetInterpolator(new DecelerateInterpolator())
				.WithEndAction(new Runnable(() => {
					if (!show)
						view.Visibility = ViewStates.Invisible;
				}))
				.Start();
		}

		private void SwapMapAndChat(bool mapFocused, bool animated = true) {
			if (recyclerView == null || minimapView == null)
				return;
			float minimapAlpha = mapFocused ? 1.0f : 0.3f,
				recyclerAlpha = mapFocused ? 0.3f : 1.0f;
			if (mapFocused) {
				minimapView.BringToFront();
			} else {
				recyclerView.BringToFront();
			}
			if (!animated) {
				minimapView.Alpha = minimapAlpha;
				recyclerView.Alpha = recyclerAlpha;
				return;
			}
			minimapView.Animate()
				.Alpha(minimapAlpha)
				.SetDuration(200)
				.SetInterpolator(new DecelerateInterpolator())
				.Start();
			recyclerView.Animate()
				.Alpha(recyclerAlpha)
				.SetDuration(200)
				.SetInterpolator(new DecelerateInterpolator())
				.Start();
		}

		private static void FadeCenterPrint(TextSwitcher view, bool show, bool animated = true) {
			float alpha = show ? 1.0f : 0.0f;
			if (show)
				view.Visibility = ViewStates.Visible;
			if (!animated) {
				view.Alpha = alpha;
				if (!show)
					view.Visibility = ViewStates.Invisible;
				return;
			}
			view.Animate()
				.Alpha(alpha)
				.SetDuration(200)
				.SetInterpolator(new DecelerateInterpolator())
				.WithEndAction(new Runnable(() => {
					if (!show)
						view.Visibility = ViewStates.Invisible;
				}))
				.Start();
		}

		private class ScrollToBottomRecyclerAdapter(IMvxAndroidBindingContext bindingContext) : BaseRecyclerViewAdapter(bindingContext) {
			private bool dragging = false, touched = false;
			private MvxSubscriptionToken sentMessageMessageToken;
			private ScrollToBottomOnScrollListener onScrollListener;

			public bool ScrolledToBottom { get; set; } = true;

			private void RecyclerViewTouch(object sender, View.TouchEventArgs ev) {
				if (ev.Event.Action == MotionEventActions.Down) {
					dragging = true;
				} else if (ev.Event.Action == MotionEventActions.Up || ev.Event.Action == MotionEventActions.Cancel) {
					dragging = false;
				}
				touched = true;
				ev.Handled = false;
			}

			private void OnSentMessageMessage(SentMessageMessage message) {
				if (RecyclerView == null)
					return;
				int position = RecyclerView.ItemsSource.Count()-1;
				if (position >= 0) {
					RecyclerView.ScrollToPosition(position);
				}
			}

			public override void NotifyDataSetChanged(NotifyCollectionChangedEventArgs ev) {
				base.NotifyDataSetChanged(ev);

				bool activityResumed = Platform.CurrentActivity is MainActivity mainActivity
					&& mainActivity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed);

				if (activityResumed && ScrolledToBottom && ev.Action == NotifyCollectionChangedAction.Add && ev.NewStartingIndex >= 0 && !dragging) {
					ScrollToBottom();
				}
			}

			public void ScrollToBottom() {
				if (RecyclerView == null)
					return;
				int position = RecyclerView.ItemsSource.Count()-1;
				if (ScrolledToBottom && position >= 0) {
					RecyclerView.ScrollToPosition(position);
				}
			}

			public override void OnAttachedToRecyclerView(RecyclerView recyclerView) {
				base.OnAttachedToRecyclerView(recyclerView);
				onScrollListener = new ScrollToBottomOnScrollListener((idle) => {
					if (idle && touched) {
						this.ScrolledToBottom = idle && !RecyclerView.CanScrollVertically(2);
						touched = false;
					}
				});
				this.RecyclerView.AddOnScrollListener(onScrollListener);
				this.RecyclerView.Touch += RecyclerViewTouch;
				sentMessageMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<SentMessageMessage>(OnSentMessageMessage);
			}

			public override void OnDetachedFromRecyclerView(RecyclerView recyclerView) {
				if (this.RecyclerView != null) {
					this.RecyclerView.Touch -= RecyclerViewTouch;
				}
				if (sentMessageMessageToken != null) {
					Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<SentMessageMessage>(sentMessageMessageToken);
					sentMessageMessageToken = null;
				}
				if (onScrollListener != null) {
					recyclerView?.RemoveOnScrollListener(onScrollListener);
					onScrollListener = null;
				}
				base.OnDetachedFromRecyclerView(recyclerView);
			}

			private class ScrollToBottomOnScrollListener(Action<bool> scrollStateChangedCallback) : RecyclerView.OnScrollListener {
				public override void OnScrollStateChanged(RecyclerView recyclerView, int newState) {
					bool idle = newState == AndroidX.RecyclerView.Widget.RecyclerView.ScrollStateIdle;
					scrollStateChangedCallback?.Invoke(idle);
					base.OnScrollStateChanged(recyclerView, newState);
				}
			}
		}

		private class LongClickLinkMovementMethod : LinkMovementMethod {
			private static readonly int LongClickTime = ViewConfiguration.LongPressTimeout;
			private readonly Handler longClickHandler = new(Looper.MainLooper);
			private bool isLongPressed = false;

			public override bool OnTouchEvent(TextView widget, ISpannable buffer, MotionEvent ev) {
				var action = ev.Action;

				if (action == MotionEventActions.Cancel) {
					longClickHandler.RemoveCallbacksAndMessages(null);
				}

				if (action == MotionEventActions.Up || action == MotionEventActions.Down) {
					int x = (int)ev.GetX();
					int y = (int)ev.GetY();

					x -= widget.TotalPaddingLeft;
					y -= widget.TotalPaddingTop;

					x += widget.ScrollX;
					y += widget.ScrollY;

					var layout = widget.Layout;
					int line = layout.GetLineForVertical(y);
					int offset = layout.GetOffsetForHorizontal(line, x);

					var link = buffer.GetSpans(offset, offset, Java.Lang.Class.FromType(typeof(LinkClickableSpan)));

					if (action == MotionEventActions.Up) {
						longClickHandler.RemoveCallbacksAndMessages(null);
						if (link.Length != 0 && !isLongPressed) {
							(link[0] as LinkClickableSpan).OnClick(widget);
						}
						isLongPressed = false;
						//(widget.Parent.Parent as View).OnTouchEvent(ev);
					} else if (action == MotionEventActions.Down) {
						if (link.Length != 0) {
							Selection.SetSelection(buffer, buffer.GetSpanStart(link[0]), buffer.GetSpanEnd(link[0]));
						} else {
							//(widget.Parent.Parent as View);
						}

						longClickHandler.PostDelayed(() => {
							isLongPressed = true;
							(widget.Parent.Parent as View).PerformLongClick();
						}, LongClickTime);
					}
					//base.OnTouchEvent(widget, buffer, ev);
					return true;
				}

				return base.OnTouchEvent(widget, buffer, ev);
			}


			public new static IMovementMethod Instance {
				get {
					instance ??= new LongClickLinkMovementMethod();
					return instance;
				}
			}
			private static LongClickLinkMovementMethod instance;
		}
	}
}
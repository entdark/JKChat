using System;
using System.Collections.Specialized;
using System.Linq;

using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using AndroidX.AppCompat.View.Menu;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;

using Java.Lang;

using JKChat.Android.Adapters;
using JKChat.Android.Controls;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Android.Views.Main;
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
		private IMenuItem copyItem, favouriteItem;
		private ImageButton sendButton, commandButton, chatTypeButton;
		private EditText messageEditText;
		private ScrollToBottomRecyclerAdapter scrollToBottomRecyclerAdapter;
		private MvxRecyclerView recyclerView;

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
					chatTypeButton.ImageTintList = global::Android.Content.Res.ColorStateList.ValueOf(new global::Android.Graphics.Color(ContextCompat.GetColor(Context, tintColor)));
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

		public ChatFragment() : base(Resource.Layout.chat_page, Resource.Menu.chat_toolbar_items) {
			PostponeTransition = true;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var view = base.OnCreateView(inflater, container, savedInstanceState);
			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.Message).To(vm => vm.Message);
			set.Bind(this).For(v => v.ChatType).To(vm => vm.ChatType);
			set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
			return view;
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not ScrollToBottomRecyclerAdapter) {
				recyclerView.Adapter = scrollToBottomRecyclerAdapter = new ScrollToBottomRecyclerAdapter((IMvxAndroidBindingContext)BindingContext) {
					AdjustHolderOnBind = (viewHolder, position) => {
						var textView = viewHolder.ItemView.FindViewById<LinkTextView>(Resource.Id.message);
						textView.MovementMethod = LongClickLinkMovementMethod.Instance;
					}
				};
			}
			scrollToBottomRecyclerAdapter = recyclerView.Adapter as ScrollToBottomRecyclerAdapter;

			sendButton = view.FindViewById<ImageButton>(Resource.Id.send_button);
			ScaleButton(sendButton, !string.IsNullOrEmpty(ViewModel.Message), false);

			commandButton = view.FindViewById<ImageButton>(Resource.Id.command_button);
			ScaleButton(commandButton, string.IsNullOrEmpty(ViewModel.Message), false);

			chatTypeButton = view.FindViewById<ImageButton>(Resource.Id.chat_type_button);

			messageEditText = view.FindViewById<EditText>(Resource.Id.message_edittext);
			messageEditText.AfterTextChanged += AfterMessageTextChanged;

			var titleView = this.BindingInflate(Resource.Layout.chat_title, null, false);
			SetCustomTitleView(titleView);
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

		public override void OnDestroy() {
			base.OnDestroy();
		}

		public override void OnPause() {
			base.OnPause();
		}

		public override void OnResume() {
			base.OnResume();
			scrollToBottomRecyclerAdapter?.ScrollToBottom();
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			if (Menu is MenuBuilder menuBuilder) {
				menuBuilder.SetOptionalIconsVisible(true);
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
			copyItem = Menu.FindItem(Resource.Id.copy_item);
			copyItem.SetClickAction(() => {
				ViewModel?.CopyCommand?.Execute(SelectedItem);
				CloseSelection();
			});
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

		private static void ScaleButton(View view, bool show, bool animated = true) {
			float scale = show ? 1.0f : 0.0f;
			if (show)
				view.Visibility = ViewStates.Visible;
			if (!animated) {
				view.ScaleX = scale;
				view.ScaleY = scale;
				view.Alpha = scale;
				if (!show)
					view.Visibility = ViewStates.Gone;
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
						view.Visibility = ViewStates.Gone;
				}))
				.Start();
		}

		private class ScrollToBottomRecyclerAdapter : BaseRecyclerViewAdapter {
			private bool dragging = false, touched = false;
			private MvxSubscriptionToken sentMessageMessageToken;
			private ScrollToBottomOnScrollListener onScrollListener;

			public bool ScrolledToBottom { get; set; } = true;

			public ScrollToBottomRecyclerAdapter(IMvxAndroidBindingContext bindingContext) : base(bindingContext) {
			}

			private void RecyclerViewTouch(object sender, View.TouchEventArgs ev) {
				if (ev.Event.Action == MotionEventActions.Down) {
					dragging = true;
				} else if (ev.Event.Action == MotionEventActions.Up) {
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
					return;
					int visiblePosition;
					if (RecyclerView.GetLayoutManager() is LinearLayoutManager linearLayoutManager) {
						visiblePosition = linearLayoutManager.FindLastVisibleItemPosition();
					} else {
						visiblePosition = -1;
					}
					if (visiblePosition == -1 || (position - visiblePosition) > 5) {
						RecyclerView.ScrollToPosition(position);
					} else {
						RecyclerView.SmoothScrollToPosition(position);
					}
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

			private class ScrollToBottomOnScrollListener : RecyclerView.OnScrollListener {
				private Action<bool> scrollStateChangedCallback;
				public ScrollToBottomOnScrollListener(Action<bool> scrollStateChangedCallback) {
					this.scrollStateChangedCallback = scrollStateChangedCallback;
				}
				public override void OnScrollStateChanged(RecyclerView recyclerView, int newState) {
					bool idle = newState == AndroidX.RecyclerView.Widget.RecyclerView.ScrollStateIdle;
					scrollStateChangedCallback?.Invoke(idle);
					base.OnScrollStateChanged(recyclerView, newState);
				}
			}
		}

		public class LongClickLinkMovementMethod : LinkMovementMethod {
			private static readonly int LongClickTime = ViewConfiguration.LongPressTimeout;
			private readonly Handler longClickHandler = new Handler(Looper.MainLooper);
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

					Layout layout = widget.Layout;
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
					if (instance == null) {
						instance = new LongClickLinkMovementMethod();
					}
					return instance;
				}
			}
			private static LongClickLinkMovementMethod instance;
		}
	}
}
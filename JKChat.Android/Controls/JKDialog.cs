using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

using JKChat.Android.Helpers;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;

using Xamarin.Essentials;

namespace JKChat.Android.Controls {
	public class JKDialog : Dialog {
		private TextView titleTextView, messageTextView;
		private Button leftButton, rightButton;
		private EditText inputEditText;
		private View dialog, messageView, inputView;
		private JKDialogRecyclerView listView;
		private readonly JKDialogConfig config;
		private readonly TaskCompletionSource<object> tcs;

		private string message => messageTextView?.Text;
		private string input => inputEditText?.Text;
		private DialogItemVM selectedItem => config.ListViewModel.Items.Find(item => item.IsSelected);

		public static int MaxScrollHeight {
			get {
				const int margin = 48;
				const int maxHeight = 312;
				int activityHeight;
				var activity = Platform.CurrentActivity;
				var decorView = activity.Window.DecorView;
				if (activity.Resources.Configuration.Orientation == global::Android.Content.Res.Orientation.Landscape) {
					activityHeight = Math.Min(decorView.Height, decorView.Width);
				} else {
					activityHeight = Math.Max(decorView.Height, decorView.Width);
				}
				return Math.Min(maxHeight.DpToPx(), activityHeight - 4*margin.DpToPx());
			}
		}

		public JKDialog(Context context, JKDialogConfig config, TaskCompletionSource<object> tcs) : base(context) {
			this.config = config;
			this.tcs = tcs;
		}

		public JKDialog(Context context, int themeResId, JKDialogConfig config, TaskCompletionSource<object> tcs) : base(context, themeResId) {
			this.config = config;
			this.tcs = tcs;
		}

		public JKDialog(Context context) : base(context) {
		}

		public JKDialog(Context context, int themeResId) : base(context, themeResId) {
		}

		protected JKDialog(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		protected JKDialog(Context context, bool cancelable, EventHandler cancelHandler) : base(context, cancelable, cancelHandler) {
		}

		protected JKDialog(Context context, bool cancelable, IDialogInterfaceOnCancelListener cancelListener) : base(context, cancelable, cancelListener) {
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.dialog_window);

			dialog = FindViewById(Resource.Id.dialog);

			titleTextView = FindViewById<TextView>(Resource.Id.title);
			titleTextView.Text = config?.Title;

			messageTextView = FindViewById<TextView>(Resource.Id.message);
			messageView = FindViewById(Resource.Id.message_view);
			if (!string.IsNullOrEmpty(config?.Message)) {
				messageTextView.Text = config?.Message;
				dialog.Background = Context.GetDrawable(Resource.Color.dialog_title_background);
			} else {
				messageView.Visibility = ViewStates.Gone;
			}

			leftButton = FindViewById<Button>(Resource.Id.left_button);
			if (!string.IsNullOrEmpty(config?.LeftButton)) {
				leftButton.Text = config?.LeftButton;
				leftButton.Click += LeftButtonClick;
			} else {
				leftButton.Visibility = ViewStates.Gone;
			}

			rightButton = FindViewById<Button>(Resource.Id.right_button);
			rightButton.Text = config?.RightButton;
			rightButton.Click += RightButtonClick;

			inputEditText = FindViewById<EditText>(Resource.Id.input);
			inputEditText.Text = config?.Input;

			listView = FindViewById<JKDialogRecyclerView>(Resource.Id.list);
			if (config?.ListViewModel != null) {
				listView.Adapter = new MvxRecyclerAdapter(new MvxAndroidBindingContext(Context, new MvxSimpleLayoutInflaterHolder(LayoutInflater), config?.ListViewModel));
				listView.ItemsSource = config?.ListViewModel.Items;
				listView.ItemClick = config?.ListViewModel.ItemClickCommand;
				dialog.Background = Context.GetDrawable(Resource.Color.dialog_title_background);
			} else {
				listView.Visibility = ViewStates.Gone;
			}

			inputView = FindViewById(Resource.Id.input_view);

			if (config == null) {
				messageView.Visibility = ViewStates.Gone;
				inputView.Visibility = ViewStates.Gone;
				listView.Visibility = ViewStates.Gone;
			} else {
				if ((config.Type & JKDialogType.Title) == 0) {
					titleTextView.Visibility = ViewStates.Gone;
				}
				if ((config.Type & JKDialogType.Message) == 0) {
					messageView.Visibility = ViewStates.Gone;
				}
				if ((config.Type & JKDialogType.Input) == 0) {
					inputView.Visibility = ViewStates.Gone;
				}
				if ((config.Type & JKDialogType.List) == 0) {
					listView.Visibility = ViewStates.Gone;
				}
			}
		}

		public override void Dismiss() {
			ButtonClick(config?.BackgroundClick);
		}

		private void LeftButtonClick(object sender, EventArgs ev) {
			ButtonClick(config?.LeftClick);
		}

		private void RightButtonClick(object sender, EventArgs ev) {
			ButtonClick(config?.RightClick);
		}

		private void ButtonClick(Action<object> action) {
			object obj;
			if ((config.Type & JKDialogType.Input) != 0) {
				obj = input;
			} else if ((config.Type & JKDialogType.List) != 0) {
				obj = selectedItem;
			} else if ((config.Type & JKDialogType.Message) != 0) {
				obj = message;
			} else {
				obj = null;
			}
			action?.Invoke(obj);
			config?.AnyClick?.Invoke(obj);

			var view = CurrentFocus;
			if (view != null) {
				var imm = (InputMethodManager)Context.GetSystemService(Context.InputMethodService);
				imm.HideSoftInputFromWindow(view.WindowToken, 0);
			}

			base.Dismiss();
			tcs?.TrySetResult(null);
		}
	}

	[Register("JKChat.Android.Controls.JKDialogRecyclerView")]
	public class JKDialogRecyclerView : MvxRecyclerView {
		public JKDialogRecyclerView(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		public JKDialogRecyclerView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {
		}

		public JKDialogRecyclerView(Context context, IAttributeSet attrs, int defStyle, IMvxRecyclerAdapter adapter) : base(context, attrs, defStyle, adapter) {
		}

		protected JKDialogRecyclerView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
			heightMeasureSpec = MeasureSpec.MakeMeasureSpec(JKDialog.MaxScrollHeight, MeasureSpecMode.AtMost);
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}
	}

	[Register("JKChat.Android.Controls.JKDialogScrollView")]
	public class JKDialogScrollView : ScrollView {
		public JKDialogScrollView(Context context) : base(context) {
		}

		public JKDialogScrollView(Context context, IAttributeSet attrs) : base(context, attrs) {
		}

		public JKDialogScrollView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) {
		}

		public JKDialogScrollView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) {
		}

		protected JKDialogScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
			heightMeasureSpec = MeasureSpec.MakeMeasureSpec(JKDialog.MaxScrollHeight, MeasureSpecMode.AtMost);
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}
	}
}
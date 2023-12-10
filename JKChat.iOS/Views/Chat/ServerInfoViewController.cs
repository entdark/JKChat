using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using Foundation;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Chat;
using JKChat.iOS.ValueConverters;
using JKChat.iOS.Views.Base;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

namespace JKChat.iOS.Views.Chat;

[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = false)]
public partial class ServerInfoViewController : BaseViewController<ServerInfoViewModel> {
	private UIBarButtonItem shareButtonItem, favouriteButtonItem;

	public override string Title {
		get => base.Title;
		set => base.Title = null;
	}

	private bool needPassword;
	public bool NeedPassword {
		get => needPassword;
		set {
			needPassword = value;
			ConnectButton.SetImage(needPassword ? Theme.Image.Lock_Medium : null, UIControlState.Normal);
		}
	}

	private bool isFavourite;
	public bool IsFavourite {
		get => isFavourite;
		set {
			isFavourite = value;
			UpdateButtonItems();
		}
	}

	public ServerInfoViewController() : base(nameof(ServerInfoViewController), null) {
	}

	public override void ViewDidLoad() {
		base.ViewDidLoad();
		
		var segmentedControl = new UISegmentedControl(new []{ "Scoreboard", "Server info" }) {
			SelectedSegment = 0,
			TranslatesAutoresizingMaskIntoConstraints = false
		};
		var source = new ServerInfoViewSource(ServerInfoTableView, segmentedControl);

		var tintedStyle = UIButtonConfiguration.TintedButtonConfiguration;
		tintedStyle.CornerStyle = UIButtonConfigurationCornerStyle.Capsule;
		var grayStyle = UIButtonConfiguration.GrayButtonConfiguration;
		grayStyle.CornerStyle = UIButtonConfigurationCornerStyle.Capsule;
		using var set = this.CreateBindingSet();
		set.Bind(source).For(s => s.ItemsSource).To(vm => vm.AllItems);
		set.Bind(source).For(s => s.SelectedSegment).To(vm => vm.SelectedTab);
		set.Bind(segmentedControl).For(s => s.SelectedSegment).To(vm => vm.SelectedTab);
		set.Bind(TitleLabel).For(v => v.AttributedText).To(vm => vm.Title).WithConversion("ColourText");
		set.Bind(StatusLabel).For(v => v.Text).To(vm => vm.Status);
		set.Bind(StatusLabel).For(v => v.TextColor).To(vm => vm.Status).WithDictionaryConversion(new Dictionary<ConnectionStatus, UIColor>() {
			[ConnectionStatus.Connected] = UIColor.Label
		}, UIColor.SecondaryLabel);
		set.Bind(StatusImageView).For(v => v.TintColor).To(vm => vm.Status).WithConversion("ConnectionColor");
		set.Bind(ConnectButton).To(vm => vm.ConnectCommand);
		set.Bind(ConnectButton).For(v => v.Configuration).To(vm => vm.Status).WithDictionaryConversion(new Dictionary<ConnectionStatus, UIButtonConfiguration>() {
			[ConnectionStatus.Disconnected] = tintedStyle
		}, grayStyle);
		set.Bind(ConnectButton).For("Title").To("If(EnumBool(Status, 'Disconnected'), 'Connect', 'Disconnect')");
		set.Bind(this).For(v => v.NeedPassword).To(vm => vm.NeedPassword);
		set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
		set.Bind(PreviewImageView).For(v => v.Image).To(vm => vm.Game).WithDictionaryConversion(new Dictionary<Game, UIImage>() {
			[Game.JediAcademy] = Theme.Image.JAPreviewBackground,
			[Game.JediOutcast] = Theme.Image.JOPreviewBackground,
			[Game.Quake3] = Theme.Image.Q3PreviewBackground
		}, null);
		set.Bind(PreviewView).For("Visibility").To("EnumBool(Game, 'Unknown')").WithConversion("InvertedVisibility");
	}

	public override void ViewWillAppear(bool animated) {
		base.ViewWillAppear(animated);

		UpdateButtonItems();

		NavigationController.NavigationBar.Translucent = true;
		NavigationController.NavigationBar.Opaque = false;
	}

	private void UpdateButtonItems() {
		shareButtonItem = new UIBarButtonItem(Theme.Image.SquareAndArrowUp, UIBarButtonItemStyle.Plain, (ev, sender) => {
			ViewModel.ShareCommand?.Execute();
		});
		favouriteButtonItem = new UIBarButtonItem(IsFavourite ? Theme.Image.StarFill : Theme.Image.Star, UIBarButtonItemStyle.Plain, (ev, sender) => {
			ViewModel.FavouriteCommand?.Execute();
		});
		NavigationItem.SetRightBarButtonItems(new []{ shareButtonItem, favouriteButtonItem }, true);
	}

	private class ServerInfoViewSource : MvxStandardTableViewSource {
		private readonly UISegmentedControl segmentedControl;
		private UIView scoreboardView;

		private int selectedSegment;
		public int SelectedSegment {
			get => selectedSegment;
			set {
				selectedSegment = value;
				if (scoreboardView != null) {
					scoreboardView.Hidden = selectedSegment != 0;
//					TableView.BeginUpdates();
//					TableView.EndUpdates();
				}
			}
		}

		public IList<KeyValueItemVM> Items => ItemsSource as IList<KeyValueItemVM>;

		public ServerInfoViewSource(UITableView tableView, UISegmentedControl segmentedControl) : base(tableView) {
			tableView.Source = this;
			tableView.SectionHeaderTopPadding = 0.0f;
			this.segmentedControl = segmentedControl;
			UseAnimations = true;
			AddAnimation = UITableViewRowAnimation.Automatic;
			RemoveAnimation = UITableViewRowAnimation.Automatic;
			ReplaceAnimation = UITableViewRowAnimation.Automatic;
		}

		public override nint NumberOfSections(UITableView tableView) {
			return 2;
		}

		public override nint RowsInSection(UITableView tableview, nint section) {
			return section == 0 ? 4 : Math.Max(Items.Count-4, 0);
		}

		protected override object GetItemAt(NSIndexPath indexPath) {
			return indexPath.Section == 0 ? Items[indexPath.Row] : Items[indexPath.Row + 4];
		}

		protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item) {
			if (indexPath.Section == 0)
				return new KeyValuePrimaryViewCell();
			else
				return new KeyValueSecondaryViewCell();
		}

		public override UIView GetViewForHeader(UITableView tableView, nint section) {
			if (section == 0)
				return new UIView();
			var headerView = new UIView() {
				BackgroundColor = UIColor.TertiarySystemBackground
			};
			if (segmentedControl.Superview != null)
				segmentedControl.RemoveFromSuperview();
			var segmentsView = new UIView();
			segmentsView.HeightAnchor.ConstraintEqualTo(60.0f).Active = true;
			segmentsView.AddSubview(segmentedControl);
			segmentsView.SafeAreaLayoutGuide.LeadingAnchor.ConstraintEqualTo(segmentedControl.LeadingAnchor, -16.0f).Active = true;
			segmentsView.SafeAreaLayoutGuide.TrailingAnchor.ConstraintEqualTo(segmentedControl.TrailingAnchor, 16.0f).Active = true;
			segmentsView.CenterYAnchor.ConstraintEqualTo(segmentedControl.CenterYAnchor, 0.0f).Active = true;
			scoreboardView = new UIView() {
				Hidden = SelectedSegment != 0
			};
			var playerLabel = new UILabel() {
				Text = "Player".ToUpper(),
				TextColor = UIColor.SecondaryLabel,
				Font = UIFont.PreferredFootnote,
				TextAlignment = UITextAlignment.Left,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			var scoreLabel = new UILabel() {
				Text = "Score/Death".ToUpper(),
				TextColor = UIColor.SecondaryLabel,
				Font = UIFont.PreferredFootnote,
				TextAlignment = UITextAlignment.Right,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			scoreboardView.HeightAnchor.ConstraintEqualTo(23.0f).Active = true;
			scoreboardView.AddSubview(playerLabel);
			scoreboardView.AddSubview(scoreLabel);
			scoreboardView.SafeAreaLayoutGuide.LeadingAnchor.ConstraintEqualTo(playerLabel.LeadingAnchor, -16.0f).Active = true;
			scoreboardView.TopAnchor.ConstraintEqualTo(playerLabel.TopAnchor, 0.0f).Active = true;
			scoreboardView.SafeAreaLayoutGuide.TrailingAnchor.ConstraintEqualTo(scoreLabel.TrailingAnchor, 16.0f).Active = true;
			scoreboardView.TopAnchor.ConstraintEqualTo(scoreLabel.TopAnchor, 0.0f).Active = true;
			playerLabel.TrailingAnchor.ConstraintEqualTo(scoreLabel.LeadingAnchor, 16.0f).Active = true;
			var headerStackView = new UIStackView(new []{ segmentsView, scoreboardView }) {
				Axis = UILayoutConstraintAxis.Vertical,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			headerView.AddSubview(headerStackView);
			headerView.LeadingAnchor.ConstraintEqualTo(headerStackView.LeadingAnchor, 0.0f).Active = true;
			headerView.TrailingAnchor.ConstraintEqualTo(headerStackView.TrailingAnchor, 0.0f).Active = true;
			headerView.TopAnchor.ConstraintEqualTo(headerStackView.TopAnchor, 0.0f).Active = true;
			headerView.BottomAnchor.ConstraintEqualTo(headerStackView.BottomAnchor, 0.0f).Active = true;
			return headerView;
		}

		public override nfloat GetHeightForHeader(UITableView tableView, nint section) {
			return section == 0 ? 0.0f : (SelectedSegment == 0 ? 83.0f : 60.0f);
		}

		protected override void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
			if (NSThread.IsMain) {
				action();
			} else {
				InvokeOnMainThread(action);
			}
			void action() {
				if (!UseAnimations) {
					ReloadTableData();
				} else if (!TryDoAnimatedChange(args)) {
					ReloadTableData();
				}
			}
		}
		protected new bool TryDoAnimatedChange(NotifyCollectionChangedEventArgs args) {
			if (args == null) {
				return false;
			}
			switch (args.Action) {
			case NotifyCollectionChangedAction.Add: {
				var array = CreateNSIndexPathArray(args.NewStartingIndex, args.NewItems.Count);
				TableView.InsertRows(array, AddAnimation);
				return true;
			}
			case NotifyCollectionChangedAction.Remove: {
				var array = CreateNSIndexPathArray(args.OldStartingIndex, args.OldItems.Count);
				TableView.DeleteRows(array, RemoveAnimation);
				return true;
			}
			case NotifyCollectionChangedAction.Move: {
				if (args.NewItems!.Count != 1 && args.OldItems.Count != 1) {
					return false;
				}
				var oldIndexPath = CreateNSIndexPath(args.OldStartingIndex);
				var newIndexPath = CreateNSIndexPath(args.NewStartingIndex);
				TableView.MoveRow(oldIndexPath, newIndexPath);
				return true;
			}
			case NotifyCollectionChangedAction.Replace: {
				if (args.NewItems.Count != args.OldItems.Count) {
					return false;
				}
				var array = CreateNSIndexPathArray(args.NewStartingIndex, args.NewItems.Count);
				TableView.ReloadRows(array, ReplaceAnimation);
				return true;
			}
			default:
				return false;
			}
		}

		protected static new NSIndexPath []CreateNSIndexPathArray(int startingPosition, int count) {
			var array = new NSIndexPath[count];
			for (int i = 0; i < count; i++) {
				array[i] = CreateNSIndexPath(i + startingPosition);
			}
			return array;
		}

		private static NSIndexPath CreateNSIndexPath(int position) {
			int row = position >= 4 ? (position - 4) : position;
			int section = position >= 4 ? 1 : 0;
			return NSIndexPath.FromRowSection(row, section);
		}

		private class KeyValuePrimaryViewCell : MvxTableViewCell {
			public static readonly NSString Key = new(nameof(KeyValuePrimaryViewCell));

			private string title;
			public string Title {
				get => title;
				set {
					title = value;
					SetValues();
				}
			}
			
			private string subtitle;
			public string Subtitle {
				get => subtitle;
				set {
					subtitle = value;
					SetValues();
				}
			}

			public KeyValuePrimaryViewCell() : base(string.Empty, UITableViewCellStyle.Value1, Key) {
				BackgroundColor = UIColor.TertiarySystemBackground;
				this.DelayBind(() => {
					using var set = this.CreateBindingSet<KeyValuePrimaryViewCell, KeyValueItemVM>();
					set.Bind(this).For(v => v.Title).To(vm => vm.Value);
					set.Bind(this).For(v => v.Subtitle).To(vm => vm.Key);
				});
			}

			private void SetValues() {
				var config = UIListContentConfiguration.ValueCellConfiguration;
				config.PrefersSideBySideTextAndSecondaryText = false;
				config.AttributedText = ColourTextValueConverter.Convert(title);
				config.SecondaryText = subtitle;
				ContentConfiguration = config;
				LayoutSubviews();
			}
		}

		private class KeyValueSecondaryViewCell : MvxTableViewCell {
			public static readonly NSString Key = new(nameof(KeyValueSecondaryViewCell));

			public KeyValueSecondaryViewCell() : base(string.Empty, UITableViewCellStyle.Value1, Key) {
				BackgroundColor = UIColor.TertiarySystemBackground;
				this.DelayBind(() => {
					using var set = this.CreateBindingSet<KeyValueSecondaryViewCell, KeyValueItemVM>();
					set.Bind(TextLabel).For(v => v.AttributedText).To(vm => vm.Key).WithConversion("ColourText");
					set.Bind(DetailTextLabel).For(v => v.AttributedText).To(vm => vm.Value).WithConversion("ColourText");
				});
			}
		}
	}
}
using System.Collections.Specialized;

using Foundation;

using JKChat.iOS.Views.ServerList.Cells;

using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.ViewSources {
	public class ServerListTableViewSource : MvxStandardTableViewSource {
		public ServerListTableViewSource(UITableView tableView) : base(tableView, ServerListViewCell.Key) {
			tableView.Source = this;
			tableView.RegisterNibForCellReuse(ServerListViewCell.Nib, ServerListViewCell.Key);
			this.UseAnimations = true;
			this.AddAnimation = UITableViewRowAnimation.Automatic;
			this.RemoveAnimation = UITableViewRowAnimation.Automatic;
		}

		protected override void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args){
			base.CollectionChangedOnCollectionChanged(sender, args);
			if (args.Action == NotifyCollectionChangedAction.Add) {
				TableView.ScrollToRow(NSIndexPath.FromRowSection(0, 0), UITableViewScrollPosition.Top, true);
			}
		}
	}
}
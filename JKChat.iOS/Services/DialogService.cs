using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.iOS.Controls.JKDialog;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.iOS.Services {
	public class DialogService : IDialogService {
		public async Task ShowAsync(JKDialogConfig config) {
			await MainThread.InvokeOnMainThreadAsync(showAsync);
			async Task showAsync() {
				var dialog = new JKDialogViewController(config);
				var viewController = Platform.GetCurrentUIViewController();
				if (viewController is JKDialogViewController && viewController.PresentingViewController != null) {
					viewController = viewController.PresentingViewController;
				}
				await viewController.PresentViewControllerAsync(dialog, true);
			}
		}
	}
}
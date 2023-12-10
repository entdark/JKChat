using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.iOS.Controls.JKDialog;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.iOS.Services {
	public class DialogService : IDialogService {
		public async Task ShowAsync(JKDialogConfig config) {
			await MainThread.InvokeOnMainThreadAsync(() => Show(config));
		}

		public static void Show(JKDialogConfig config) {
			MainThread.BeginInvokeOnMainThread(() => {
				var dialog = new JKDialogViewController(config);
				var viewController = Platform.GetCurrentUIViewController();
				if (viewController is JKDialogViewController && viewController.PresentingViewController != null) {
					viewController = viewController.PresentingViewController;
				}
				viewController.PresentViewController(dialog, true, null);
			});
		}
	}
}
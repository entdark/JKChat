using System.Collections.Generic;

namespace JKChat.Core.Services {
	public interface INotificationsService {
		void CancelNotifications(string tag = null);
		void ShowNotification(string title, string message, IDictionary<string, string> data, string tag = null);
		bool NotificationsEnabled { get; }
	}
}
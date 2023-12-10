using System.Collections.Generic;
using System.Diagnostics;

using JKChat.Core;
using JKChat.Core.Helpers;
using JKChat.Core.Services;
using JKChat.iOS.Helpers;

using UserNotifications;

namespace JKChat.iOS.Services {
	public class NotificationsService : INotificationsService {
		private ulong notificationId = 0L;

		public void CancelNotifications(string tag = null) {
			var notificationCenter = UNUserNotificationCenter.Current;
			if (string.IsNullOrEmpty(tag)) {
				notificationCenter.RemoveAllDeliveredNotifications();
			} else {
				notificationCenter.GetDeliveredNotifications(notifications => {
					if (notifications.IsNullOrEmpty())
						return;
					var idsToRemove = new List<string>(notifications.Length);
					foreach (var notification in notifications) {
						string id = notification.Request.Identifier;
						var nId = NotificationId.FromString(id);
						if (nId?.Tag == tag) {
							idsToRemove.Add(id);
						}
					}
					if (idsToRemove.Count > 0) {
						notificationCenter.RemoveDeliveredNotifications(idsToRemove.ToArray());
					}
				});
			}
		}

		public void ShowNotification(string title, string message, IDictionary<string, string> data, string tag = null) {
			var content = new UNMutableNotificationContent() {
				Title = title,
				Body = message,
				UserInfo = data.ToNSDictionary(),
				Sound = UNNotificationSound.Default
			};
			var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(double.Epsilon, false);
			tag ??= string.Empty;
			string requestID = new NotificationId(notificationId++, tag);
			var request = UNNotificationRequest.FromIdentifier(requestID, content, trigger);
			UNUserNotificationCenter.Current.AddNotificationRequest(request, (error) => {
				if (error != null) {
					Debug.WriteLine(error);
				}
			});
		}

		public bool NotificationsEnabled => UNUserNotificationCenter.Current.GetNotificationSettingsAsync().Result.AuthorizationStatus
			is UNAuthorizationStatus.Authorized
			or UNAuthorizationStatus.Provisional
			or UNAuthorizationStatus.Ephemeral;

		private class NotificationId {
			public string Tag { get; init; }
			public ulong Id { get; init; }
			public NotificationId() {}
			public NotificationId(ulong id, string tag) {
				Id = id;
				Tag = tag;
			}
			public override string ToString() {
				return this.Serialize(this.Id.ToString);
			}
			public static NotificationId FromString(string nId) {
				return nId.Deserialize<NotificationId>();
			}
			public static implicit operator string(NotificationId nId) {
				return nId.ToString();
			}
		}
	}
}
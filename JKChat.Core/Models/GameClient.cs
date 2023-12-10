using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.Core.ViewModels.Dialog;

using JKClient;

using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;

using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Core;
using MvvmCross.Navigation;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

using Common = JKClient.Common;

namespace JKChat.Core.Models {
	public class GameClient {
		private const int MaxChatMessages = 512;
		private JKClient.JKClient Client;
		private readonly IMvxMainThreadAsyncDispatcher mainThread;
		private readonly IMvxNavigationService navigationService;
		private readonly IDialogService dialogService;
		private readonly INotificationsService notificationsService;
		private readonly IMvxMessenger messenger;
		private readonly IMvxLifetime lifetime;
		private MvxSubscriptionToken playerNameMessageToken;
		private bool minimized = false;
		private readonly LimitedObservableCollection<ChatItemVM> pendingItems;
		private bool addingPending = false;
		private readonly HashSet<string> blockedPlayers;
		private IMvxViewModel viewModel;
		internal IMvxViewModel ViewModel {
			get => viewModel;
			set {
				viewModel = value;
				if (value != null) {
					RemoveNotifications();
					addingPending = true;
					unreadMessages = 0;
					messenger.Publish(new ServerInfoMessage(this, ServerInfo, Status));
					ChatItemVM []pendingItemsCopy;
					lock (pendingItems) {
						pendingItemsCopy = pendingItems.ToArray();
						pendingItems.Clear();
					}
					if (pendingItemsCopy.Length > 0) {
						if (DeviceInfo.Platform == DevicePlatform.Android) {
							Task.Run(async () => {
								int count = 0;
								lock (Items) {
									count = Items.Count;
								}
								if (true||count > 0) {
									await mainThread.ExecuteOnMainThreadAsync(() => {
										lock (Items) {
											Items.AddRange(pendingItemsCopy);
										}
									});
								} else {
									//await Task.Delay(200);
									await mainThread.ExecuteOnMainThreadAsync(() => {
										lock (Items) {
											Items.ReplaceWith(pendingItemsCopy);
										}
									});
									//const int maxSingleInsertion = 20;
									//foreach (var item in pendingItemsCopy.Reverse().Take(maxSingleInsertion)) {
									//	await mainThread.ExecuteOnMainThreadAsync(() => {
									//		lock (Items) {
									//			Items.Insert(0, item);
									//		}
									//	});
									//}
									//if (pendingItemsCopy.Length > maxSingleInsertion) {
									//	await mainThread.ExecuteOnMainThreadAsync(() => {
									//		lock (Items) {
									//			Items.InsertRange(0, pendingItemsCopy.Reverse().Skip(maxSingleInsertion), true);
									//		}
									//	});
									//}
								}
								addingPending = false;
							});
						} else if (DeviceInfo.Platform.IsApple()) {
							mainThread.ExecuteOnMainThreadAsync(() => {
								lock (Items) {
									Items.InsertRange(0, pendingItemsCopy);
								}
								addingPending = false;
							});
						}
					} else {
						addingPending = false;
					}
				}
			}
		}
		private ConnectionStatus status;
		internal ConnectionStatus Status {
			get => status;
			private set {
				status = value;
				messenger.Publish(new ServerInfoMessage(this, ServerInfo, value));
			}
		}
		private int unreadMessages;
		internal int UnreadMessages {
			get => unreadMessages;
			set {
				if (ViewModel == null || minimized) {
					unreadMessages = value;
					messenger.Publish(new ServerInfoMessage(this, ServerInfo, Status));
				}
			}
		}
		internal LimitedObservableCollection<ChatItemVM> Items { get; init; }
		internal ServerInfo ServerInfo { get; private set; }
		internal NetAddress Address => ServerInfo.Address;
		internal ClientInfo[] ClientInfo => Client?.ClientInfo;
		internal GameModification Modification => Client?.Modification ?? GameModification.Unknown;

		internal GameClient(ServerInfo serverInfo) {
			mainThread = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
			navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
			dialogService = Mvx.IoCProvider.Resolve<IDialogService>();
			notificationsService = Mvx.IoCProvider.Resolve<INotificationsService>();
			messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			lifetime = Mvx.IoCProvider.Resolve<IMvxLifetime>();
			pendingItems = new LimitedObservableCollection<ChatItemVM>(MaxChatMessages);
			blockedPlayers = new HashSet<string>();
			ServerInfo = serverInfo;
			Items = new LimitedObservableCollection<ChatItemVM>(MaxChatMessages);
			Status = ConnectionStatus.Disconnected;
		}

		private void OnPlayerNameMessage(PlayerNameMessage message) {
			if (Client != null) {
				Client.Name = AppSettings.PlayerName;
			}
		}

		private void ServerInfoChanged(ServerInfo serverInfo) {
			ServerInfo = serverInfo;
			messenger.Publish(new ServerInfoMessage(this, serverInfo, Status));
		}

		internal async Task Start() {
			await Helpers.Common.ExceptionalTaskRun(() => {
				if (Client != null) {
					Client.Start(ExceptionCallback);
					return;
				}
				Client = new JKClient.JKClient(JKClient.JKClient.GetKnownClientHandler(ServerInfo)) {
					Name = AppSettings.PlayerName
				};
				try {
					Client.Guid = AppSettings.PlayerId;
				} catch {}
				Client.ServerCommandExecuted += ServerCommandExecuted;
				Client.ServerInfoChanged += ServerInfoChanged;
				Client.FrameExecuted += FrameExecuted;
				Client.Start(ExceptionCallback);
				lifetime.LifetimeChanged += LifetimeChanged;
				playerNameMessageToken = messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
			});
		}

		private long lastFrameTime = 0L, lastScoresTime = 0L;
		private void FrameExecuted(long frameTime) {
			if (Status == ConnectionStatus.Connected) {
				if ((frameTime - lastScoresTime) > 2000L) {
					ExecuteCommand("score", false);
					lastScoresTime = frameTime;
				}
			}
			lastFrameTime = frameTime;
		}

		internal async Task Connect(bool ignoreDialog = true) {
			if (Status != ConnectionStatus.Disconnected) {
				return;
			}
			Status = ConnectionStatus.Connecting;
			await Start();
			if (ServerInfo.NeedPassword/* && string.IsNullOrEmpty(Client.Password)*/) {
				string password = Client.Password;
				await ShowDialog(new JKDialogConfig() {
					Title = "Enter Password",
					CancelText = "Cancel",
					CancelAction = (_) => {
						Task.Run(disconnect);
					},
					OkText = "Connect",
					OkAction = config => {
						Client.Password = config?.Input?.Text;
						Task.Run(connect);
					},
					Input = new DialogInputViewModel(password)
				});
			} else {
				await connect();
			}
			async Task connect() {
				try {
					await Client.Connect(ServerInfo);
					if (Client.Status == JKClient.ConnectionStatus.Active) {
						Status = ConnectionStatus.Connected;
					}
					if (Status != ConnectionStatus.Connected) {
						Status = ConnectionStatus.Disconnected;
					}
				} catch (Exception exception) {
					Debug.WriteLine(exception);
				}
			}
			async Task disconnect() {
				Disconnect();
				await navigationService.Close(ViewModel);
			}
		}
		private async Task Connect() {
			await Connect(true);
		}

		internal void Disconnect(bool showDisconnected = false) {
			if (Client != null && Client.Started) {
				Client.Disconnect();
				Client.Stop();
			}
			if (showDisconnected) {
				showDisconnected = Status != ConnectionStatus.Disconnected;
			}
			Status = ConnectionStatus.Disconnected;
			if (showDisconnected) {
				Task.Run(ShowDisconnected);
			}
		}

		private async Task ShowDisconnected() {
			await ShowDialog(new JKDialogConfig() {
				Title = "Disconnected",
				CancelText = "OK",
				CancelAction = (_) => {
					if (Status == ConnectionStatus.Disconnected) {
						Task.Run(CloseViewModel);
					}
				},
				OkText = "Reconnect",
				OkAction = (_) => {
					Task.Run(Connect);
				}
			});
		}

		internal void Shutdown() {
			Disconnect();
			if (Client != null) {
				Client.ServerCommandExecuted -= ServerCommandExecuted;
				Client.ServerInfoChanged -= ServerInfoChanged;
				Client.FrameExecuted -= FrameExecuted;
				Client.Dispose();
				Client = null;
			}
			lifetime.LifetimeChanged -= LifetimeChanged;
			if (playerNameMessageToken != null) {
				messenger.Unsubscribe<ServerInfoMessage>(playerNameMessageToken);
				playerNameMessageToken = null;
			}
		}

		internal async void ExecuteCommand(string cmd, bool addToChat = false) {
			if (addToChat && Client != null) {
				string cmd2 = cmd.StartsWith("/", StringComparison.Ordinal) ? cmd : ("/" + cmd);
				await AddItem(new ChatMessageItemVM(Client.Name, Client.Name+":", cmd2, Client.Version == ClientVersion.JO_v1_02));
			}
			Client?.ExecuteCommand(cmd);
		}

		private void LifetimeChanged(object sender, MvxLifetimeEventArgs ev) {
			switch (ev.LifetimeEvent) {
			case MvxLifetimeEvent.ActivatedFromMemory:
				minimized = false;
				if (ViewModel != null) {
					unreadMessages = 0;
					messenger.Publish(new ServerInfoMessage(this, ServerInfo, Status));
					RemoveNotifications();
				}
				break;
			case MvxLifetimeEvent.Deactivated:
				minimized = true;
				break;
			}
		}

		private async void ServerCommandExecuted(CommandEventArgs commandEventArgs) {
			var command = commandEventArgs.Command;
			string cmd = command[0];
			if (string.Compare(cmd, "chat", StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(cmd, "tchat", StringComparison.OrdinalIgnoreCase) == 0) {
				await AddToChat(command);
			} else if (string.Compare(cmd, "lchat", StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(cmd, "ltchat", StringComparison.OrdinalIgnoreCase) == 0) {
				await AddToLocationChat(command);
			} else if (string.Compare(cmd, "print", StringComparison.OrdinalIgnoreCase) == 0) {
				string title;
				if (string.Compare(command[1], 0, "@@@INVALID_ESCAPE_TO_MAIN", 0, 25, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "Invalid password", 0, 16, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Invalid Password";
				} else if (string.Compare(command[1], 0, "@@@SERVER_IS_FULL", 0, 17, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "Server is full.", 0, 15, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Server is Full";
				} else {
					await AddToPrint(command);
					return;
				}
				Disconnect();
				await ShowDialog(new JKDialogConfig() {
					Title = title,
					OkText = "OK",
					OkAction = _ => {
						Task.Run(CloseViewModel);
					}
				});
			} else if (string.Compare(cmd, "disconnect", StringComparison.OrdinalIgnoreCase) == 0) {
				string reason;
				if (string.Compare(command[1], 0, "@@@WAS_KICKED", 0, 13, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "was kicked", 0, 10, StringComparison.OrdinalIgnoreCase) == 0) {
					reason = "You were kicked";
				} else if (string.Compare(command[1], 0, "@@@DISCONNECTED", 0, 15, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "disconnected", 0, 12, StringComparison.OrdinalIgnoreCase) == 0) {
					reason = "You disconnected";
				} else {
					reason = command[1];
				}
				Disconnect();
				await ShowDialog(new JKDialogConfig() {
					Title = "Disconnected",
					Message = reason,
					CancelText = "OK",
					CancelAction = (_) => {
						Task.Run(CloseViewModel);
					},
					OkText = "Reconnect",
					OkAction = (_) => {
						Task.Run(Connect);
					}
				});
			}
		}

		private async Task AddToChat(Command command) {
			string fullMessage = command[1];
			int separator = fullMessage.IndexOf(Common.EscapeCharacter + ": ", StringComparison.Ordinal);
			if (separator < 0) {
				return;
			}
			separator += 3;
			string name = fullMessage[..separator];
			string playerName = name.Replace(Common.EscapeCharacter, string.Empty, StringComparison.Ordinal);
			string escapedPlayerName = GetEscapedPlayerName(name);
			if (blockedPlayers.Contains(escapedPlayerName))
				return;
			string message = fullMessage[separator..].Replace(Common.EscapeCharacter, string.Empty, StringComparison.Ordinal);
			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, message, Client?.Version == ClientVersion.JO_v1_02);
			ProcessItemForNotifications(chatItem, command);
			await AddItem(chatItem);
		}

		private async Task AddToLocationChat(Command command) {
			if (command.Length < 4) {
				return;
			}
			string name = command[1];
			string location = command[2];
			string colour = command[3];
			string message = command[4];

			string playerName = name.Replace(Common.EscapeCharacter, string.Empty, StringComparison.Ordinal);
			string escapedPlayerName = GetEscapedPlayerName(name);
			if (blockedPlayers.Contains(escapedPlayerName))
				return;

			var stringBuilder = new StringBuilder();
			stringBuilder
				.Append("^7<")
				.Append(location)
				.Append("> ^")
				.Append(colour)
				.Append(message);
			string fullMessage = stringBuilder.ToString().Replace(Common.EscapeCharacter, string.Empty, StringComparison.Ordinal);

			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, fullMessage, Client?.Version == ClientVersion.JO_v1_02);
			ProcessItemForNotifications(chatItem, command);
			await AddItem(chatItem);
		}

		private static string GetEscapedPlayerName(string playerName) {
			const string escapeColon = Common.EscapeCharacter + ": ";
			const string escapeStartTeam = Common.EscapeCharacter + "(";
			const string escapeEndTeam = Common.EscapeCharacter + ")";
			const string escapeStartPrivate = Common.EscapeCharacter + "[";
			const string escapeEndPrivate = Common.EscapeCharacter + "]";

			int endIndex = playerName.IndexOf(escapeEndTeam, StringComparison.Ordinal);
			if (endIndex < 0) endIndex = playerName.IndexOf(escapeEndPrivate, StringComparison.Ordinal);
			if (endIndex < 0) endIndex = playerName.IndexOf(escapeColon, StringComparison.Ordinal);
			if (endIndex < 0) return playerName;

			int startIndex = playerName.IndexOf(escapeStartTeam, StringComparison.Ordinal);
			if (startIndex < 0) startIndex = playerName.IndexOf(escapeStartPrivate, StringComparison.Ordinal);
			if (startIndex < 0) startIndex = -2;
			startIndex += 2;

			return playerName[startIndex..endIndex];
		}

		private static bool IsPrivateMessage(string message) => message?.Contains(Common.EscapeCharacter+"]"+Common.EscapeCharacter+": ", StringComparison.Ordinal) ?? false;

		private async Task AddToPrint(Command command) {
			string text = command[1].TrimEnd('\n');
			var chatItem = new ChatInfoItemVM(text, Client?.Version == ClientVersion.JO_v1_02);
			ProcessItemForNotifications(chatItem, command);
			await AddItem(chatItem);
		}

		private void ProcessItemForNotifications(ChatMessageItemVM messageItem, Command command) {
			var options = AppSettings.NotificationOptions;
			bool show = false;
			if (options.HasFlag(NotificationOptions.PrivateMessages) && IsPrivateMessage(command[1])) {
				show = true;
			} else if (AppSettings.NotificationKeywords is { Length: > 0 } keywords) {
				var words = messageItem.Message.CleanString().Split(new []{' ', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '!', '?', '+', '-', '='}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var word in words) {
					foreach (var keyword in keywords) {
						if (string.Compare(word, keyword, StringComparison.OrdinalIgnoreCase) == 0) {
							show = true;
							break;
						}
					}
					if (show)
						break;
				}
			}
			if (show) {
				ShowNotification(messageItem.PlayerName + messageItem.Message);
			}
		}

		private void ProcessItemForNotifications(ChatInfoItemVM infoItem, Command command) {
			var options = AppSettings.NotificationOptions;
			string text = infoItem.Text;
			if (options.HasFlag(NotificationOptions.PlayerConnects)) {
				if (text.Contains("@@@PLCONNECT", StringComparison.OrdinalIgnoreCase) is bool stringEd && stringEd
					|| (Client?.Version == ClientVersion.Q3_v1_32 && text.Contains(" connected", StringComparison.OrdinalIgnoreCase))) {
					string notification = text;
					if (stringEd) {
						notification = notification.Replace("@@@PLCONNECT", "connected", StringComparison.OrdinalIgnoreCase);
					};
					ShowNotification(notification.TrimEnd('\n'));
				}
			}
		}

		private async Task AddItem(ChatItemVM item) {
			while (addingPending);
			bool pending = false;
			lock (pendingItems) {
				lock (Items) {
					pending = DeviceInfo.Platform == DevicePlatform.Android && ViewModel == null;
					var items = pending ? pendingItems : Items;
					if (items.Count > 0) {
						ChatItemVM prevItem = null;
						if (DeviceInfo.Platform == DevicePlatform.Android) {
							prevItem = items[^1];
						} else if (DeviceInfo.Platform.IsApple()) {
							prevItem = items[0];
						}
						prevItem.BottomVMType = item.ThisVMType;
						item.TopVMType = prevItem.ThisVMType;
					}
					if (pending) {
						pendingItems.Add(item);
					}
				}
			}
			if (!pending) {
				await mainThread.ExecuteOnMainThreadAsync(() => {
					lock (Items) {
						if (DeviceInfo.Platform == DevicePlatform.Android) {
							Items.Add(item);
						} else if (DeviceInfo.Platform.IsApple()) {
							Items.Insert(0, item);
						}
					}
				});
			}
			UnreadMessages++;
		}

		internal void RemoveItem(ChatItemVM item) {
			lock (Items) {
				int removeIndex = Items.IndexOf(item);
				Items.Remove(item);
				if (Items.Count > 0 && removeIndex >= 0) {
					ChatItemVM prevItem = null, nextItem = null;
					if (DeviceInfo.Platform == DevicePlatform.Android) {
						int prevIndex = removeIndex - 1;
						int nextIndex = removeIndex;
						if (prevIndex >= 0) {
							prevItem = Items[prevIndex];
						}
						if (nextIndex < Items.Count) {
							nextItem = Items[nextIndex];
						}
					} else if (DeviceInfo.Platform.IsApple()) {
						int prevIndex = removeIndex;
						int nextIndex = removeIndex - 1;
						if (prevIndex < Items.Count) {
							prevItem = Items[prevIndex];
						}
						if (nextIndex >= 0) {
							nextItem = Items[nextIndex];
						}
					}
					if (prevItem != null) {
						prevItem.BottomVMType = nextItem?.ThisVMType;
					}
					if (nextItem != null) {
						nextItem.TopVMType = prevItem?.ThisVMType;
					}
				}
			}
		}

		internal void HideAllMessages(ChatItemVM item) {
			string playerName = (item as ChatMessageItemVM)?.EscapedPlayerName;
			if (playerName == null) {
				return;
			}
			blockedPlayers.Add(playerName);
			var removeItems = Items.Where(it => it is ChatMessageItemVM messageItem && messageItem.EscapedPlayerName == playerName).ToArray();
			foreach (var removeItem in removeItems) {
				RemoveItem(removeItem);
			}
			//Items.RemoveItems(removeItems);
		}

		public void MakeAllPending() {
			lock (pendingItems) {
				lock (Items) {
					if (false&&DeviceInfo.Platform == DevicePlatform.Android) {
						Items.AddRange(pendingItems);
						pendingItems.ReplaceWith(Items);
						Items.Clear();
					}
				}
			}
		}

		private async Task ShowDialog(JKDialogConfig config) {
			while (ViewModel == null);
			await dialogService.ShowAsync(config);
		}

		private async Task ExceptionCallback(JKClientException exception) {
			string message = Helpers.Common.GetExceptionMessage(exception);

			Disconnect();
			await ShowDialog(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				CancelText = "Copy",
				CancelAction = (_) => {
					Task.Run(copyAndClose);
				},
				OkText = "OK",
				OkAction = _ => {
					Task.Run(CloseViewModel);
				}
			});
			async Task copyAndClose() {
				await Clipboard.SetTextAsync(message);
				await CloseViewModel();
			}
		}

		private async Task CloseViewModel() {
			if (ViewModel != null) {
				await navigationService.Close(ViewModel);
			}
		}

		private void ShowNotification(string message, bool cleanMessage = true) {
			if (!AppSettings.NotificationOptions.HasFlag(NotificationOptions.Enabled))
				return;
			if (ViewModel != null && !minimized)
				return;
			string title = JKChat.Core.Helpers.ColourTextHelper.CleanString(ServerInfo.HostName);
			if (cleanMessage) {
				message = JKChat.Core.Helpers.ColourTextHelper.CleanString(message);
			}
			notificationsService.ShowNotification(
				title,
				message,
				Mvx.IoCProvider.Resolve<INavigationService>().MakeNavigationParameters($"jkchat://chat?address={Address}", Address.ToString()),
				Address.ToString()
			);
		}

		private void RemoveNotifications() {
			if (!AppSettings.NotificationOptions.HasFlag(NotificationOptions.Enabled))
				return;
			notificationsService.CancelNotifications(Address.ToString());
		}
	}
}
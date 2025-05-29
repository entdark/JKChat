using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
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

using Common = JKClient.Common;

namespace JKChat.Core.Models {
	public class GameClient {
		private const string EC = Common.EscapeCharacter;
		private const int MaxChatMessages = 512;
		private JKClient.JKClient Client;
		private Dictionary<string, string> pendingUserInfo;
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
		private readonly TasksQueue chatQueue = new();
		private ChatViewModel viewModel;
		private bool dialogOffsersReconnect = false;
		internal ChatViewModel ViewModel {
			get => viewModel;
			set {
				viewModel = value;
				if (value != null) {
					if (pendingDialogConfig != null) {
						dialogService.Show(pendingDialogConfig);
						pendingDialogConfig = null;
						dialogOffsersReconnect = false;
					}
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
							mainThread.ExecuteOnMainThreadAsync(() => {
								lock (Items) {
									Items.AddRange(pendingItemsCopy);
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
				} else {
					FrameExecuted = null;
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
		internal ClientGame ClientGame => Client.ClientGame;
		internal GameModification Modification => Client?.Modification ?? GameModification.Unknown;
		internal event Action<long> FrameExecuted;

		internal GameClient(ServerInfo serverInfo) {
			mainThread = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
			navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
			dialogService = Mvx.IoCProvider.Resolve<IDialogService>();
			notificationsService = Mvx.IoCProvider.Resolve<INotificationsService>();
			messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			lifetime = Mvx.IoCProvider.Resolve<IMvxLifetime>();
			pendingItems = new(MaxChatMessages);
			blockedPlayers = new();
			ServerInfo = serverInfo;
			Items = new(MaxChatMessages);
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
				Client = new(JKClient.JKClient.GetKnownClientHandler(ServerInfo)) {
					Name = AppSettings.PlayerName
				};
				try {
					Client.Guid = AppSettings.PlayerId;
				} catch {}
				Client.ServerCommandExecuted += ServerCommandExecuted;
				Client.EntityEventExecuted += EntityEventExecuted;
				Client.ServerInfoChanged += ServerInfoChanged;
				Client.FrameExecuted += ClientFrameExecuted;
				Client.Start(ExceptionCallback);
				if (!pendingUserInfo.IsNullOrEmpty()) {
					foreach (var keyValuePair in pendingUserInfo) {
						Client.SetUserInfoKeyValue(keyValuePair.Key, keyValuePair.Value);
					}
					pendingUserInfo = null;
				}
				lifetime.LifetimeChanged += LifetimeChanged;
				playerNameMessageToken = messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
			});
		}

		private long lastFrameTime = 0L, lastScoresTime = 0L;
		private void ClientFrameExecuted(long frameTime) {
			if (Status == ConnectionStatus.Connected) {
				if ((frameTime - lastScoresTime) > 2000L) {
					ExecuteCommand("score", false);
					lastScoresTime = frameTime;
				}
			}
			lastFrameTime = frameTime;
			FrameExecuted?.Invoke(frameTime);
			lastDisruptorEnd = Vector3.Zero;
		}

		internal async Task Connect(bool ignoreDialog = true) {
			if (!ignoreDialog && pendingDialogConfig != null && dialogOffsersReconnect) {
				return;
			}
			if (Status != ConnectionStatus.Disconnected) {
				return;
			}
			Status = ConnectionStatus.Connecting;
			await Start();
			if (ServerInfo.NeedPassword) {
				string password = Client.Password;
				ShowDialog(new JKDialogConfig() {
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
				//to make sure disconnect command is sent to the server
				Client.WaitFrames(3);
			}
			if (showDisconnected) {
				showDisconnected = Status != ConnectionStatus.Disconnected;
			}
			Status = ConnectionStatus.Disconnected;
			if (showDisconnected) {
				Task.Run(updateAfterDisconnect);
				ShowDisconnected();
			}
			async Task updateAfterDisconnect() {
				var serverInfo = await Mvx.IoCProvider.Resolve<IServerListService>().GetServerInfo(ServerInfo);
				if (serverInfo != null) {
					await Mvx.IoCProvider.Resolve<ICacheService>().UpdateServer(serverInfo);
				}
			}
		}

		private void ShowDisconnected() {
			if (ViewModel == null) {
				return;
			}
			ShowDialog(new JKDialogConfig() {
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
			}, true);
		}

		internal void Shutdown() {
			Disconnect();
			if (Client != null) {
				Client.ServerCommandExecuted -= ServerCommandExecuted;
				Client.EntityEventExecuted -= EntityEventExecuted;
				Client.ServerInfoChanged -= ServerInfoChanged;
				Client.FrameExecuted -= ClientFrameExecuted;
				Client.Dispose();
				Client = null;
			}
			lifetime.LifetimeChanged -= LifetimeChanged;
			if (playerNameMessageToken != null) {
				messenger.Unsubscribe<ServerInfoMessage>(playerNameMessageToken);
				playerNameMessageToken = null;
			}
		}

		internal void ExecuteCommand(string cmd, bool addToChat = false) {
			if (addToChat && Client != null) {
				string cmd2 = cmd.StartsWith("/", StringComparison.Ordinal) ? cmd : ("/" + cmd);
				AddItem(new ChatMessageItemVM(Client.Name, Client.Name+"^7:", cmd2, Client.Version == ClientVersion.JO_v1_02));
			}
			Client?.ExecuteCommand(cmd);
		}

		internal void SetUserInfoKeyValue(string key, string value) {
			if (Client != null) {
				Client.SetUserInfoKeyValue(key, value);
			} else {
				pendingUserInfo ??= new();
				pendingUserInfo[key] = value;
			}
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

		private void ServerCommandExecuted(CommandEventArgs commandEventArgs) {
			var command = commandEventArgs.Command;
			string cmd = command[0];
			if (string.Compare(cmd, "chat", StringComparison.Ordinal) == 0
				|| string.Compare(cmd, "tchat", StringComparison.Ordinal) == 0) {
				AddToChat(command);
			} else if (string.Compare(cmd, "lchat", StringComparison.Ordinal) == 0
				|| string.Compare(cmd, "ltchat", StringComparison.Ordinal) == 0) {
				AddToLocationChat(command);
			} else if (string.Compare(cmd, "cp", StringComparison.Ordinal) == 0) {
				if (AppSettings.CenterPrint && ViewModel != null) {
					ViewModel.CenterPrint = command[1].TrimEnd('\n');
				}
			} else if (string.Compare(cmd, "print", StringComparison.Ordinal) == 0) {
				string title;
				if (string.Compare(command[1], 0, "@@@INVALID_ESCAPE_TO_MAIN", 0, 25, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "Invalid password", 0, 16, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Invalid Password";
				} else if (string.Compare(command[1], 0, "@@@SERVER_IS_FULL", 0, 17, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command[1], 0, "Server is full.", 0, 15, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Server is Full";
				} else {
					AddToPrint(command);
					return;
				}
				Disconnect();
				ShowDialog(new JKDialogConfig() {
					Title = title,
					OkText = "OK",
					OkAction = _ => {
						Task.Run(CloseViewModel);
					}
				});
			} else if (string.Compare(cmd, "disconnect", StringComparison.Ordinal) == 0) {
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
				ShowDialog(new JKDialogConfig() {
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
				}, true);
			}
		}

		private Vector3 lastDisruptorEnd = Vector3.Zero;
		public List<EntityData> TempEntities { get; init; } = new();
		private void EntityEventExecuted(EntityEventArgs entityEventArgs) {
			Vector3 start, end;
			var entity = entityEventArgs.Entity;
			var now = DateTime.UtcNow;
			TempEntities.RemoveAll(entity => (entity.Life - now).TotalMilliseconds <= 0);

			var options = AppSettings.MinimapOptions;
			if (!options.HasFlag(MinimapOptions.Weapons))
				return;

			switch (entityEventArgs.Event) {
				case ClientGame.EntityEvent.DisruptorMainShot:
					if (lastDisruptorEnd != Vector3.Zero) {
						start = lastDisruptorEnd;
						lastDisruptorEnd = Vector3.Zero;
					} else {
						start = entity.Origin2;
					}
					end = entity.LerpOrigin;
					TempEntities.Add(new EntityData(EntityType.Shot, 1000) {
						Origin = start,
						Origin2 = end,
						Color = Color.OrangeRed
					});
					lastDisruptorEnd = end;
					addImpact(end, Color.OrangeRed);
					break;
				case ClientGame.EntityEvent.DisruptorSniperShot:
					start = entity.Origin2;
					end = entity.LerpOrigin;
					TempEntities.Add(new EntityData(EntityType.Shot, 1000) {
						Origin = start,
						Origin2 = end,
						Color = Color.OrangeRed
					});
					lastDisruptorEnd = end;
					addImpact(end, Color.OrangeRed);
					break;
				case ClientGame.EntityEvent.ConcAltImpact:
					start = entity.Origin2;
					end = entity.Origin2+entity.Angles2*entity.Angles.Length();
					TempEntities.Add(new EntityData(EntityType.Shot, 500) {
						Origin = start,
						Origin2 = end,
						Color = Color.DodgerBlue
					});
					addImpact(end, Color.DodgerBlue);
					break;
				case ClientGame.EntityEvent.PlayEffect:
				case ClientGame.EntityEvent.MissileHit:
				case ClientGame.EntityEvent.MissileMiss:
				case ClientGame.EntityEvent.MissileMissMetal:
					var weapon = ClientGame.GetWeapon(ref entity, out bool altFire);
					var color = weapon.ToColor(altFire);
					addImpact(entity.LerpOrigin, color);
					break;
			}
			void addImpact(Vector3 origin, Color color, int life = 700) {
				TempEntities.Add(new EntityData(EntityType.Impact, life) {
					Origin = origin,
					Color = color
				});
			}
		}

		private void AddToChat(Command command) {
			string fullMessage = command[1];
			int separator = fullMessage.IndexOf(EC + ": ", StringComparison.Ordinal);
			if (separator < 0) {
				return;
			}
			separator += 3;
			string name = fullMessage[..separator];
			string playerName = name.Replace(EC, string.Empty, StringComparison.Ordinal);
			string escapedPlayerName = GetEscapedPlayerName(name);
			if (blockedPlayers.Contains(escapedPlayerName))
				return;
			string message = fullMessage[separator..].Replace(EC, string.Empty, StringComparison.Ordinal);
			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, message, Client?.Version == ClientVersion.JO_v1_02);
			ProcessItemForNotifications(chatItem, command);
			AddItem(chatItem);
		}

		private void AddToLocationChat(Command command) {
			if (command.Length < 4) {
				return;
			}
			string name = command[1];
			string location = command[2];
			string colour = command[3];
			string message = command[4];

			string playerName = name.Replace(EC, string.Empty, StringComparison.Ordinal);
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
			string fullMessage = stringBuilder.ToString().Replace(EC, string.Empty, StringComparison.Ordinal);

			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, fullMessage, Client?.Version == ClientVersion.JO_v1_02);
			ProcessItemForNotifications(chatItem, command);
			AddItem(chatItem);
		}

		private static string GetEscapedPlayerName(string playerName) {
			const string escapeColon = EC + ": ";
			const string escapeStartTeam = EC + "(";
			const string escapeEndTeam = EC + ")";
			const string escapeStartPrivate = EC + "[";
			const string escapeEndPrivate = EC + "]";

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

		private static bool IsPrivateMessage(string message) {
			const string escapePrivate = EC + "]" + EC + ": ";
			return message?.Contains(escapePrivate, StringComparison.Ordinal) ?? false;
		}

		private void AddToPrint(Command command) {
			string text = command[1];
			bool mergeNext = !text.EndsWith('\n');
			if (!mergeNext) {
				text = text.TrimEnd('\n');
			}
			var chatItem = new ChatInfoItemVM(text, Client?.Version == ClientVersion.JO_v1_02, mergeNext);
			ProcessItemForNotifications(chatItem, command);
			AddItem(chatItem);
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

		private void AddItem(ChatItemVM item) {
			while (addingPending);
			bool pending = false;
			lock (pendingItems) {
				lock (Items) {
					pending = DeviceInfo.Platform == DevicePlatform.Android && ViewModel == null;
					if (pending && !mergeInfoItems(pendingItems.LastOrDefault())) {
						pendingItems.Add(item);
					}
				}
			}
			if (!pending) {
				chatQueue.EnqueueOnMainThread(() => {
					lock (Items) {
						if (DeviceInfo.Platform == DevicePlatform.Android) {
							if (!mergeInfoItems(Items.LastOrDefault())) {
								Items.Add(item);
							}
						} else if (DeviceInfo.Platform.IsApple()) {
							if (!mergeInfoItems(Items.FirstOrDefault())) {
								Items.Insert(0, item);
							}
						}
					}
				});
			}
			bool mergeInfoItems(ChatItemVM prevItem) {
				if (item is ChatInfoItemVM thisInfoItem && prevItem is ChatInfoItemVM prevInfoItem
					&& (prevInfoItem.MergeNext || (thisInfoItem.DateTime - prevInfoItem.DateTime) < TimeSpan.FromSeconds(1.337))) {
					if (prevInfoItem.MergeNext)
						prevInfoItem.Text += thisInfoItem.Text;
					else
						prevInfoItem.Text += '\n' + thisInfoItem.Text;
					prevInfoItem.MergeNext = thisInfoItem.MergeNext;
					return true;
				}
				UnreadMessages++;
				return false;
			}
		}

		internal void RemoveItem(ChatItemVM item) {
			lock (Items) {
				int removeIndex = Items.IndexOf(item);
				Items.Remove(item);
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
		}

		private JKDialogConfig pendingDialogConfig;
		private void ShowDialog(JKDialogConfig config, bool offersReconnect = false) {
			if (offersReconnect) {
				if (dialogOffsersReconnect) {
					return;
				} else {
					dialogOffsersReconnect = true;
				}
			}
			if (pendingDialogConfig != null) {
				return;
			}
			if (ViewModel == null) {
				pendingDialogConfig = config;
				return;
			}
			dialogService.Show(config);
			pendingDialogConfig = null;
			dialogOffsersReconnect = false;
		}

		private void ExceptionCallback(JKClientException exception) {
			string message = Helpers.Common.GetExceptionMessage(exception);

			Disconnect();
			ShowDialog(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				CancelText = "Copy",
				CancelAction = (_) => {
					Clipboard.SetTextAsync(message);
					Task.Run(CloseViewModel);
				},
				OkText = "OK",
				OkAction = _ => {
					Task.Run(CloseViewModel);
				}
			});
		}

		private async Task CloseViewModel() {
			if (ViewModel != null) {
				await navigationService.Close(ViewModel);
			}
		}

		private void ShowNotification(string message) {
			if (!AppSettings.NotificationOptions.HasFlag(NotificationOptions.Enabled))
				return;
			if (ViewModel != null && !minimized)
				return;
			string title = ServerInfo.HostName;
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat.Items;

using JKClient;

using MvvmCross;
using MvvmCross.Core;
using MvvmCross.Navigation;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

using Xamarin.Essentials;

namespace JKChat.Core.Models {
	public class GameClient {
		private const int MaxChatMessages = 512;
		private JKClient.JKClient Client;
		private bool showingDialog;
		private readonly IMvxNavigationService navigationService;
		private readonly IDialogService dialogService;
		private readonly IMvxMessenger messenger;
		private readonly IMvxLifetime lifetime;
		private MvxSubscriptionToken playerNameMessageToken;
		private bool minimized = false;
		private readonly LimitedObservableCollection<ChatItemVM> pendingItems;
		private readonly HashSet<string> blockedPlayers;
		private IMvxViewModel viewModel;
		internal IMvxViewModel ViewModel {
			get => viewModel;
			set {
				viewModel = value;
				if (value != null) {
					unreadMessages = 0;
					messenger.Publish(new ServerInfoMessage(this, ServerInfo, Status));
					lock (pendingItems) {
						lock (Items) {
							if (pendingItems.Count > 0) {
								Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {
									if (DeviceInfo.Platform == DevicePlatform.Android) {
										Items.AddRange(pendingItems);
									} else if (DeviceInfo.Platform == DevicePlatform.iOS) {
										Items.InsertRange(0, pendingItems);
									}
									pendingItems.Clear();
								});
							}
						}
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
		internal LimitedObservableCollection<ChatItemVM> Items { get; private set; }
		internal ServerInfo ServerInfo { get; private set; }
		internal ClientInfo[] ClientInfo => Client?.ClientInfo;

		internal GameClient(ServerInfo serverInfo) {
			navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
			dialogService = Mvx.IoCProvider.Resolve<IDialogService>();
			messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			lifetime = Mvx.IoCProvider.Resolve<IMvxLifetime>();
			lifetime.LifetimeChanged += LifetimeChanged;
			playerNameMessageToken = messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
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
				} catch { }
				Client.ServerCommandExecuted += ServerCommandExecuted;
				Client.ServerInfoChanged += ServerInfoChanged;
				Client.Start(ExceptionCallback);
			});
		}

		internal async Task Connect(bool ignoreDialog = true) {
			if (Status != ConnectionStatus.Disconnected || (!ignoreDialog && showingDialog)) {
				return;
			}
			Status = ConnectionStatus.Connecting;
			try {
				await Start();
				if (ServerInfo.NeedPassword/* && string.IsNullOrEmpty(Client.Password)*/) {
					string password = Client.Password;
					bool close = false;
					await ShowDialog(new JKDialogConfig() {
						Title = "Enter Password",
						LeftButton = "Cancel",
						LeftClick = (_) => {
							close = true;
						},
						RightButton = "Connect",
						RightClick = (input) => {
							password = input as string;
						},
						BackgroundClick = (_) => {
							close = true;
						},
						Type = JKDialogType.Title | JKDialogType.Input
					});
					if (close) {
						Disconnect();
						await navigationService.Close(ViewModel);
						return;
					} else {
						Client.Password = password;
					}
				}
				await Client.Connect(ServerInfo);
				if (Client.Status == JKClient.ConnectionStatus.Active) {
					Status = ConnectionStatus.Connected;
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			if (Status != ConnectionStatus.Connected) {
				Status = ConnectionStatus.Disconnected;
			}
		}

		internal void Disconnect(bool showDisconnected = false) {
			if (Client != null && Client.Started) {
				Client.Disconnect();
				Client.Stop();
			}
			Status = ConnectionStatus.Disconnected;
			if (showDisconnected) {
				Task.Run(ShowDisconnected);
			}
		}

		private async Task ShowDisconnected() {
			bool close = false;
			await ShowDialog(new JKDialogConfig() {
				Title = "Disconnected",
				LeftButton = "OK",
				LeftClick = (_) => {
					close = true;
				},
				RightButton = "Reconnect",
				RightClick = (_) => {
					Connect();
				},
				BackgroundClick = (_) => {
					close = true;
				},
				Type = JKDialogType.Title
			});
			if (close && Status == ConnectionStatus.Disconnected) {
				await navigationService.Close(ViewModel);
			}
		}

		internal void Shutdown() {
			Disconnect();
			if (Client != null) {
				Client.ServerCommandExecuted -= ServerCommandExecuted;
				Client.ServerInfoChanged -= ServerInfoChanged;
				Client.Dispose();
			}
			lifetime.LifetimeChanged -= LifetimeChanged;
			if (playerNameMessageToken != null) {
				messenger.Unsubscribe<ServerInfoMessage>(playerNameMessageToken);
				playerNameMessageToken = null;
			}
		}

		internal void ExecuteCommand(string cmd, Encoding encoding = null) {
			Client?.ExecuteCommand(cmd, encoding);
		}

		private void LifetimeChanged(object sender, MvxLifetimeEventArgs ev) {
			switch (ev.LifetimeEvent) {
			case MvxLifetimeEvent.ActivatedFromMemory:
				minimized = false;
				if (ViewModel != null) {
					unreadMessages = 0;
					messenger.Publish(new ServerInfoMessage(this, ServerInfo, Status));
				}
				break;
			case MvxLifetimeEvent.Deactivated:
				minimized = true;
				break;
			}
		}

		private async void ServerCommandExecuted(CommandEventArgs commandEventArgs) {
			var command = commandEventArgs.Command;
			string cmd = command.Argv(0);
			if (string.Compare(cmd, "chat", StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(cmd, "tchat", StringComparison.OrdinalIgnoreCase) == 0) {
				AddToChat(command, commandEventArgs.UTF8Command);
			} else if (string.Compare(cmd, "lchat", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(cmd, "ltchat", StringComparison.OrdinalIgnoreCase) == 0) {
				AddToLocationChat(command, commandEventArgs.UTF8Command);
			} else if (string.Compare(cmd, "print", StringComparison.OrdinalIgnoreCase) == 0) {
				string title;
				if (string.Compare(command.Argv(1), 0, "@@@INVALID_ESCAPE_TO_MAIN", 0, 25, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command.Argv(1), 0, "Invalid password", 0, 16, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Invalid Password";
				} else if (string.Compare(command.Argv(1), 0, "@@@SERVER_IS_FULL", 0, 17, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command.Argv(1), 0, "Server is full.", 0, 15, StringComparison.OrdinalIgnoreCase) == 0) {
					title = "Server is Full";
				} else {
					AddToPrint(command);
					return;
				}
				Disconnect();
				bool close = false;
				await ShowDialog(new JKDialogConfig() {
					Title = title,
					RightButton = "OK",
					AnyClick = (_) => {
						close = true;
					},
					Type = JKDialogType.Title
				});
				if (close) {
					await navigationService.Close(ViewModel);
				}
			} else if (string.Compare(cmd, "disconnect", StringComparison.OrdinalIgnoreCase) == 0) {
				string reason;
				if (string.Compare(command.Argv(1), 0, "@@@WAS_KICKED", 0, 13, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command.Argv(1), 0, "was kicked", 0, 10, StringComparison.OrdinalIgnoreCase) == 0) {
					reason = "You were kicked";
				} else if (string.Compare(command.Argv(1), 0, "@@@DISCONNECTED", 0, 15, StringComparison.OrdinalIgnoreCase) == 0
					|| string.Compare(command.Argv(1), 0, "disconnected", 0, 12, StringComparison.OrdinalIgnoreCase) == 0) {
					reason = "You disconnected";
				} else {
					reason = command.Argv(1);
				}
				JKDialogType type = JKDialogType.Title;
				if (!string.IsNullOrEmpty(reason)) {
					type |= JKDialogType.Message;
				}
				Disconnect();
				bool close = false;
				await ShowDialog(new JKDialogConfig() {
					Title = "Disconnected",
					Message = reason,
					LeftButton = "OK",
					LeftClick = (_) => {
						close = true;
					},
					RightButton = "Reconnect",
					RightClick = (_) => {
						Connect();
					},
					BackgroundClick = (_) => {
						close = true;
					},
					Type = type
				});
				if (close) {
					await navigationService.Close(ViewModel);
				}
			}
		}

		private void AddToChat(Command command, Command utf8Command) {
			string fullMessage = command.Argv(1);
			string utf8FullMessage = utf8Command?.Argv(1) ?? fullMessage;
			int separator = fullMessage.IndexOf(Common.EscapeCharacter + ": ");
			if (separator < 0) {
				return;
			}
			separator += 3;
			string name = fullMessage.Substring(0, separator);
			string playerName = name.Replace(Common.EscapeCharacter, string.Empty);
			string escapedPlayerName = GetEscapedPlayerName(name);
			string message = utf8FullMessage.Substring(separator, utf8FullMessage.Length-separator).Replace(Common.EscapeCharacter, string.Empty);
			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, message, Client?.Version == ClientVersion.JO_v1_02);
			AddItem(chatItem);
		}

		private void AddToLocationChat(Command command, Command utf8Command) {
			if (command.Argc < 4) {
				return;
			}
			string name = command.Argv(1);
			string location = command.Argv(2);
			string colour = command.Argv(3);
			string message = utf8Command.Argv(4);

			string playerName = name.Replace(Common.EscapeCharacter, string.Empty);
			string escapedPlayerName = GetEscapedPlayerName(name);

			var stringBuilder = new StringBuilder();
			stringBuilder
				.Append("^7<")
				.Append(location)
				.Append("> ^")
				.Append(colour)
				.Append(message)
				.Replace(Common.EscapeCharacter, string.Empty);
			string fullMessage = stringBuilder.ToString();

			var chatItem = new ChatMessageItemVM(escapedPlayerName, playerName, fullMessage, Client?.Version == ClientVersion.JO_v1_02);
			AddItem(chatItem);
		}

		private static string GetEscapedPlayerName(string playerName) {
			const string escapeColon = Common.EscapeCharacter + ": ";
			const string escapeStartTeam = Common.EscapeCharacter + "(";
			const string escapeEndTeam = Common.EscapeCharacter + ")";
			const string escapeStartPrivate = Common.EscapeCharacter + "[";
			const string escapeEndPrivate = Common.EscapeCharacter + "]";

			int startIndex = playerName.IndexOf(escapeStartTeam);
			if (startIndex < 0) startIndex = playerName.IndexOf(escapeStartPrivate);
			if (startIndex < 0) startIndex = -2;
			startIndex += 2;

			int endIndex = playerName.IndexOf(escapeEndTeam);
			if (endIndex < 0) endIndex = playerName.IndexOf(escapeEndPrivate);
			if (endIndex < 0) endIndex = playerName.IndexOf(escapeColon);
			if (endIndex < 0) return playerName;

			return playerName.Substring(startIndex, endIndex-startIndex);
		}

		private void AddToPrint(Command command) {
			string text = command.Argv(1).TrimEnd('\n');
			var chatItem = new ChatInfoItemVM(text, Client?.Version == ClientVersion.JO_v1_02);
			AddItem(chatItem);
		}

		private void AddItem(ChatItemVM item) {
			if (item is ChatMessageItemVM messageItem && blockedPlayers.Contains(messageItem.EscapedPlayerName)) {
				return;
			}
			lock (pendingItems) {
				lock (Items) {
					bool pending = DeviceInfo.Platform == DevicePlatform.Android && ViewModel == null;
					var items = pending ? pendingItems : Items;
					if (items.Count > 0) {
						ChatItemVM prevItem = null;
						if (DeviceInfo.Platform == DevicePlatform.Android) {
							prevItem = items[items.Count - 1];
						} else if (DeviceInfo.Platform == DevicePlatform.iOS) {
							prevItem = items[0];
						}
						prevItem.BottomVMType = item.ThisVMType;
						item.TopVMType = prevItem.ThisVMType;
					}
					if (pending) {
						pendingItems.Add(item);
					} else {
						Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {
							if (DeviceInfo.Platform == DevicePlatform.Android) {
								Items.Add(item);
							} else if (DeviceInfo.Platform == DevicePlatform.iOS) {
								Items.Insert(0, item);
							}
						});
					}
				}
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
					} else if (DeviceInfo.Platform == DevicePlatform.iOS) {
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
					if (DeviceInfo.Platform == DevicePlatform.Android) {
						Items.AddRange(pendingItems);
						pendingItems.ReplaceWith(Items);
						Items.Clear();
					}
				}
			}
		}

		private async Task ShowDialog(JKDialogConfig config) {
			if (!showingDialog) {
				showingDialog = true;
				while (ViewModel == null);
				if (dialogService.Showing) {
					while (dialogService.Showing);
					//delay only if pending any
					await Task.Delay(500);
				}
				await dialogService.ShowAsync(config);
				showingDialog = false;
			}
		}

		private async Task ExceptionCallback(JKClientException exception) {
			string message = Helpers.Common.GetExceptionMessage(exception);

			Disconnect();
			bool close = false;
			await ShowDialog(new JKDialogConfig() {
				Title = "Error",
				Message = message,
				LeftButton = "Copy",
				LeftClick = (_) => {
					Xamarin.Essentials.Clipboard.SetTextAsync(message);
				},
				RightButton = "OK",
				AnyClick = (_) => {
					close = true;
				},
				Type = JKDialogType.Title | JKDialogType.Message
			});
			if (close) {
				Disconnect();
				await navigationService.Close(ViewModel);
			}
		}
	}
}

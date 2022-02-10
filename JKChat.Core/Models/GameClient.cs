using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly JKClient.JKClient Client;
		private bool showingDialog;
		private readonly IMvxNavigationService navigationService;
		private readonly IDialogService dialogService;
		private readonly IMvxMessenger messenger;
		private readonly IMvxLifetime lifetime;
		private bool minimized = false;
		private readonly LimitedObservableCollection<ChatItemVM> pendingItems;
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
		internal ClientInfo []ClientInfo => Client.ClientInfo;

		internal GameClient(ServerInfo serverInfo) {
			navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();
			dialogService = Mvx.IoCProvider.Resolve<IDialogService>();
			messenger = Mvx.IoCProvider.Resolve<IMvxMessenger>();
			lifetime = Mvx.IoCProvider.Resolve<IMvxLifetime>();
			lifetime.LifetimeChanged += LifetimeChanged;
			pendingItems = new LimitedObservableCollection<ChatItemVM>(MaxChatMessages);
			ServerInfo = serverInfo;
			Client = new JKClient.JKClient(JKClient.JKClient.GetKnownClientHandler(serverInfo)) {
				Name = Settings.PlayerName,
				Guid = Settings.PlayerId
			};
			Client.ServerCommandExecuted += ServerCommandExecuted;
			Client.ServerInfoChanged += ServerInfoChanged;
			Items = new LimitedObservableCollection<ChatItemVM>(MaxChatMessages);
			Status = ConnectionStatus.Disconnected;
		}

		private void ServerInfoChanged(ServerInfo serverInfo) {
			ServerInfo = serverInfo;
			messenger.Publish(new ServerInfoMessage(this, serverInfo, Status));
		}

		internal async Task Connect(bool ignoreDialog = true) {
			if (!Client.Started) {
				Client.Start(ExceptionCallback);
			}
			if (Status != ConnectionStatus.Disconnected || (!ignoreDialog && showingDialog)) {
				return;
			}
			Status = ConnectionStatus.Connecting;
			try {
				if (ServerInfo.NeedPassword/* && string.IsNullOrEmpty(Client.Password)*/) {
					string password = string.Empty;
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
			if (Client.Started) {
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
			Client.ServerCommandExecuted -= ServerCommandExecuted;
			Client.ServerInfoChanged -= ServerInfoChanged;
			Client.Dispose();
			lifetime.LifetimeChanged -= LifetimeChanged;
		}

		internal void ExecuteCommand(string cmd, Encoding encoding = null) {
			Client.ExecuteCommand(cmd, encoding);
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
			int separator = fullMessage.IndexOf("\u0019: ");
			if (separator < 0) {
				return;
			}
			separator += 3;
			string playerName = fullMessage.Substring(0, separator).Replace("\u0019", string.Empty);
			string message = utf8FullMessage.Substring(separator, utf8FullMessage.Length-separator).Replace("\u0019", string.Empty);
			var chatItem = new ChatMessageItemVM(playerName, message);
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

			string playerName = name.Replace("\u0019", string.Empty);

			var stringBuilder = new StringBuilder();
			stringBuilder
				.Append("^7<")
				.Append(location)
				.Append("> ^")
				.Append(colour)
				.Append(message)
				.Replace("\u0019", string.Empty);
			string fullMessage = stringBuilder.ToString();

			var chatItem = new ChatMessageItemVM(playerName, fullMessage);
			AddItem(chatItem);
		}

		private void AddToPrint(Command command) {
			string text = command.Argv(1).TrimEnd('\n');
			var chatItem = new ChatInfoItemVM(text);
			AddItem(chatItem);
		}

		private void AddItem(ChatItemVM item) {
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
			Exception realException;
			if (exception.InnerException is AggregateException aggregateException) {
				realException = aggregateException.InnerExceptions != null ? aggregateException.InnerExceptions[0] : aggregateException;
			} else if (exception.InnerException != null) {
				realException = exception.InnerException;
			} else {
				realException = exception;
			}
			string message = realException.Message + (!string.IsNullOrEmpty(realException.StackTrace) ? ("\n\n" + realException.StackTrace) : string.Empty);

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

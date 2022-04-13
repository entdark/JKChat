using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.Core.ViewModels.ServerList.Items;
using JKChat.Core.ViewModels.Settings;

using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;

using Xamarin.Essentials;

namespace JKChat.Core.ViewModels.Chat {
	public class ChatViewModel : ReportViewModel<ChatItemVM, ServerListItemVM> {
		private GameClient gameClient;
		private MvxSubscriptionToken serverInfoMessageToken;

		public IMvxCommand ItemClickCommand { get; private set; }
		public IMvxCommand CopyCommand { get; private set; }
		public IMvxCommand SendMessageCommand { get; private set; }
		public IMvxCommand ChatTypeCommand { get; private set; }
		public IMvxCommand CommonChatTypeCommand { get; private set; }
		public IMvxCommand TeamChatTypeCommand { get; private set; }
		public IMvxCommand PrivateChatTypeCommand { get; private set; }

		protected override string ReportTitle => "Report message";
		protected override string ReportMessage => "Do you want to report this message?";
		protected override string ReportedTitle => "Message reported";
		protected override string ReportedMessage => "Thank you for reporting this message";

		private ConnectionStatus status;
		public ConnectionStatus Status {
			get => status;
			set {
				if (SetProperty(ref status, value)) {
					SendMessageCommand.RaiseCanExecuteChanged();
					IsLoading = value == ConnectionStatus.Connecting;
				}
			}
		}

		private MvxObservableCollection<ChatItemVM> items;
		public override MvxObservableCollection<ChatItemVM> Items {
			get => items;
			set => SetProperty(ref items, value);
		}

		private string message;
		public string Message {
			get => message;
			set {
				if (SetProperty(ref message, value)) {
					SendMessageCommand.RaiseCanExecuteChanged();
				}
			}
		}

		private ChatType chatType;
		public ChatType ChatType {
			get => chatType;
			set => SetProperty(ref chatType, value);
		}

		private bool selectingChatType;
		public bool SelectingChatType {
			get => selectingChatType;
			set => SetProperty(ref selectingChatType, value);
		}

		internal JKClient.ServerInfo ServerInfo => gameClient?.ServerInfo;

		public ChatViewModel() {
			ItemClickCommand = new MvxAsyncCommand<ChatItemVM>(ItemClickExecute);
			CopyCommand = new MvxAsyncCommand<ChatItemVM>(CopyExecute);
			SendMessageCommand = new MvxAsyncCommand(SendMessageExecute, SendMessageCanExecute);
			ChatTypeCommand = new MvxCommand(ChatTypeExecute);
			CommonChatTypeCommand = new MvxCommand(CommonChatTypeExecute);
			TeamChatTypeCommand = new MvxCommand(TeamChatTypeExecute);
			PrivateChatTypeCommand = new MvxCommand(PrivateChatTypeExecute);

			Items = new MvxObservableCollection<ChatItemVM>();
			ChatType = ChatType.Common;
			SelectingChatType = false;
			serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			if (gameClient?.ServerInfo.Address == message.ServerInfo.Address) {
				Status = message.Status;
				Title = message.ServerInfo.HostName;
/*				if (gameClient.ViewModel == null && Status == ConnectionStatus.Disconnected) {
					Task.Run(ShowDisconnected);
				}*/
			}
		}

		private async Task ItemClickExecute(ChatItemVM item) {
			string text;
			if (item is ChatMessageItemVM messageItem) {
				text = messageItem.Message;
			} else if (item is ChatInfoItemVM infoItem) {
				text = infoItem.Text;
			} else {
				return;
			}
			var uriAttributes = new List<AttributeData<Uri>>();
			text.CleanString(uriAttributes: uriAttributes);
			Uri uri;
			if (uriAttributes.Count > 1) {
				var dialogList = new DialogListViewModel();
				for (int i = 0; i < uriAttributes.Count; i++) {
					dialogList.Items.Add(new DialogItemVM() {
						Id = i,
						Name = uriAttributes[i].Value.ToString()
					});
				}
				int id = -1;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Select Link",
					LeftButton = "Cancel",
					RightButton = "OK",
					RightClick = (input) => {
						if (input is DialogItemVM dialogItem) {
							id = dialogItem.Id;
						}
					},
					ListViewModel = dialogList,
					Type = JKDialogType.Title | JKDialogType.List
				});
				if (id == -1) {
					return;
				}
				uri = uriAttributes[id].Value;
			} else if (uriAttributes.Count <= 0) {
				return;
			} else {
				uri = uriAttributes[0].Value;
			}
//			foreach (var uriAttribute in uriAttributes) {
//				uri = uriAttribute.Value;
				try {
					if (string.Compare(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) != 0
					|| string.Compare(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) != 0
					|| string.Compare(uri.Scheme, "ftp", StringComparison.OrdinalIgnoreCase) != 0) {
						throw new Exception();
					}
					await Browser.OpenAsync(uri, BrowserLaunchMode.External);
				} catch (Exception exception) {
					Debug.WriteLine(exception);
					await Launcher.TryOpenAsync(uri);
				}
//			}
		}

		protected override async Task<bool> ReportExecute(ChatItemVM item) {
			bool report = await base.ReportExecute(item);
//			if (report) {
//				gameClient.RemoveItem(item);
//			}
			if (report && item is ChatMessageItemVM) {
				bool block = false;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Block user",
					Message = "Would you like to block the user and hide all their messages?",
					LeftButton = "No",
					RightButton = "Yes",
					RightClick = (_) => {
						block = true;
					},
					Type = JKDialogType.Title | JKDialogType.Message
				});
				if (block) {
					gameClient.HideAllMessages(item);
				}
			}
			return report;
		}

		protected override void SelectExecute(ChatItemVM item) {
			base.SelectExecute(item);
			if (GetSelectedItem() == null) {
				Title = gameClient?.ServerInfo?.HostName;
			}
		}

		private async Task CopyExecute(ChatItemVM item) {
			string text;
			if (item is ChatMessageItemVM messageItem) {
				text = messageItem.Message;
			} else if (item is ChatInfoItemVM infoItem) {
				text = infoItem.Text;
			} else {
				return;
			}
			if (string.IsNullOrEmpty(text)) {
				return;
			}
			await Clipboard.SetTextAsync(text.CleanString());
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Message is Copied",
//				Message = "To copy message with color codes click \"With Colors\"",
				LeftButton = "With Colors",
				LeftClick = (_) => {
					Clipboard.SetTextAsync(text);
				},
				RightButton = "OK",
				Type = JKDialogType.Title// | JKDialogType.Message
			});
		}

		private async Task SendMessageExecute() {
			if (Message.StartsWith("/")) {
				gameClient.ExecuteCommand(Message.Substring(1), Encoding.UTF8);
				Message = string.Empty;
				Messenger.Publish(new SentMessageMessage(this));
				return;
			}
			string command;
			switch (ChatType) {
			default:
			case ChatType.Common:
				command = $"say \"{Message}\"\n";
				break;
			case ChatType.Team:
				command = $"say_team \"{Message}\"\n";
				break;
			case ChatType.Private:
				var dialogList = new DialogListViewModel();
				if (gameClient.ClientInfo != null) {
					dialogList.Items.AddRange(gameClient.ClientInfo.Where(ci => ci.InfoValid).Select(SetupItem));
				}
				int id = -1;
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Private Message",
					LeftButton = "Cancel",
					RightButton = "OK",
					RightClick = (input) => {
						if (input is DialogItemVM dialogItem) {
							id = dialogItem.Id;
						}
					},
					ListViewModel = dialogList,
					Type = JKDialogType.Title | JKDialogType.List
				});
				if (id == -1) {
					return;
				}
				command = $"tell {id} \"{Message}\"\n";
				break;
			}
			gameClient.ExecuteCommand(command, Encoding.UTF8);
			Message = string.Empty;
			Messenger.Publish(new SentMessageMessage(this));
		}

		private bool SendMessageCanExecute() {
			return !string.IsNullOrEmpty(Message) && Status == ConnectionStatus.Connected;
		}

		private DialogItemVM SetupItem(JKClient.ClientInfo clientInfo) {
			return new DialogItemVM() {
				Id = clientInfo.ClientNum,
				Name = clientInfo.Name
			};
		}

		private void ChatTypeExecute() {
			SelectingChatType = !SelectingChatType;
		}

		private void CommonChatTypeExecute() {
			ChatType = ChatType.Common;
			SelectingChatType = false;
		}

		private void TeamChatTypeExecute() {
			ChatType = ChatType.Team;
			SelectingChatType = false;
		}

		private void PrivateChatTypeExecute() {
			ChatType = ChatType.Private;
			SelectingChatType = false;
		}

		public async Task<bool> OfferDisconnect() {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Disconnect",
				Message = "Disconnect from this server?",
				LeftButton = "No",
				RightButton = "Yes",
				RightClick = (input) => {
					gameClient.Disconnect();
				},
				Type = JKDialogType.Title | JKDialogType.Message,
				ImmediateResult = false
			}, () => {
				NavigationService.Close(this);
			});
			return true;
		}

		public override void Prepare(ServerListItemVM parameter) {
			Prepare(parameter.ServerInfo);
		}

		public override Task Initialize() {
			return Connect();
		}

		public override void ViewCreated() {
			base.ViewCreated();
			if (serverInfoMessageToken == null) {
				serverInfoMessageToken = Messenger.Subscribe<ServerInfoMessage>(OnServerInfoMessage);
			}
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (serverInfoMessageToken != null) {
					Messenger.Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
					serverInfoMessageToken = null;
				}
				gameClient.MakeAllPending();
			}
			base.ViewDestroy(viewFinishing);
		}

		public override void ViewAppeared() {
			base.ViewAppeared();
			if (Items.Count <= 0) {
				Task.Run(async () => {
					await Task.Delay(200);
					gameClient.ViewModel = this;
				});
			} else {
				gameClient.ViewModel = this;
			}
		}

		public override void ViewDisappearing() {
			gameClient.ViewModel = null;
			base.ViewDisappearing();
		}

		protected override void SaveStateToBundle(IMvxBundle bundle) {
			base.SaveStateToBundle(bundle);
			//TODO or NOTTODO: serialize into JSON
			bundle.Data["Address"] = gameClient.ServerInfo.Address.ToString();
			bundle.Data["Protocol"] = gameClient.ServerInfo.Protocol.ToString();
			bundle.Data["NeedPassword"] = gameClient.ServerInfo.NeedPassword.ToString();
			bundle.Data["HostName"] = gameClient.ServerInfo.HostName;
		}

		protected override void ReloadFromBundle(IMvxBundle state) {
			base.ReloadFromBundle(state);
			var serverInfo = new JKClient.ServerInfo();
			if (state.Data.TryGetValue("Address", out string address) && !string.IsNullOrEmpty(address))
				serverInfo.Address = JKClient.NetAddress.FromString(address);
			if (state.Data.TryGetValue("Protocol", out string protocolStr) && Enum.TryParse(protocolStr, out JKClient.ProtocolVersion protocol))
				serverInfo.Protocol = protocol;
			if (state.Data.TryGetValue("NeedPassword", out string needPasswordStr) && bool.TryParse(needPasswordStr, out bool needPassword))
				serverInfo.NeedPassword = needPassword;
			if (state.Data.TryGetValue("HostName", out string hostName) && !string.IsNullOrEmpty(hostName))
				serverInfo.HostName = hostName;
			Prepare(serverInfo);
		}

		private void Prepare(JKClient.ServerInfo serverInfo) {
			gameClient = Mvx.IoCProvider.Resolve<IGameClientsService>().GetOrStartClient(serverInfo);
			//gameClient.ViewModel = this;
			Items = gameClient.Items;
			Status = gameClient.Status;
			Title = gameClient.ServerInfo.HostName;
		}

		private async Task Connect() {
			gameClient.Connect(false);
		}
	}
}

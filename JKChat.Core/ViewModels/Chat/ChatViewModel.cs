using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;
using JKChat.Core.ViewModels.ServerList.Items;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

[assembly: MvxNavigation(typeof(ChatViewModel), @"jkchat://chat\?address=(?<address>.*)")]
namespace JKChat.Core.ViewModels.Chat {
	public class ChatViewModel : ReportViewModel<ChatItemVM, ServerInfoParameter>, IFromRootNavigatingViewModel {
		private static readonly string []commonCommands = new []{ "/rcon", "/callvote" };
		private static readonly string []baseEnhancedCommands = new []{ "/whois", "/rules", "/mappool", "/ctfstats", "/toptimes", "/topaim", "/pugstats" };
		private static readonly string []jaPlusCommands = new []{ "/aminfo", "/amsay" };
		private readonly HashSet<string> commands = commonCommands.ToHashSet();
		
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;

		private string address;
		private GameClient gameClient;

		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand CopyCommand { get; init; }
		public IMvxCommand SendMessageCommand { get; init; }
		public IMvxCommand StartCommandCommand { get; init; }
		public IMvxCommand ChatTypeCommand { get; init; }
		public IMvxCommand CommonChatTypeCommand { get; init; }
		public IMvxCommand TeamChatTypeCommand { get; init; }
		public IMvxCommand PrivateChatTypeCommand { get; init; }
		public IMvxCommand CommandItemClickCommand { get; init; }
		public IMvxCommand ServerInfoCommand { get; init; }
		public IMvxCommand FavouriteCommand { get; init; }
		public IMvxCommand DisconnectCommand { get; init; }
		public IMvxCommand ShareCommand { get; init; }
		public IMvxCommand ServerReportCommand { get; init; }

		protected override string ReportTitle => "Report message";
		protected override string ReportMessage => "Do you want to report this message?";
		protected override string ReportedTitle => "Message reported";
		protected override string ReportedMessage => "Thank you for reporting this message";

		private ConnectionStatus status;
		public ConnectionStatus Status {
			get => status;
			set => SetProperty(ref status, value, () => {
				IsLoading = value == ConnectionStatus.Connecting;
				SendMessageCommand.RaiseCanExecuteChanged();
				StartCommandCommand.RaiseCanExecuteChanged();
			});
		}

		private string message;
		public string Message {
			get => message;
			set => SetProperty(ref message, value, () => {
				SendMessageCommand.RaiseCanExecuteChanged();
				StartCommandCommand.RaiseCanExecuteChanged();
				int oldCount = CommandItems.Count;
				if (message?.StartsWith('/') ?? false) {
					var matchingCommands = commands.Where(c => c.Contains(message) && string.Compare(c, message, StringComparison.OrdinalIgnoreCase) != 0);
					CommandItems.ReplaceWith(matchingCommands);
				} else if (CommandItems.Count > 0) {
					CommandItems.Clear();
				}
				if (CommandItems.Count != oldCount) {
					RaisePropertyChanged(nameof(CommandItems) + "." + nameof(CommandItems.Count));
				}
			});
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

		private MvxObservableCollection<string> commandItems;
		public MvxObservableCollection<string> CommandItems {
			get => commandItems;
			set => SetProperty(ref commandItems, value);
		}

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set => SetProperty(ref isFavourite, value);
		}

		private bool commandSetAutomatically;
		public bool CommandSetAutomatically {
			get => commandSetAutomatically;
			set => SetProperty(ref commandSetAutomatically, value);
		}

		internal JKClient.ServerInfo ServerInfo => gameClient?.ServerInfo;

		public ChatViewModel(ICacheService cacheService, IGameClientsService gameClientsService) {
			this.cacheService = cacheService;
			this.gameClientsService = gameClientsService;
			ItemClickCommand = new MvxAsyncCommand<ChatItemVM>(ItemClickExecute);
			CopyCommand = new MvxAsyncCommand<ChatItemVM>(CopyExecute);
			SendMessageCommand = new MvxAsyncCommand(SendMessageExecute, SendMessageCanExecute);
			StartCommandCommand = new MvxCommand(StartCommandExecute, StartCommandCanExecute);
			ChatTypeCommand = new MvxCommand(ChatTypeExecute);
			CommonChatTypeCommand = new MvxCommand(CommonChatTypeExecute);
			TeamChatTypeCommand = new MvxCommand(TeamChatTypeExecute);
			PrivateChatTypeCommand = new MvxCommand(PrivateChatTypeExecute);
			CommandItemClickCommand = new MvxCommand<string>(CommandItemClickExecute);
			ServerInfoCommand = new MvxAsyncCommand(ServerInfoExecute);
			FavouriteCommand = new MvxCommand(FavouriteExecute);
			DisconnectCommand = new MvxAsyncCommand(DisconnectExecute);
			ShareCommand = new MvxAsyncCommand(ShareExecute);
			ServerReportCommand = new MvxAsyncCommand(ReportServerExecute);

			ChatType = ChatType.Common;
			SelectingChatType = false;
			CommandItems = new();
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			if (gameClient?.ServerInfo == message.ServerInfo) {
				AddCommands();
				Status = message.Status;
				Title = message.ServerInfo.HostName;
/*				if (gameClient.ViewModel == null && Status == ConnectionStatus.Disconnected) {
					Task.Run(ShowDisconnected);
				}*/
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			if (gameClient?.ServerInfo == message.ServerInfo) {
				IsFavourite = message.IsFavourite;
			}
		}

		private void AddCommands() {
			var mod = gameClient.Modification;
			switch (mod) {
			case JKClient.GameModification.BaseEnhanced:
			case JKClient.GameModification.BaseEntranced:
				commands.UnionWith(baseEnhancedCommands);
				break;
			case JKClient.GameModification.JAPlus:
				commands.UnionWith(jaPlusCommands);
				break;
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
			Uri uri = null;
			if (uriAttributes.Count > 1) {
				var dialogList = new DialogListViewModel(uriAttributes.Select(ua => new DialogItemVM() {
					Name = ua.Value.ToString()
				}), DialogSelectionType.InstantSelection);
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Select Link",
					CancelText = "Cancel",
					OkAction = config => {
						if (config?.List?.SelectedIndex is int id && id >= 0) {
							uri = uriAttributes[id].Value;
							Task.Run(openUri);
						}
					},
					List = dialogList
				});
			} else if (uriAttributes.Count <= 0) {
				return;
			} else {
				uri = uriAttributes[0].Value;
				await openUri();
			}
			async Task openUri() {
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
			}
		}

		protected override async Task ReportExecute(ChatItemVM item, Action<bool> reported = null) {
			await base.ReportExecute(item, report => {
//				if (report) {
//					gameClient.RemoveItem(item);
//				}
				if (report && item is ChatMessageItemVM messageItem) {
					Task.Run(async () => await reportExecuteBlock(messageItem));
				}
			});
			async Task reportExecuteBlock(ChatMessageItemVM item) {
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Block User",
					Message = "Would you like to block the user and hide all their messages?",
					CancelText = "No",
					OkText = "Yes",
					OkAction = _ => {
						gameClient.HideAllMessages(item);
					}
				});
			}
		}

		protected override void SelectExecute(ChatItemVM item) {
			base.SelectExecute(item);
			if (SelectedItem == null) {
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
				CancelText = "With Colors",
				CancelAction = _ => {
					Clipboard.SetTextAsync(text);
				},
				OkText = "OK"
			});
		}

		private async Task SendMessageExecute() {
			if (Message.StartsWith("/")) {
				if (Message.Length > 1) {
					gameClient.ExecuteCommand(Message[1..], true);
				}
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
				var dialogList = new DialogListViewModel(gameClient.ClientInfo?.Where(ci => ci.InfoValid).Select(ci => new DialogItemVM() {
					Id = ci.ClientNum,
					Name = ci.Name
				}), DialogSelectionType.InstantSelection);
				await DialogService.ShowAsync(new JKDialogConfig() {
					Title = "Private Message",
					CancelText = "Cancel",
					OkAction = config => {
						if (config?.List?.SelectedItem?.Id is int id && id >= 0) {
							command = $"tell {id} \"{Message}\"\n";
							executeCommand();
						}
					},
					List = dialogList
				});
				return;
			}
			executeCommand();
			void executeCommand() {
				gameClient.ExecuteCommand(command);
				Message = string.Empty;
				Messenger.Publish(new SentMessageMessage(this));
			}
		}

		private bool SendMessageCanExecute() {
			return !string.IsNullOrEmpty(Message) && Status == ConnectionStatus.Connected;
		}

		private void StartCommandExecute() {
			CommandSetAutomatically = true;
			Message = "/";
		}

		private bool StartCommandCanExecute() {
			return string.IsNullOrEmpty(Message) && Status == ConnectionStatus.Connected;
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

		private void CommandItemClickExecute(string command) {
			CommandSetAutomatically = true;
			Message = command;
		}

		private async Task ServerInfoExecute() {
			await NavigationService.Navigate<ServerInfoViewModel, ServerInfoParameter>(new(ServerInfo) { IsFavourite = IsFavourite, Status = Status, LoadInfo = false });
		}

		private async Task DisconnectExecute() {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Disconnect",
				Message = "Disconnect from this server?",
				CancelText = "No",
				OkText = "Yes",
				OkAction = _ => {
					gameClient?.Disconnect();
					NavigationService.Close(this);
				}
			});
		}

		private void FavouriteExecute() {
			Messenger.Publish(new FavouriteMessage(this, ServerInfo, !IsFavourite));
		}

		private async Task ShareExecute() {
			if (ServerInfo == null)
				return;
			await Share.RequestAsync($"{ColourTextHelper.CleanString(ServerInfo.HostName)}\n/connect {ServerInfo.Address}", $"Connect to {ServerInfo.Version.ToDisplayString()} server");
		}

		private async Task ReportServerExecute() {
			//TODO: copy paste from ServerListViewModel
		}

		public override void Prepare(ServerInfoParameter parameter) {
			Prepare(parameter.ServerInfo, parameter.IsFavourite);
		}

		public void Init(string address) {
			if (string.IsNullOrEmpty(this.address))
				this.address = address;
		}

		protected override void SaveStateToBundle(IMvxBundle bundle) {
			base.SaveStateToBundle(bundle);
			bundle.Data[nameof(address)] = ServerInfo?.Address?.ToString() ?? string.Empty;
		}

		protected override void ReloadFromBundle(IMvxBundle state) {
			base.ReloadFromBundle(state);
			if (state.Data.TryGetValue(nameof(this.address), out string address))
				this.address = address;
		}

		protected override Task BackgroundInitialize() {
			return Connect();
		}

		public override void ViewCreated() {
			base.ViewCreated();
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				gameClient?.MakeAllPending();
			}
			base.ViewDestroy(viewFinishing);
		}

		public override void ViewAppeared() {
			base.ViewAppeared();
			if (gameClient == null) {
				return;
			}
			if (gameClient.ViewModel != null) {
				return;
			}
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
			if (gameClient != null) {
				gameClient.ViewModel = null;
			}
			base.ViewDisappearing();
		}

		private void Prepare(JKClient.ServerInfo serverInfo, bool isFavourite) {
			gameClient = gameClientsService.GetOrStartClient(serverInfo);
			Items = gameClient.Items;
			Status = gameClient.Status;
			Title = gameClient.ServerInfo.HostName;
			IsFavourite = isFavourite;
			AddCommands();
		}

		private async Task Connect() {
			if (gameClient == null) {
				if (!string.IsNullOrEmpty(this.address)) {
					IsLoading = true;
					var server = await ServerListItemVM.FindExistingOrLoad(this.address);
					if (server == null) {
						IsLoading = false;
						await DialogService.ShowAsync(new JKDialogConfig() {
							Title = "Failed to Connect",
							Message = $"There is no server with address \"{address}\"",
							OkText = "OK",
							OkAction = _ => {
								Task.Run(close);
							}
						});
						return;
					}
					Prepare(server.ServerInfo, server.IsFavourite);
				} else {
					await DialogService.ShowAsync(new JKDialogConfig() {
						Title = "Failed to Connect",
						Message = $"Server address is empty",
						OkText = "OK",
						OkAction = _ => {
							Task.Run(close);
						}
					});
					return;
				}
			}
			//should never happen
			if (gameClient == null)
				return;
			await cacheService.SaveRecentServer(ServerInfo);
			await gameClient.Connect(false);

			async Task close() {
				await NavigationService.Close(this);
			}
		}

		public bool ShouldLetOtherNavigateFromRoot(object data) {
			if (data is JKClient.ServerInfo serverInfo)
				return this.ServerInfo != serverInfo;
			else if (data is string s && JKClient.NetAddress.FromString(s) is { } address)
				return this.ServerInfo.Address != address;
			return true;
		}
	}
}

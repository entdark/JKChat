using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

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

using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

using JAClientGame = JKClient.JAClientGame;

[assembly: MvxNavigation(typeof(ChatViewModel), @"jkchat://chat\?address=(?<address>.*)")]
namespace JKChat.Core.ViewModels.Chat {
	public class ChatViewModel : ReportViewModel<ChatItemVM, ServerInfoParameter>, IFromRootNavigatingViewModel {
		private static readonly string []commonCommands = new []{ "/rcon", "/callvote" };
		private static readonly string []baseEnhancedCommands = new []{ "/whois", "/rules", "/mappool", "/ctfstats", "/toptimes", "/topaim", "/pugstats" };
		private static readonly string []jaPlusCommands = new []{ "/aminfo", "/amsay" };
		private static readonly string []mapHelperCommands = new []{ "/follownext", "/followprev", "/follow", "/team spectator" };
		private readonly HashSet<string> commands = commonCommands.ToHashSet();
		
		private readonly ICacheService cacheService;
		private readonly IGameClientsService gameClientsService;

		private string address;
		private GameClient gameClient;
		private string mapName;

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

		private string players;
		public string Players {
			get => players;
			set => SetProperty(ref players, value);
		}

		private string message;
		public string Message {
			get => message;
			set => SetProperty(ref message, value, () => {
				SendMessageCommand.RaiseCanExecuteChanged();
				StartCommandCommand.RaiseCanExecuteChanged();
				int oldCount = CommandItems.Count;
				if (message?.StartsWith('/') ?? false) {
					var commandsAll = MapData != null ? commands.Concat(mapHelperCommands) : commands;
					var matchingCommands = commandsAll.Where(c => c.Contains(message) && string.Compare(c, message, StringComparison.OrdinalIgnoreCase) != 0);
					CommandItems.ReplaceWith(matchingCommands);
				} else if (CommandItems.Count > 0) {
					CommandItems.Clear();
				}
				CommandItemsCount = CommandItems.Count;
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

		private int commandItemsCount;
		public int CommandItemsCount {
			get => commandItemsCount;
			set => SetProperty(ref commandItemsCount, value);
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

		private string timer;
		public string Timer {
			get => timer;
			set => SetProperty(ref timer, value);
		}

		private const string DefaultScores = "Scores: -";
		private string scores = DefaultScores;
		public string Scores {
			get => scores;
			set => SetProperty(ref scores, value);
		}

		private Timer centerPrintTimer;
		private string centerPrint;
		public string CenterPrint {
			get => centerPrint;
			set => SetProperty(ref centerPrint, value, () => {
				ShowCenterPrint = true;
				if (centerPrintTimer == null) {
					centerPrintTimer = new Timer(3000.0);
					centerPrintTimer.Elapsed += TimerElapsed;
					centerPrintTimer.Start();
				} else {
					centerPrintTimer.Interval = 3000.0;
				}
			});
		}

		private void TimerElapsed(object sender, ElapsedEventArgs ev) {
			ShowCenterPrint = false;
			centerPrintTimer.Elapsed -= TimerElapsed;
			centerPrintTimer = null;
		}

		private bool showCenterPrint;
		public bool ShowCenterPrint {
			get => showCenterPrint;
			set => SetProperty(ref showCenterPrint, value);
		}

		private EntityData []entities;
		public EntityData []Entities {
			get => entities;
			set => SetProperty(ref entities, value);
		}

		private MapData mapData;
		public MapData MapData {
			get => mapData;
			set => SetProperty(ref mapData, value, () => {
				MapFocused = MapData != null && Items.Count == 0;
			});
		}

		private bool mapFocused;
		public bool MapFocused {
			get => mapFocused;
			set => SetProperty(ref mapFocused, value);
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
				LoadMapData();
				PrepareForModification();
				if (message.Status.HasValue) {
					Status = message.Status.Value;
				}
				Title = message.ServerInfo.HostName;
				Players = $"{message.ServerInfo.Clients.ToString(CultureInfo.InvariantCulture)}/{message.ServerInfo.MaxClients.ToString(CultureInfo.InvariantCulture)}";
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

		private void PrepareForModification() {
			var mod = gameClient.Modification;
			if (ServerInfo.Version == JKClient.ClientVersion.Q3_v1_32 && mod == JKClient.GameModification.Unknown) {
				if (ServerInfo.GameName.Contains("cpma", StringComparison.InvariantCultureIgnoreCase)) {
					mod = JKClient.GameModification.CPMA;
				} else if (ServerInfo.GameName.Contains("osp", StringComparison.InvariantCultureIgnoreCase)) {
					mod = JKClient.GameModification.OSP;
				}
			}
			switch (mod) {
			case JKClient.GameModification.BaseEnhanced:
			case JKClient.GameModification.BaseEntranced:
				commands.UnionWith(baseEnhancedCommands);
				break;
			case JKClient.GameModification.JAPlus:
				commands.UnionWith(jaPlusCommands);
				break;
			case JKClient.GameModification.CPMA:
			case JKClient.GameModification.OSP:
				if (DateTime.TryParseExact(ServerInfo["gamedate"], "MMM dd yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
					gameClient.SetUserInfoKeyValue("osp_client", dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
				}
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
				var dialogList = new DialogListViewModel(gameClient.ClientGame?.ClientsInfo?.Where(ci => ci.InfoValid).Select(ci => new DialogItemVM() {
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
					gameClient.FrameExecuted += FrameExecuted;
				});
			} else {
				gameClient.ViewModel = this;
				gameClient.FrameExecuted += FrameExecuted;
			}
			Items.CollectionChanged += ItemsCollectionChanged;
		}

		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs ev) {
			if (Items.Count == 1 && AppSettings.MinimapOptions.HasFlag(MinimapOptions.FirstUnfocus))
				MapFocused = false;
		}

		private List<EntityData> ents;
		private void FrameExecuted(long frameTime){
			var clientGame = gameClient.ClientGame;
			Entities = getEntities(clientGame)
				.OrderBy(entity => entity.Team)
				.Concat(this.gameClient.TempEntities)
				.ToArray();
			Timer = $"{clientGame.Timer/60000}:{clientGame.Timer/1000%60:D2}";

			switch (ServerInfo.GameType) {
				case JKClient.GameType.Siege when clientGame is JAClientGame jaClientGame:
					Scores = scoresSiege(jaClientGame, ServerInfo);
					break;
				case >= JKClient.GameType.Team:
					Scores = $"^7Red: ^1{scoresString(clientGame.Scores1)}^7 Blue: ^4{scoresString(clientGame.Scores2)}";
					break;
				default:
					var clientInfo = clientGame.ClientsInfo != null ? clientGame.ClientsInfo.Where(ci => ci.InfoValid).Append(default).Aggregate((ci1, ci2) => ci1.CompareTo(ref ci2) > 0 ? ci1 : ci2) : default;
					Scores = clientInfo.InfoValid ? $"Leader: {scoresString(clientInfo.Score)}\n{clientInfo.Name}" : DefaultScores;
					break;
			}

			IEnumerable<EntityData> getEntities(JKClient.ClientGame clientGame) {
				var options = AppSettings.MinimapOptions;
				if (!options.HasFlag(MinimapOptions.Enabled))
					return Array.Empty<EntityData>();

				var entities = clientGame.Entities;
				ents?.Clear();
				ents ??= new(entities.Length);
				for (int i = 0; i < entities.Length; i++) {
					ref var cent = ref entities[i];
					if (!cent.Valid || clientGame.IsNoDraw(ref cent)) {
						continue;
					}
					JKClient.ClientEntity playerCent = default;
					if (options.HasFlag(MinimapOptions.Players) && clientGame.IsPlayer(ref cent) && !clientGame.IsInvisible(ref cent)
						&& (clientGame.ClientsInfo?[cent.ClientNum].InfoValid ?? false)) {
						var team = clientGame.ClientsInfo[cent.ClientNum].Team;
						string name = clientGame.ClientsInfo[cent.ClientNum].Name;
						if (clientGame.IsFollowed(ref cent))
							name = "👁 " + name;
						ents.Add(new EntityData(EntityType.Player, (Team)team) {
							Origin = cent.LerpOrigin,
							Angles = cent.LerpAngles,
							Name = options.HasFlag(MinimapOptions.Names) ? name : null
						});
					} else if (options.HasFlag(MinimapOptions.Predicted) && clientGame.IsPredictedClient(ref cent) && clientGame.IsInvisible(ref cent)
						&& (clientGame.ClientsInfo?[cent.ClientNum].InfoValid ?? false)) {
						var team = clientGame.ClientsInfo[cent.ClientNum].Team;
						string name = clientGame.ClientsInfo[cent.ClientNum].Name;
						ents.Add(new EntityData(EntityType.Player, (Team)team) {
							Origin = cent.LerpOrigin,
							Angles = cent.LerpAngles,
							Name = options.HasFlag(MinimapOptions.Names) ? name : null
						});
					} else if (options.HasFlag(MinimapOptions.Players) && clientGame.IsVehicle(ref cent, ref playerCent)
						&& (clientGame.ClientsInfo?[cent.Owner].InfoValid ?? false)) {
						var team = clientGame.ClientsInfo[cent.Owner].Team;
						string name = clientGame.ClientsInfo[cent.Owner].Name;
						if (!playerCent.Valid || clientGame.IsNoDraw(ref playerCent)) {
							if (clientGame.IsFollowed(ref playerCent))
								name = "👁 " + name;
							ents.Add(new EntityData(EntityType.Player, (Team)team) {
								Origin = cent.LerpOrigin,
								Angles = cent.LerpAngles,
								Name = options.HasFlag(MinimapOptions.Names) ? name : null
							});
						}
						ents.Add(new EntityData(EntityType.Vehicle, (Team)team) {
							Origin = cent.LerpOrigin,
							Angles = cent.LerpAngles
						});
					} else if (options.HasFlag(MinimapOptions.Weapons) && clientGame.IsMissile(ref cent)) {
						var weapon = clientGame.GetWeapon(ref cent, out bool altFire);
						var color = weapon.ToColor(altFire);
						ents.Add(new EntityData(EntityType.Projectile) {
							Origin = cent.LerpOrigin,
							Angles = cent.LerpAngles,
							Color = color
						});
					}
					if (options.HasFlag(MinimapOptions.Flags) && clientGame.GetFlagTeam(ref cent) is var team3 && team3 is JKClient.Team.Red or JKClient.Team.Blue) {
						ents.Add(new EntityData(EntityType.Flag, (Team)team3) {
							Origin = cent.LerpOrigin,
							Angles = cent.LerpAngles
						});
					}
				}
				return ents;
			}

			static string scoresSiege(JAClientGame clientGame, JKClient.ServerInfo serverInfo) {
				if (clientGame.SiegeRoundState != 0) {
					switch (clientGame.SiegeRoundState) {
						case 1:
							return "Waiting for players";
						case 2:
							int time = JAClientGame.SiegeRoundBeginTime - (clientGame.Time - clientGame.SiegeRoundTime);
							time = Math.Clamp(time, 0, JAClientGame.SiegeRoundBeginTime);
							time /= 1000;
							time++;
							if (time < 1)
								time = 1;
							return $"Round begins in {time}";
						default:
							return DefaultScores;
					}
				} else if (clientGame.SiegeRoundTime != 0) {
//dirty, clientGame data has to be read only
					clientGame.SiegeRoundTime = 0;
				} else if (clientGame.SiegeRoundBeganTime != 0) {
					int timedValue = clientGame.BeatingSiegeTime;
					int timeRemaining;
					bool forward = true;
					if (JKClient.Common.Atoi(serverInfo["g_siegeTeamSwitch"]) != 0 && clientGame.BeatingSiegeTime == 0) {
						forward = true;
						timeRemaining = clientGame.Time - clientGame.SiegeRoundBeganTime;
					} else {
						forward = false;
						timeRemaining = clientGame.SiegeRoundBeganTime + timedValue - clientGame.Time;
					}
					if (timedValue > 0 && timeRemaining > timedValue) {
						timeRemaining = timedValue;
					} else if (timeRemaining < 0) {
						timeRemaining = 0;
					}
					timeRemaining /= 1000;
					return $"Round time: ^{(forward ? 2 : 1)}{timeRemaining/60}:{timeRemaining%60:D2}";
				}
				return DefaultScores;
			}
			static string scoresString(int scores) => scores == JKClient.ClientGame.ScoreNotPresent ? "-" : scores.ToString();
		}

		public override void ViewDisappearing() {
			if (gameClient != null) {
				gameClient.FrameExecuted -= FrameExecuted;
				gameClient.ViewModel = null;
			}
			if (Items != null) {
				Items.CollectionChanged -= ItemsCollectionChanged;
			}
			base.ViewDisappearing();
		}

		private void Prepare(JKClient.ServerInfo serverInfo, bool isFavourite) {
			gameClient = gameClientsService.GetClient(serverInfo, true);
			Items = gameClient.Items;
			Status = gameClient.Status;
			Title = gameClient.ServerInfo.HostName;
			Players = $"{gameClient.ServerInfo.Clients.ToString(CultureInfo.InvariantCulture)}/{gameClient.ServerInfo.MaxClients.ToString(CultureInfo.InvariantCulture)}";
			IsFavourite = isFavourite;
			LoadMapData();
			PrepareForModification();
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
//force IsLoading = true;
			Status = gameClient.Status;
			await cacheService.SaveRecentServer(ServerInfo);
			await gameClient.Connect(false);

			async Task close() {
				await NavigationService.Close(this);
			}
		}

		public bool ShouldLetOtherNavigateFromRoot(object data) {
			if (data is JKClient.ServerInfo serverInfo)
				return this.ServerInfo != serverInfo;
			else if (data is string s && JKClient.NetAddress.FromString(s) is var address)
				return this.ServerInfo?.Address != address;
			return true;
		}

		private void LoadMapData() {
			if (!AppSettings.MinimapOptions.HasFlag(MinimapOptions.Enabled))
				return;

			if (string.Compare(mapName, ServerInfo.MapName, StringComparison.OrdinalIgnoreCase) == 0)
				return;

			var resourceMapName = mapName = ServerInfo.MapName;

			if (mapName == null) {
				MapData = null;
				return;
			}

//special case for mappers who cannot properly recreate a map
			const string siege_hoth = "siege_hoth";
			if (mapName.StartsWith(siege_hoth) && mapName.Length > siege_hoth.Length && mapName[siege_hoth.Length] is char c && char.IsDigit(c) && (c - '0') >= 3)
				resourceMapName = "mp/siege_hoth2";
			var assembly = this.GetType().Assembly;
			string path = $"JKChat.Core.Resources.Minimaps.{ServerInfo.Version.ToGame()}.{resourceMapName.Replace('/','.')}.xy,";
			foreach (var resourceName in assembly.GetManifestResourceNames()) {
				if (resourceName.StartsWith(path, StringComparison.OrdinalIgnoreCase)) {
					string []minMax = resourceName.Substring(path.Length).Replace(".png","").Split(',');
					if (minMax?.Length == 6
						&& int.TryParse(minMax[0], out int minX)
						&& int.TryParse(minMax[1], out int minY)
						&& int.TryParse(minMax[2], out int minZ)
						&& int.TryParse(minMax[3], out int maxX)
						&& int.TryParse(minMax[4], out int maxY)
						&& int.TryParse(minMax[5], out int maxZ)) {
						MapData = new() {
							Assembly = assembly,
							Path = resourceName,
							Min = new(minX, minY, minZ),
							Max = new(maxX, maxY, maxZ),
							HasShadow = ServerInfo.Version == JKClient.ClientVersion.JO_v1_02
						};
						return;
					}
					break;
				}
			}
			MapData = null;
		}
	}
}

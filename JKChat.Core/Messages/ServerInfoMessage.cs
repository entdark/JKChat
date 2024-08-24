using JKChat.Core.Models;

using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class ServerInfoMessage : MvxMessage {
		internal ConnectionStatus? Status { get; init; }
		internal JKClient.ServerInfo ServerInfo { get; init; }
		internal bool UpdateInCache { get; init; } = true;
		internal ServerInfoMessage(object sender, JKClient.ServerInfo serverInfo, ConnectionStatus? status = null) : base(sender) {
			Status = status;
			ServerInfo = serverInfo;
		}
	}
}
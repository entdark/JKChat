using System;
using System.Linq;

using JKChat.Core.Models;

using JKClient;

namespace JKChat.Core.Helpers {
	internal static class ClientVersionExtensions {
		public const int ClientVersionJA = 1 << (int)ClientVersion.JA_v1_00 | 1 << (int)ClientVersion.JA_v1_01;
		public const int ClientVersionJO = 1 << (int)ClientVersion.JO_v1_02 | 1 << (int)ClientVersion.JO_v1_03 | 1 << (int)ClientVersion.JO_v1_04;
		public const int ClientVersionJK = ClientVersionJA | ClientVersionJO;
		public const int ClientVersionQ3 = 1 << (int)ClientVersion.Q3_v1_32;
		public const int ClientVersionAll = ClientVersionQ3 | ClientVersionJK;
		public static readonly ClientVersion []Versions = Enum.GetValues(typeof(ClientVersion)).Cast<ClientVersion>().ToArray();
		public static string ToDisplayString(this int version) {
			return version switch {
				ClientVersionJA => "Jedi Academy",
				(1 << (int)ClientVersion.JA_v1_00) => "Jedi Academy v1.00",
				(1 << (int)ClientVersion.JA_v1_01) => "Jedi Academy v1.01",
				ClientVersionJO => "Jedi Outcast",
				(1 << (int)ClientVersion.JO_v1_02) => "Jedi Outcast v1.02",
				(1 << (int)ClientVersion.JO_v1_03) => "Jedi Outcast v1.03",
				(1 << (int)ClientVersion.JO_v1_04) => "Jedi Outcast v1.04",
				ClientVersionJK => "Jedi Knight Series",
//				ClientVersionQ3 => "Quake III Arena",
				(1 << (int)ClientVersion.Q3_v1_32) => "Quake III Arena v1.32",
				ClientVersionAll => "All",
				_ when version != 0 => string.Join(", ", Versions.Where(ver => version.HasField(ver)).Select(gt => gt.ToDisplayString())),
				_ => "Unknown game"
			};
		}
		public static string ToDisplayString(this ClientVersion version) {
			return version.ToBitField().ToDisplayString();
		}
		public static int ToBitField(this ClientVersion version) => 1 << (int)version;
		public static bool HasField(this int version, ClientVersion ver) => (version & ver.ToBitField()) != 0;
		public static int CountVersions(this int version) {
			return Versions.Count(ver => version.HasField(ver));
		}
		internal static Game ToGame(this ClientVersion version) {
			return version switch {
				ClientVersion.JA_v1_00 or ClientVersion.JA_v1_01 => Game.JediAcademy,
				ClientVersion.JO_v1_02 or ClientVersion.JO_v1_03 or ClientVersion.JO_v1_04 => Game.JediOutcast,
				ClientVersion.Q3_v1_32 => Game.Quake3,
				_ => Game.Unknown
			};
		}
	}
	internal static class GameTypeExtensions {
		public const int GameTypeAll = 1 << (int)GameType.FFA | 1 << (int)GameType.Holocron | 1 << (int)GameType.JediMaster | 1 << (int)GameType.Duel | 1 << (int)GameType.PowerDuel | 1 << (int)GameType.SinglePlayer
			| 1 << (int)GameType.Team | 1 << (int)GameType.Siege | 1 << (int)GameType.CTF | 1 << (int)GameType.CTY | 1 << (int)GameType.OneFlagCTF | 1 << (int)GameType.Obelisk | 1 << (int)GameType.Harvester;
		public static readonly GameType []GameTypes = Enum.GetValues(typeof(GameType)).Cast<GameType>().ToArray();
		public static string ToDisplayString(this int gameType, int version = ClientVersionExtensions.ClientVersionAll) {
			bool isQ3 = version == ClientVersionExtensions.ClientVersionQ3;
			bool isJK = (version & ClientVersionExtensions.ClientVersionJK) == version;
			return gameType switch {
				(1 << (int)GameType.FFA) when isQ3 => "Deathmatch",
				(1 << (int)GameType.FFA) when isJK => "Free For All",
				(1 << (int)GameType.FFA) => "Free For All/Deathmatch",
				(1 << (int)GameType.Holocron) => "Holocron",
				(1 << (int)GameType.JediMaster) => "Jedi Master",
				(1 << (int)GameType.Duel) when isQ3 => "Tournament",
				(1 << (int)GameType.Duel) when isJK => "Duel",
				(1 << (int)GameType.Duel) => "Duel/Tournament",
				(1 << (int)GameType.PowerDuel) => "Power Duel",
				(1 << (int)GameType.SinglePlayer) => "Single Player",
				(1 << (int)GameType.Team) when isQ3 => "Team Deathmatch",
				(1 << (int)GameType.Team) when isJK => "Team FFA",
				(1 << (int)GameType.Team) => "Team FFA/Team Deathmatch",
				(1 << (int)GameType.Siege) => "Siege",
				(1 << (int)GameType.CTF) => "Capture the Flag",
				(1 << (int)GameType.CTY) => "Capture the Ysalamiri",
				(1 << (int)GameType.OneFlagCTF) => "1 Flag CTF",
				(1 << (int)GameType.Obelisk) => "Obelisk",
				(1 << (int)GameType.Harvester) => "Harvester",
				GameTypeAll => "All",
				_ when gameType != 0 => string.Join(", ", GameTypes.Where(gt => gameType.HasField(gt)).Select(gt => gt.ToDisplayString())),
				_ => "Unknown game type"
			};
		}
		public static string ToDisplayString(this GameType gameType, int version = ClientVersionExtensions.ClientVersionAll) {
			return gameType.ToBitField().ToDisplayString(version);
		}
		public static string ToDisplayString(this GameType gameType, ClientVersion version) {
			return gameType.ToBitField().ToDisplayString(version.ToBitField());
		}
		public static int ToBitField(this GameType gameType) => 1 << (int)gameType;
		public static bool HasField(this int gameType, GameType gt) => (gameType & gt.ToBitField()) != 0;
		public static int CountGameTypes(this int gameType) {
			return GameTypes.Count(ver => gameType.HasField(ver));
		}
	}
}
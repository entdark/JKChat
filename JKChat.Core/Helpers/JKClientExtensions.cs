using System;
using System.Drawing;
using System.Linq;

using JKChat.Core.Models;

using JKClient;

using Weapon = JKClient.ClientGame.Weapon;

namespace JKChat.Core.Helpers {
	internal static class ClientVersionExtensions {
		public const int ClientVersionJA = 1 << (int)ClientVersion.JA_v1_00 | 1 << (int)ClientVersion.JA_v1_01;
		public const int ClientVersionJO = 1 << (int)ClientVersion.JO_v1_02 | 1 << (int)ClientVersion.JO_v1_03 | 1 << (int)ClientVersion.JO_v1_04;
		public const int ClientVersionJK = ClientVersionJA | ClientVersionJO;
		public const int ClientVersionQ3 = 1 << (int)ClientVersion.Q3_v1_32;
		public const int ClientVersionAll = ClientVersionQ3 | ClientVersionJK;
		public static readonly ClientVersion []Versions = Enum.GetValues<ClientVersion>();
		public static string ToDisplayString(this int version, bool countMultiple = false) {
			return version switch {
				ClientVersionJA => "Jedi Academy",
				_ when version.MatchesField(ClientVersion.JA_v1_00) => "Jedi Academy v1.00",
				_ when version.MatchesField(ClientVersion.JA_v1_01) => "Jedi Academy v1.01",
				ClientVersionJO => "Jedi Outcast",
				_ when version.MatchesField(ClientVersion.JO_v1_02) => "Jedi Outcast v1.02",
				_ when version.MatchesField(ClientVersion.JO_v1_03) => "Jedi Outcast v1.03",
				_ when version.MatchesField(ClientVersion.JO_v1_04) => "Jedi Outcast v1.04",
				ClientVersionJK => "Jedi Knight Series",
//				ClientVersionQ3 => "Quake III Arena",
				_ when version.MatchesField(ClientVersion.Q3_v1_32) => "Quake III Arena v1.32",
				ClientVersionAll => "All",
				_ when version != 0 && countMultiple => CountVersions(version).ToString(),
				_ when version != 0 => string.Join(", ", Versions.Where(ver => version.HasField(ver)).Select(ver => ver.ToDisplayString())),
				_ => "Unknown game"
			};
		}
		public static string ToDisplayString(this ClientVersion version, bool count = false) {
			return version.ToBitField().ToDisplayString(count);
		}
		public static int ToBitField(this ClientVersion version) => 1 << (int)version;
		public static bool HasField(this int version, ClientVersion ver) => (version & ver.ToBitField()) != 0;
		public static bool MatchesField(this int version, ClientVersion ver) => version == ver.ToBitField();
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
		public static readonly GameType []GameTypes = Enum.GetValues<GameType>();
		public static string ToDisplayString(this int gameType, int version = ClientVersionExtensions.ClientVersionAll, bool countMultiple = false) {
			bool isQ3 = version == ClientVersionExtensions.ClientVersionQ3;
			bool isJK = (version & ClientVersionExtensions.ClientVersionJK) == version;
			return gameType switch {
				_ when gameType.MatchesField(GameType.FFA) && isQ3 => "Deathmatch",
				_ when gameType.MatchesField(GameType.FFA) && isJK => "Free For All",
				_ when gameType.MatchesField(GameType.FFA) => "Free For All/Deathmatch",
				_ when gameType.MatchesField(GameType.Holocron) => "Holocron",
				_ when gameType.MatchesField(GameType.JediMaster) => "Jedi Master",
				_ when gameType.MatchesField(GameType.Duel) && isQ3 => "Tournament",
				_ when gameType.MatchesField(GameType.Duel) && isJK => "Duel",
				_ when gameType.MatchesField(GameType.Duel) => "Duel/Tournament",
				_ when gameType.MatchesField(GameType.PowerDuel) => "Power Duel",
				_ when gameType.MatchesField(GameType.SinglePlayer) => "Single Player",
				_ when gameType.MatchesField(GameType.Team) && isQ3 => "Team Deathmatch",
				_ when gameType.MatchesField(GameType.Team) && isJK => "Team FFA",
				_ when gameType.MatchesField(GameType.Team) => "Team FFA/Team Deathmatch",
				_ when gameType.MatchesField(GameType.Siege) => "Siege",
				_ when gameType.MatchesField(GameType.CTF) => "Capture the Flag",
				_ when gameType.MatchesField(GameType.CTY) => "Capture the Ysalamiri",
				_ when gameType.MatchesField(GameType.OneFlagCTF) => "1 Flag CTF",
				_ when gameType.MatchesField(GameType.Obelisk) => "Obelisk",
				_ when gameType.MatchesField(GameType.Harvester) => "Harvester",
				GameTypeAll => "All",
				_ when gameType != 0 && countMultiple => CountGameTypes(gameType).ToString(),
				_ when gameType != 0 => string.Join(", ", GameTypes.Where(gt => gameType.HasField(gt)).Select(gt => gt.ToDisplayString(version))),
				_ => "Unknown game type"
			};
		}
		public static string ToDisplayString(this GameType gameType, int version = ClientVersionExtensions.ClientVersionAll, bool countMultiple = false) {
			return gameType.ToBitField().ToDisplayString(version, countMultiple);
		}
		public static string ToDisplayString(this GameType gameType, ClientVersion version, bool countMultiple = false) {
			return gameType.ToBitField().ToDisplayString(version.ToBitField(), countMultiple);
		}
		public static int ToBitField(this GameType gameType) => 1 << (int)gameType;
		public static bool HasField(this int gameType, GameType gt) => (gameType & gt.ToBitField()) != 0;
		public static bool MatchesField(this int gameType, GameType gt) => gameType == gt.ToBitField();
		public static int CountGameTypes(this int gameType) {
			return GameTypes.Count(gt => gameType.HasField(gt));
		}
	}
	internal static class ClientInfoExtensions {
		public static long CompareTo(this ClientInfo ci1, ref ClientInfo ci2) {
			return GetComparerKey(ci1) - GetComparerKey(ci2);
		}
		public static long GetComparerKey(this ClientInfo? ci) {
			long score = ci?.Score ?? 0;
			var team = ci?.Team ?? JKClient.Team.Spectator;
			return score + team switch {
				JKClient.Team.Red => (long)int.MaxValue << 2,
				JKClient.Team.Blue => (long)int.MaxValue << 1,
				JKClient.Team.Free => int.MaxValue << 0,
				_ => 0
			};
		}
	}
	internal static class WeaponExtensions {
//colors: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.brushes?view=windowsdesktop-9.0
		public static Color ToColor(this Weapon weapon, bool altFire = false) {
			return weapon switch {
				Weapon.BryarPistol or Weapon.BryarOld or Weapon.Turret => Color.Yellow,
				Weapon.Blaster or Weapon.EmplacedGun => Color.DeepPink,
				Weapon.Disruptor => Color.OrangeRed,
				Weapon.Bowcaster => Color.Lime,
				Weapon.Repeater when altFire => Color.DeepSkyBlue,
				Weapon.Repeater => Color.Yellow,
				Weapon.Demp2 => Color.MediumPurple,
				Weapon.Flechette => Color.Gold,
				Weapon.RocketLauncher => Color.Red,
				Weapon.Thermal or Weapon.GrenadeLauncher => Color.LimeGreen,
				Weapon.Concussion => Color.DodgerBlue,
				Weapon.TripMine => Color.DodgerBlue,
				Weapon.DetPack => Color.OrangeRed,
				Weapon.Machinegun => Color.Khaki,
				Weapon.Shotgun => Color.Khaki,
				Weapon.Lightning => Color.LightYellow,
				Weapon.Railgun => Color.SpringGreen,
				Weapon.Plasmagun => Color.Cyan,
				Weapon.BFG => Color.Lime,
				Weapon.GrapplingHook => Color.SaddleBrown,
				_ => Color.White
			};
		}
	}
}
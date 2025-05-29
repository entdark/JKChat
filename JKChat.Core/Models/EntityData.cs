using System;
using System.Drawing;
using System.Numerics;

namespace JKChat.Core.Models;

public class EntityData {
	public Vector3 Origin { get; init; }
	public Vector3 Origin2 { get; init; }
	public Vector3 Angles { get; init; }
	public string Name { get; init; }
	public EntityType Type { get; init; }
	public DateTime Life { get; init; }
	public int LifeLength { get; init; }
	public Color Color { get; init; }
	public Team Team { get; init; }
	public EntityData(EntityType type, int life = 0) {
		Type = type;
		if (life > 0) {
			LifeLength = life;
			Life = DateTime.UtcNow.AddMilliseconds(life);
		}
	}
	public EntityData(EntityType type, Team team) {
		Type = type;
		Team = team;
		Color = team switch {
			Team.Red => Color.Red,
			Team.Blue => Color.Blue,
			Team.Free => Color.Yellow,
			_ => Color.White
		};
	}
}

public enum EntityType {
	Player,
	Vehicle,
	Flag,
	Shot,
	Projectile,
	Impact
}
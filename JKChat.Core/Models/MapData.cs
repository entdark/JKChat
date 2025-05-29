using System;
using System.Numerics;
using System.Reflection;

namespace JKChat.Core.Models;

public class MapData {
	public Assembly Assembly { get; init; }
	public string Path { get; init; }
	public Vector3 Min { get; init; }
	public Vector3 Max { get; init; }
	public bool HasShadow { get; init; }

	public static bool operator ==(MapData mapData1, MapData mapData2) {
		return string.Compare(mapData1?.Path, mapData2?.Path, StringComparison.OrdinalIgnoreCase) == 0;
	}

	public static bool operator !=(MapData mapData1, MapData mapData2) {
		return (mapData1 == mapData2) != true;
	}
}
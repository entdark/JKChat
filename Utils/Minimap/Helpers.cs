using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utils.Minimap
{
	public static partial class Minimap
	{
		public static void Generate(in string inputFolderPath, in MinimapConfiguration minimapConfiguration = null, bool rethrow = false)
		{
			var bspRegex = BspRegex();
			var zipRecursor = new ZipRecursor(bspRegex, MakeMinimapFromBSP, minimapConfiguration, rethrow: rethrow);
			zipRecursor.HandleFolder(inputFolderPath);
		}

		static void MakeMinimapFromBSP(string filename, byte[] fileData, string path, object userData = null)
		{
			Stack<string> pathStack = new Stack<string>();
			string[] pathPartsArr = path is null ? new string[0] : path.Split(new char[] { '\\', '/' });
			bool mapsFolderFound = false;
			for (int i = pathPartsArr.Length - 1; i >= 0; i--)
			{
				string pathPart = pathPartsArr[i];
				if (pathPart.Equals("maps", StringComparison.InvariantCultureIgnoreCase))
				{
					mapsFolderFound = true;
					break;
				}
				else if (pathPart.EndsWith(".pk3", StringComparison.InvariantCultureIgnoreCase) || pathPart.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				{
					// this is weird. must be some weird isolated file
					pathStack.Clear();
					break;
				}
				else
				{
					pathStack.Push(pathPart);
				}
			}
			if (!mapsFolderFound)
			{
				pathStack.Clear();
			}
			// TODO What if there is some maps folder really low in the hierarchy?
			var sb = new StringBuilder();
			while (pathStack.Count > 0)
			{
				sb.Append(pathStack.Pop());
				sb.Append(Path.DirectorySeparatorChar);
			}
			sb.Append(Path.GetFileNameWithoutExtension(filename));
			string mapname = sb.ToString();
			Debug.WriteLine($"Found {filename} ({mapname}) in {path}");

			var cfg = (userData as MinimapConfiguration) ?? new();
			BSPToMiniMap.MakeMiniMap(mapname, fileData, cfg.OutputFolderPath, cfg.PixelsPerUnit, cfg.MaxWidth, cfg.MaxHeight, cfg.ExtraBorderUnits, cfg.Predicate, cfg.ProgressCallback, cfg.CancellationToken);
		}

		public class MinimapConfiguration
		{
			public string OutputFolderPath { get; init; } = null;
			public float PixelsPerUnit { get; init; } = 0.1f;
			public int MaxWidth { get; init; } = 500;
			public int MaxHeight { get; init; } = 500;
			public int ExtraBorderUnits { get; init; } = 10;
			public Func<string, bool> Predicate { get; init; }
			public Action<float> ProgressCallback { get; init; }
			public CancellationToken? CancellationToken { get; init; }
		}

		[GeneratedRegex(@"\.bsp$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
		private static partial Regex BspRegex();
	}

	public static class Helpers
	{
		public static T ReadBytesAsType<T>(this BinaryReader br, long byteOffset = -1, SeekOrigin seekOrigin = SeekOrigin.Begin)
		{
			if (!(byteOffset == -1 && seekOrigin == SeekOrigin.Begin))
			{
				br.BaseStream.Seek(byteOffset, seekOrigin);
			}
			byte[] bytes = br.ReadBytes(Marshal.SizeOf(typeof(T)));
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T retVal = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();
			return retVal;
		}
		public static void WriteBytesFromType<T>(this BinaryWriter bw, T value, long byteOffset = -1, SeekOrigin seekOrigin = SeekOrigin.Begin)
		{
			if (!(byteOffset == -1 && seekOrigin == SeekOrigin.Begin))
			{
				bw.BaseStream.Seek(byteOffset, seekOrigin);
			}
			byte[] byteData = new byte[Marshal.SizeOf(typeof(T))];
			GCHandle handle = GCHandle.Alloc(byteData, GCHandleType.Pinned);
			// TODO Not sure if this is safe? Am I expected to do some fancy AllocHGlobal and then Marshal.Copy?! Why? This seems to work so whats the problem?
			Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
			bw.Write(byteData);
		}
		public static byte[] BytesFromType<T>(T value)
		{
			byte[] byteData = new byte[Marshal.SizeOf(typeof(T))];
			GCHandle handle = GCHandle.Alloc(byteData, GCHandleType.Pinned);
			// TODO Not sure if this is safe? Am I expected to do some fancy AllocHGlobal and then Marshal.Copy?! Why? This seems to work so whats the problem?
			Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
			return byteData;
		}

		public static T ArrayBytesAsType<T, B>(B data, int byteOffset = 0)
		{
			GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			T retVal = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + byteOffset, typeof(T));
			handle.Free();
			return retVal;
		}
		public static float zCross2d(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
		{
			return ((p2.X - p1.X) * (p3.Y - p2.Y)) - ((p2.Y - p1.Y) * (p3.X - p2.X));
		}
		public static bool pointInTriangle2D(ref Vector3 point, ref Vector3 t1, ref Vector3 t2, ref Vector3 t3)
		{
			float a = zCross2d(ref t1, ref t2, ref point);
			float b = zCross2d(ref t2, ref t3, ref point);
			float c = zCross2d(ref t3, ref t1, ref point);

			return a > 0 && b > 0 && c > 0 || a < 0 && b < 0 && c < 0;
		}
	}
}
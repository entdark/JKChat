using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Utils.Minimap
{
	static class BSPToMiniMap
	{
		const int MAX_QPATH = 64;		// max length of a quake game pathname

		const int LUMP_ENTITIES = 0;
		const int LUMP_SHADERS = 1;
		const int LUMP_PLANES = 2;
		const int LUMP_NODES = 3;
		const int LUMP_LEAFS = 4;
		const int LUMP_LEAFSURFACES = 5;
		const int LUMP_LEAFBRUSHES = 6;
		const int LUMP_MODELS = 7;
		const int LUMP_BRUSHES = 8;
		const int LUMP_BRUSHSIDES = 9;
		const int LUMP_DRAWVERTS = 10;
		const int LUMP_DRAWINDEXES = 11;
		const int LUMP_FOGS = 12;
		const int LUMP_SURFACES = 13;
		const int LUMP_LIGHTMAPS = 14;
		const int LUMP_LIGHTGRID = 15;
		const int LUMP_VISIBILITY = 16;
		const int LUMP_LIGHTARRAY = 17;
		const int HEADER_LUMPS = 18; // q3 is 19. but its ok if we dont read the last one.

		const int MAXLIGHTMAPS = 4;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct lump_t
		{
			public int fileofs, filelen;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct dheader_t
		{
			public int ident;
			public int version;

			public unsafe fixed int lumps[HEADER_LUMPS * 2]; // lump_t is really just 2 ints: fileofs, filelen

			public lump_t GetLump(int index)
			{
				return Helpers.ArrayBytesAsType<lump_t, dheader_t>(this, (int)Marshal.OffsetOf<dheader_t>("lumps") + Marshal.SizeOf(typeof(lump_t)) * index);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct dshader_t
		{
			public unsafe fixed byte shader[MAX_QPATH];
			public int surfaceFlags;
			public int contentFlags;
			public unsafe string getShaderName()
			{
				fixed (byte* shaderPtr = shader)
				{
					return Encoding.ASCII.GetString(shaderPtr, MAX_QPATH).TrimEnd((Char)0);
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct mapVert_t
		{
			public unsafe fixed float xyz[3];
			public unsafe fixed float st[2];
			public unsafe fixed float lightmap[MAXLIGHTMAPS * 2];
			public unsafe fixed float normal[3];
			public unsafe fixed byte color[MAXLIGHTMAPS * 4];
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct mapVertQ3_t
		{
			public unsafe fixed float xyz[3];
			public unsafe fixed float st[2];
			public unsafe fixed float lightmap[2];
			public unsafe fixed float normal[3];
			public unsafe fixed byte color[4];
		}
		class mapVertWrapper
		{
			bool isQ3;
			public float[] xyz = new float[3];
			public unsafe mapVertWrapper(bool isQ3A, BinaryReader br)
			{
				isQ3 = isQ3A;
				if (isQ3)
				{
					mapVertQ3_t a = Helpers.ReadBytesAsType<mapVertQ3_t>(br);
					xyz[0] = a.xyz[0];
					xyz[1] = a.xyz[1];
					xyz[2] = a.xyz[2];
				}
				else
				{
					mapVert_t a = Helpers.ReadBytesAsType<mapVert_t>(br);
					xyz[0] = a.xyz[0];
					xyz[1] = a.xyz[1];
					xyz[2] = a.xyz[2];
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct dsurface_t
		{
			public int shaderNum;
			public int fogNum;
			public int surfaceType;

			public int firstVert;
			public int numVerts;

			public int firstIndex;
			public int numIndexes;

			public unsafe fixed byte lightmapStyles[MAXLIGHTMAPS], vertexStyles[MAXLIGHTMAPS];
			public unsafe fixed int lightmapNum[MAXLIGHTMAPS];
			public unsafe fixed int lightmapX[MAXLIGHTMAPS], lightmapY[MAXLIGHTMAPS];
			public int lightmapWidth, lightmapHeight;

			public unsafe fixed float lightmapOrigin[3];
			public unsafe fixed float lightmapVecs[9]; // for patches, [0] and [1] are lodbounds

			public int patchWidth;
			public int patchHeight;
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct dsurfaceQ3_t
		{
			public int shaderNum;
			public int fogNum;
			public int surfaceType;

			public int firstVert;
			public int numVerts;

			public int firstIndex;
			public int numIndexes;

			public int lightmapNum;
			public int lightmapX, lightmapY;
			public int lightmapWidth, lightmapHeight;

			public unsafe fixed float lightmapOrigin[3];
			public unsafe fixed float lightmapVecs[9]; // for patches, [0] and [1] are lodbounds

			public int patchWidth;
			public int patchHeight;
		}
		class dsurfaceWrapper
		{
			object dsurface;
			bool isQ3;
			public int surfaceType { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.surfaceType : (dsurface as dsurface_t?).Value.surfaceType; } }
			public int patchHeight { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.patchHeight : (dsurface as dsurface_t?).Value.patchHeight; } }
			public int patchWidth { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.patchWidth : (dsurface as dsurface_t?).Value.patchWidth; } }
			public int numIndexes { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.numIndexes : (dsurface as dsurface_t?).Value.numIndexes; } }
			public int shaderNum { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.shaderNum : (dsurface as dsurface_t?).Value.shaderNum; } }
			public int numVerts { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.numVerts : (dsurface as dsurface_t?).Value.numVerts; } }
			public int firstVert { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.firstVert : (dsurface as dsurface_t?).Value.firstVert; } }
			public int firstIndex { get { return isQ3 ? (dsurface as dsurfaceQ3_t?).Value.firstIndex : (dsurface as dsurface_t?).Value.firstIndex; } }
			public dsurfaceWrapper(bool isQ3A, BinaryReader br)
			{
				isQ3 = isQ3A;
				dsurface = isQ3 ? Helpers.ReadBytesAsType<dsurfaceQ3_t>(br) : Helpers.ReadBytesAsType<dsurface_t>(br);
			}
		}

		class EzAccessTriangle
		{
			public Vector3[] points;
			public float[] normalAngles = new float[3];
			public float[] mins = new float[3];
			public float[] maxs = new float[3];
		}


		enum ShaderType
		{
			NORMAL,
			SYSTEM,
			SKY,
		}

		static JsonSerializerOptions jsonOpts = new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
		public static readonly string minimapsPathDefault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "minimaps");

		public static MiniMapMeta DecodeMiniMapMeta(string data)
		{
			return JsonSerializer.Deserialize<MiniMapMeta>(data, jsonOpts);
		}

		public static unsafe void MakeMiniMap(string mapNameClean, byte[] bspData, string minimapsPath, float pixelsPerUnit = 0.1f, int maxWidth = 4000, int maxHeight = 4000, int extraBorderUnits = 100)
		{
			bool isQ3 = false;
			string minimapPath = Path.Combine(minimapsPath ?? minimapsPathDefault, mapNameClean);
			string propsJsonFilePath = Path.Combine(minimapPath, "meta.json");
			//string xyPath = Path.Combine(minimapPath, "xy.png");
			//string xzPath = Path.Combine(minimapPath, "xz.png");
			//string yzPath = Path.Combine(minimapPath, "yz.png");

//			if (File.Exists(propsJsonFilePath))
			if (new DirectoryInfo(minimapPath) is { Exists: true } d && d.GetFiles() is { Length: > 0 })
			{
				Console.WriteLine($"Minimap for {mapNameClean} seems to already exist. Overwrite [y/n]?");
				return;
/*/				string line = Console.ReadLine();
				if (line?.StartsWith("n", StringComparison.InvariantCultureIgnoreCase) ?? false)
				{
					return;
				}
/*				if (MessageBox.Show($"Minimap for {mapNameClean} seems to already exist. Overwrite?", "Minimap already exists", MessageBoxButton.YesNo) == MessageBoxResult.No)
				{
					return;
				}*/
			}

			using (MemoryStream ms = new MemoryStream(bspData))
			{
				using (BinaryReader br = new BinaryReader(ms))
				{
					dheader_t header = Helpers.ReadBytesAsType<dheader_t>(br);
					if (header.version != 1)
					{
						if (header.version == 46)
						{
							isQ3 = true;
						}
						else
						{
							throw new Exception("BSP header version is not 1 or 46");
						}
					}

					Directory.CreateDirectory(minimapPath);

					lump_t shadersLump = header.GetLump(LUMP_SHADERS);

					int shaderCount = shadersLump.filelen / Marshal.SizeOf(typeof(dshader_t));

					// Make look up table that quickly tells us what kind of shader a particular shader index is. Is it a system shader or sky shader? So we quickly see whether a surface should be considered as "walkable"
					ShaderType[] shaderTypeLUT = new ShaderType[shaderCount];
					string[] shaderNames = new string[shaderCount];
					br.BaseStream.Seek(shadersLump.fileofs, SeekOrigin.Begin);
					for (int i = 0; i < shaderCount; i++)
					{
						dshader_t shaderHere = Helpers.ReadBytesAsType<dshader_t>(br);
						string shaderName = shaderHere.getShaderName();
						shaderNames[i] = shaderName;
						if (shaderName.StartsWith("textures/system/"))
						{
							shaderTypeLUT[i] = ShaderType.SYSTEM;
						}
						else if (shaderName.StartsWith("textures/skies/"))
						{
							shaderTypeLUT[i] = ShaderType.SKY;
						}
						else
						{
							shaderTypeLUT[i] = ShaderType.NORMAL;
						}
					}

					// Read surfaces, indices and verts into arrays and figure out total map dimensions
					float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
					float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
					float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

					lump_t surfacesLump = header.GetLump(LUMP_SURFACES);
					lump_t vertsLump = header.GetLump(LUMP_DRAWVERTS);
					lump_t indexLump = header.GetLump(LUMP_DRAWINDEXES);

					int surfaceSize = isQ3 ? Marshal.SizeOf(typeof(dsurfaceQ3_t)) : Marshal.SizeOf(typeof(dsurface_t));
					int surfacesCount = surfacesLump.filelen / surfaceSize;
					int vertSize = isQ3 ? Marshal.SizeOf(typeof(mapVertQ3_t)) : Marshal.SizeOf(typeof(mapVert_t));
					int vertsCount = vertsLump.filelen / vertSize;
					int indexCount = indexLump.filelen / sizeof(int);

					dsurfaceWrapper[] surfaces = new dsurfaceWrapper[surfacesCount];
					mapVertWrapper[] verts = new mapVertWrapper[vertsCount];
					int[] indices = new int[indexCount];

					// Read verts
					br.BaseStream.Seek(vertsLump.fileofs, SeekOrigin.Begin);
					for (int i = 0; i < vertsCount; i++)
					{
						verts[i] = new mapVertWrapper(isQ3, br);
					}
					// Read indices
					br.BaseStream.Seek(indexLump.fileofs, SeekOrigin.Begin);
					for (int i = 0; i < indexCount; i++)
					{
						indices[i] = br.ReadInt32();
					}

					List<EzAccessTriangle> triangles = new List<EzAccessTriangle>();

					// Read surfaces and do some processing
					br.BaseStream.Seek(surfacesLump.fileofs, SeekOrigin.Begin);
					for (int i = 0; i < surfacesCount; i++)
					{
						dsurfaceWrapper surf = surfaces[i] = new dsurfaceWrapper(isQ3, br);
						//if (surf.surfaceType != 2) continue;
						int surfaceCount = surf.surfaceType == 2 ? ((surf.patchHeight - 1) * (surf.patchWidth - 0)) * 2 : surf.numIndexes;
						if (surf.surfaceType != 2 && (surfaceCount % 3) > 0)
						{
							throw new Exception("(surf.numIndexes % 3) > 0");
						}
						if (shaderTypeLUT[surf.shaderNum] == ShaderType.NORMAL)
						{
							for (int v = 0; v < surf.numVerts; v++)
							{
								mapVertWrapper vert = verts[surf.firstVert + v];
								minX = Math.Min(vert.xyz[0], minX);
								maxX = Math.Max(vert.xyz[0], maxX);
								minY = Math.Min(vert.xyz[1], minY);
								maxY = Math.Max(vert.xyz[1], maxY);
								minZ = Math.Min(vert.xyz[2], minZ);
								maxZ = Math.Max(vert.xyz[2], maxZ);
								//if(minX < 1000 || minY < 1000)
								//{
								//   string shaderName = shaderNames[surf.shaderNum];
								//    int a = 1;
								//}
							}

							int advance = surf.surfaceType == 2 ? 1 : 3;
							if (surf.surfaceType == 2)
							{
								surfaceCount -= 2;
							}

							for (int index = 0; index < surfaceCount; index += advance)
							{
								int vertIndex0;
								int vertIndex1;
								int vertIndex2;

								if (surf.surfaceType == 2) // Shitty workaround for patches. Check and fix up some day? Dunno if I'm winding the triangles the right way.
								{
									int indexHere = index / 2;
									int surfaceTriangle = index % 2;
									if (surfaceTriangle == 0)
									{
										vertIndex0 = surf.firstVert + indexHere;
										vertIndex1 = surf.firstVert + surf.patchWidth + indexHere;
										vertIndex2 = surf.firstVert + surf.patchWidth + indexHere + 1;
									}
									else
									{
										vertIndex0 = surf.firstVert + indexHere;
										vertIndex1 = surf.firstVert + surf.patchWidth + indexHere + 1;
										vertIndex2 = surf.firstVert + indexHere + 1;
									}
								}
								else
								{
									vertIndex0 = surf.firstVert + indices[surf.firstIndex + index];
									vertIndex1 = surf.firstVert + indices[surf.firstIndex + index + 1];
									vertIndex2 = surf.firstVert + indices[surf.firstIndex + index + 2];
								}

								mapVertWrapper vert1 = verts[vertIndex0];
								mapVertWrapper vert2 = verts[vertIndex1];
								mapVertWrapper vert3 = verts[vertIndex2];


								EzAccessTriangle triangle = new EzAccessTriangle()
								{
									points = new Vector3[] {
										new Vector3() { X = vert1.xyz[0], Y = vert1.xyz[1], Z = vert1.xyz[2] },
										new Vector3() { X = vert2.xyz[0], Y = vert2.xyz[1], Z = vert2.xyz[2] },
										new Vector3() { X = vert3.xyz[0], Y = vert3.xyz[1], Z = vert3.xyz[2] },
									},
								};

								triangle.mins[0] = Math.Min(vert1.xyz[0], Math.Min(vert2.xyz[0], vert3.xyz[0]));
								triangle.mins[1] = Math.Min(vert1.xyz[1], Math.Min(vert2.xyz[1], vert3.xyz[1]));
								triangle.mins[2] = Math.Min(vert1.xyz[2], Math.Min(vert2.xyz[2], vert3.xyz[2]));
								triangle.maxs[0] = Math.Max(vert1.xyz[0], Math.Max(vert2.xyz[0], vert3.xyz[0]));
								triangle.maxs[1] = Math.Max(vert1.xyz[1], Math.Max(vert2.xyz[1], vert3.xyz[1]));
								triangle.maxs[2] = Math.Max(vert1.xyz[2], Math.Max(vert2.xyz[2], vert3.xyz[2]));

								// Calculate normal
								Vector3 normal = Vector3.Normalize(Vector3.Cross(triangle.points[2] - triangle.points[1], triangle.points[2] - triangle.points[0]));

								triangle.normalAngles = new float[3]
								{
									360.0f * (float)Math.Acos(normal.X) / (float)Math.PI / 2.0f,
									360.0f * (float)Math.Acos(normal.Y) / (float)Math.PI / 2.0f,
									360.0f * (float)Math.Acos(normal.Z) / (float)Math.PI / 2.0f
								};

								if (triangle.normalAngles[0] > 90.0f && triangle.normalAngles[0] < 180.0f)
								{
									triangle.normalAngles[0] -= 180.0f;
								}
								if (triangle.normalAngles[1] > 90.0f && triangle.normalAngles[1] < 180.0f)
								{
									triangle.normalAngles[1] -= 180.0f;
								}
								if (triangle.normalAngles[2] > 90.0f && triangle.normalAngles[2] < 180.0f)
								{
									triangle.normalAngles[2] -= 180.0f;
								}

								// Just roughly consider walkable surfaces
								//if(angle < 45.0f)
								//{
								triangles.Add(triangle);
								//}

							}
						}
					}

					float imgMinX = minX - extraBorderUnits;
					float imgMaxX = maxX + extraBorderUnits;
					float imgMinY = minY - extraBorderUnits;
					float imgMaxY = maxY + extraBorderUnits;
					float imgMinZ = minZ - extraBorderUnits;
					float imgMaxZ = maxZ + extraBorderUnits;

					var miniMapMeta = new MiniMapMeta()
					{
						minX = imgMinX,
						minY = imgMinY,
						minZ = imgMinZ,
						maxX = imgMaxX,
						maxY = imgMaxY,
						maxZ = imgMaxZ
					};

					float xRange = imgMaxX - imgMinX;
					float yRange = imgMaxY - imgMinY;
					float zRange = imgMaxZ - imgMinZ;
					int xRes = (int)(Math.Ceiling(imgMaxX - imgMinX) * pixelsPerUnit);
					int yRes = (int)(Math.Ceiling(imgMaxY - imgMinY) * pixelsPerUnit);
					int zRes = (int)(Math.Ceiling(imgMaxZ - imgMinZ) * pixelsPerUnit);
					if (xRes > maxWidth)
					{
						yRes = yRes * maxWidth / xRes;
						zRes = zRes * maxHeight / xRes;
						xRes = maxWidth;
					}
					if (yRes > maxHeight)
					{
						xRes = xRes * maxHeight / yRes;
						zRes = zRes * maxHeight / yRes;
						yRes = maxHeight;
					}
					if (zRes > maxHeight)
					{
						xRes = xRes * maxHeight / zRes;
						yRes = yRes * maxWidth / zRes;
						zRes = maxHeight;
					}

					for (int axis = 0; axis < 1/*3*/; axis++)
					{
						// Do all 3 axes
						float zValueScale = 1.0f / (maxZ - minZ);
						float zValueOffset = -minZ;

						int xResHere = xRes;
						int yResHere = yRes;


						float imgMinXHere = imgMinX;
						float imgMinYHere = imgMinY;
						float xRangeHere = xRange;
						float yRangeHere = yRange;
						int triangleMinMaxIndexX = 0;
						int triangleMinMaxIndexY = 1;
						int angleExcludeIndex = 2;
						float minAngle = 0.0f;
						float maxAngle = 45.0f;
						string axisName = "xy";
						switch (axis)
						{
							case 1: // Side1
								yResHere = zRes;
								imgMinYHere = imgMinZ;
								yRangeHere = zRange;
								triangleMinMaxIndexY = 2;
								angleExcludeIndex = 1;
								zValueScale = 1.0f / (maxY - minY);
								zValueOffset = -minY;
								//minAngle = -89.0f;
								maxAngle = 89.0f;
								axisName = "xz";
								break;
							case 2: // Side2
								xResHere = yRes;
								imgMinXHere = imgMinY;
								xRangeHere = yRange;
								triangleMinMaxIndexX = 1;
								yResHere = zRes;
								imgMinYHere = imgMinZ;
								yRangeHere = zRange;
								triangleMinMaxIndexY = 2;
								angleExcludeIndex = 0;
								zValueScale = 1.0f / (maxX - minX);
								zValueOffset = -minX;
								//minAngle = -89.0f;
								maxAngle = 89.0f;
								axisName = "yz";
								break;
						}

						float[] pixelData = new float[xResHere * yResHere];
						float[] pixelDataDivider = new float[xResHere * yResHere];

						Parallel.For(0, yResHere, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, (y) =>
						{
							//for(int y = 0; y < yRes; y++)
							//{
							for (int x = 0; x < xResHere; x++)
							{
								Vector3 pixelWorldCoordinate = new Vector3() { X = (float)(x /*xResHere - x - 1*/) / (float)xResHere * xRangeHere + imgMinXHere, Y = (float)(yResHere - y - 1) / (float)yResHere * yRangeHere + imgMinYHere };

								for (int t = 0; t < triangles.Count; t++)
								{
									if (triangles[t].normalAngles[angleExcludeIndex] > maxAngle || triangles[t].normalAngles[angleExcludeIndex] < minAngle
										|| pixelWorldCoordinate.X < triangles[t].mins[triangleMinMaxIndexX] || pixelWorldCoordinate.Y < triangles[t].mins[triangleMinMaxIndexY]
										|| pixelWorldCoordinate.X > triangles[t].maxs[triangleMinMaxIndexX] || pixelWorldCoordinate.Y > triangles[t].maxs[triangleMinMaxIndexY]
										)
									{
										continue; // Quick skip triangles that obviously don't apply.
									}
									Vector3 point1 = triangles[t].points[0];
									Vector3 point2 = triangles[t].points[1];
									Vector3 point3 = triangles[t].points[2];
/*									switch (axis)
									{
										case 1: // Side1
											(point1.Z, point1.Y) = (point1.Y, point1.Z);
											(point2.Z, point2.Y) = (point2.Y, point2.Z);
											(point3.Z, point3.Y) = (point3.Y, point3.Z);
											break;
										case 2: // Side2
											(point1.X, point1.Y) = (point1.Y, point1.X);
											(point2.X, point2.Y) = (point2.Y, point2.X);
											(point3.X, point3.Y) = (point3.Y, point3.X);
											(point1.Z, point1.Y) = (point1.Y, point1.Z);
											(point2.Z, point2.Y) = (point2.Y, point2.Z);
											(point3.Z, point3.Y) = (point3.Y, point3.Z);
											break;
									}*/
									if (Helpers.pointInTriangle2D(ref pixelWorldCoordinate, ref point1, ref point2, ref point3))
									{
										float Z = zValueScale * (zValueOffset + (point1.Z + point2.Z + point3.Z) / 3f);// dumb but should work good enough
										pixelData[y * xResHere + x] += 1.0f + Z;
										pixelDataDivider[y * xResHere + x]++;
									}
								}
							}
							//}
						});

						//float[] pixelDiffData = new float[xRes * yRes];

						var image = new Image<La16>(xResHere, yResHere);

						image.ProcessPixelRows(processor =>
						{
							// Kinda edge detection
							for (int y = 0; y < processor.Height; y++)
							{
								var pixelRow = processor.GetRowSpan(y);
								for (int x = 0; x < pixelRow.Length; x++)
								{
									if (pixelData[y * xResHere + x] > 0)
									{
										byte color = (byte)Math.Clamp(255.0f * (pixelData[y * xResHere + x] / pixelDataDivider[y * xResHere + x] - 1.0f), 0, 255);

										ref var pixel = ref pixelRow[x];
										pixel.L = Math.Max(pixel.L, color);
										pixel.A = 255;
									}
									for (int yJitter = Math.Max(0, y - 1); yJitter < Math.Min(y + 1, yResHere); yJitter++)
									{
										for (int xJitter = Math.Max(0, x - 1); xJitter < Math.Min(x + 1, xResHere); xJitter++)
										{
											float diff = pixelData[yJitter * xResHere + xJitter] / Math.Max(1f, pixelDataDivider[yJitter * xResHere + xJitter]) - pixelData[y * xResHere + x] / Math.Max(1f, pixelDataDivider[y * xResHere + x]);
											if (diff != 0)
											{
												byte delta = (byte)Math.Clamp(255.0f * Math.Pow(Math.Abs(diff), 0.22f), 0, 255);

												ref var pixel = ref pixelRow[x];
												pixel.L = Math.Max(pixel.L, delta);
												pixel.A = 255;
											}
										}
									}

								}
							}
						});

						image.Save(Path.Combine(minimapPath, $"{axisName},{miniMapMeta}.png"));
						image.Dispose();
					}

//					File.WriteAllText(propsJsonFilePath, JsonSerializer.Serialize(miniMapMeta, jsonOpts));
				}
			}
		}



	}

	public class ByteImage
	{
		public byte[] imageData;
		public int stride;
		public int width, height;
//		public PixelFormat pixelFormat;

		public ByteImage(byte[] imageDataA, int strideA, int widthA, int heightA/*, PixelFormat pixelFormatA*/)
		{
			imageData = imageDataA;
			stride = strideA;
			width = widthA;
			height = heightA;
//			pixelFormat = pixelFormatA;
		}

		public int Length
		{
			get { return imageData.Length; }
		}

		public byte this[int index]
		{
			get
			{
				return imageData[index];
			}

			set
			{
				imageData[index] = value;
			}
		}
	}
}
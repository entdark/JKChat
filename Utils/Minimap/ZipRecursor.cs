//#define SHARPZIPLIB
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SharpCompress.Readers;
using SharpCompress.Archives.SevenZip;
#if SHARPZIPLIB
using ICSharpCode.SharpZipLib.Zip;
#else
using System.IO.Compression;
#endif

namespace Utils.Minimap
{
	class ZipRecursor
	{
		Dictionary<string, Dictionary<int, List<string>>> versionsWhere = new Dictionary<string, Dictionary<int, List<string>>>();

		Dictionary<string, List<byte[]>> versions = new Dictionary<string, List<byte[]>>();
		Regex searchFile = null;
		Action<string, byte[], string, object> _foundFileCallback = null;
		object _userData = null;
		bool _trackVersions = false;
		bool _rethrow;
		bool _generatingMinimap;
		string pathRoot = "";

		public ZipRecursor(Regex fileSearchRegex, Action<string, byte[], string, object> foundFileCallback, object userData = null, bool trackVersions = false, bool rethrow = false)
		{
			if (fileSearchRegex is null || foundFileCallback is null)
			{
				throw new InvalidOperationException("ZipRecursor: Regex can't be null");
			}
			searchFile = fileSearchRegex;
			_foundFileCallback = foundFileCallback;
			_userData = userData;
			_trackVersions = trackVersions;
			_rethrow = rethrow;
		}
		public void HandleFolder(string folderPath)
		{
			string mainFolderPath = folderPath;
			pathRoot = folderPath;
			AnalyzeFolder(mainFolderPath);
		}

		static readonly string[] SharpCompressFormats = new string[] { ".rar", ".tar", ".tar.bz2", ".tbz2", ".tar.gz", ".tgz", ".tar.lz", ".tlz", ".tar.xz", ".txz", ".gz", };
		public void HandleFile(string file)
		{
			pathRoot = Path.GetDirectoryName(file);
			// TODO Try to make all the folder paths consistent across the class
			string folderPath = Path.GetDirectoryName(file);
			if (searchFile.Match(file).Success)
			{
				HandleMatchedFile(file, folderPath, File.ReadAllBytes(file));
			}
			else if (file.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || file.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
#if SHARPZIPLIB
					using (ZipFile mainArchive = new ZipFile(file))
					{
						AnalyzeZipFile(Path.Combine(folderPath, file), mainArchive, searchFile, ref versionsWhere, ref versions);
					}
#else
					using (ZipArchive mainArchive = ZipFile.OpenRead(file))
					{
						AnalyzeZipFile(Path.Combine(folderPath, file), mainArchive, searchFile);
					}
#endif
				}
				catch (Exception exception)
				{
					Debug.WriteLine($"Error opening: {Path.Combine(folderPath, file)}", exception.Message);
					if (_rethrow)
						throw new Exception($"Error opening: {Path.Combine(folderPath, file)}", exception);
				}
			}
			else if (file.EndsWith($".7z", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					using (SevenZipArchive mainArchive = SevenZipArchive.Open(file))
					{
						Analyze7ZipFile(Path.Combine(folderPath, file), mainArchive, searchFile);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error opening: {Path.Combine(folderPath, file)}", ex.Message);
					if (_rethrow)
						throw;
				}
			}
			else
			{
				bool isSharpCompressFormat = false;
				foreach (string format in SharpCompressFormats)
				{
					if (file.EndsWith(format, StringComparison.OrdinalIgnoreCase))
					{
						isSharpCompressFormat = true;
						break;
					}
				}
				if (isSharpCompressFormat)
				{
					try
					{
						// may be archive. may not.
						using (Stream stream = File.OpenRead(file))
						using (IReader reader = ReaderFactory.Open(stream))
						{
							AnalyzeSharpCompressReader(Path.Combine(folderPath, file), reader, searchFile);
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error opening: {Path.Combine(folderPath, file)}", ex.Message);
						if (_rethrow)
							throw;
					}
				}
			}

		}

		public string GetVersionsReport()
		{
			if (!_trackVersions) return null;

			StringBuilder sb = new StringBuilder();
			foreach (var kvp in versions)
			{
				sb.Append($"\n{kvp.Key}:\n");
				for (int i = 0; i < kvp.Value.Count; i++)
				{

					sb.Append($"Version {i} is in the following paths:\n");
					foreach (string path in versionsWhere[kvp.Key][i])
					{
						sb.Append($"\t{path}\n");
					}
				}
			}
			return sb.ToString();
		}

		void AnalyzeFolder(string folderPath)
		{
			string[] listOfFiles = null;
			string[] listOfFolders = null;
			try
			{
				listOfFiles = Directory.GetFiles(folderPath);
				listOfFolders = Directory.GetDirectories(folderPath);
			}
			catch (Exception exception)
			{
				PrintAndThrow(exception);
//				Helpers.logToFile(e.ToString());
				return; // maybe for some reason the folder was not accessible, like junction tahts not connected
			}

			foreach (string file in listOfFiles)
			{
				if (searchFile.Match(file).Success)
				{
					HandleMatchedFile(file, folderPath, File.ReadAllBytes(file));
				}
				else if (file.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || file.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
#if SHARPZIPLIB
					using (ZipFile mainArchive = new ZipFile(file))
					{
						AnalyzeZipFile(Path.Combine(folderPath, file), mainArchive, searchFile, ref versionsWhere, ref versions);
					}
#else
						using (ZipArchive mainArchive = ZipFile.OpenRead(file))
						{
							AnalyzeZipFile(Path.Combine(folderPath, file), mainArchive, searchFile);
						}
#endif
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {Path.Combine(folderPath, file)}", exception);
					}
				}
				else if (file.EndsWith($".7z", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using (SevenZipArchive mainArchive = SevenZipArchive.Open(file))
						{
							Analyze7ZipFile(Path.Combine(folderPath, file), mainArchive, searchFile);
						}
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {Path.Combine(folderPath, file)}", exception);
					}
				}
				else
				{
					bool isSharpCompressFormat = false;
					foreach (string format in SharpCompressFormats)
					{
						if (file.EndsWith(format, StringComparison.OrdinalIgnoreCase))
						{
							isSharpCompressFormat = true;
							break;
						}
					}
					if (isSharpCompressFormat)
					{
						try
						{
							// may be archive. may not.
							using (Stream stream = File.OpenRead(file))
							using (IReader reader = ReaderFactory.Open(stream))
							{
								AnalyzeSharpCompressReader(Path.Combine(folderPath, file), reader, searchFile);
							}
						}
						catch (Exception exception)
						{
							PrintAndThrow($"Error opening: {Path.Combine(folderPath, file)}", exception);
						}
					}
				}
			}

			foreach (string folder in listOfFolders)
			{
				AnalyzeFolder(folder);
			}
		}
#if SHARPZIPLIB
	static void AnalyzeZipFile(string path, ZipFile mainArchive, Regex searchFile, ref Dictionary<string, Dictionary<int, List<string>>> versionsWhere, ref Dictionary<string, List<byte[]>> versions)
#else
		void AnalyzeZipFile(string path, ZipArchive mainArchive, Regex searchFile)
#endif
		{
#if SHARPZIPLIB
		foreach (ZipEntry entry in mainArchive)
#else
			foreach (ZipArchiveEntry entry in mainArchive.Entries)
#endif
			{
				if (entry.Name.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || entry.Name.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
#if SHARPZIPLIB
					using (Stream zipStream = mainArchive.GetInputStream(entry))
					{
						using (MemoryStream ms = new MemoryStream())
						{
							zipStream.CopyTo(ms);
							using (ZipFile subArchive = new ZipFile(ms))
							{
								AnalyzeZipFile(Path.Combine(path, entry.Name), subArchive, searchFile, ref versionsWhere, ref versions);
							}
						}
					}
#else
						using (Stream zipStream = entry.Open())
						{
							using (ZipArchive subArchive = new ZipArchive(zipStream))
							{
								AnalyzeZipFile(Path.Combine(path, entry.FullName), subArchive, searchFile);
							}
						}
#endif
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {path}/{entry.FullName}", exception);
					}
				}
				else if (entry.Name.EndsWith($".7z", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using (Stream zipStream = entry.Open())
						{
							using (SevenZipArchive mainArchive2 = SevenZipArchive.Open(zipStream))
							{
								Analyze7ZipFile(Path.Combine(path, entry.FullName), mainArchive2, searchFile);
							}
						}
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {Path.Combine(path, entry.FullName)}", exception);
					}
				}
				else
				{

					bool isSharpCompressFormat = false;
					foreach (string format in SharpCompressFormats)
					{
						if (entry.Name.EndsWith(format, StringComparison.OrdinalIgnoreCase))
						{
							isSharpCompressFormat = true;
							break;
						}
					}
					if (isSharpCompressFormat)
					{
						try
						{
							// may be archive. may not.
							using (Stream stream = entry.Open())
							using (IReader reader2 = ReaderFactory.Open(stream))
							{
								AnalyzeSharpCompressReader(Path.Combine(path, entry.FullName), reader2, searchFile);
							}
						}
						catch (Exception exception)
						{
							PrintAndThrow($"Error opening: {Path.Combine(path, entry.FullName)}", exception);
						}
					}
				}

				if (searchFile.Match(entry.Name).Success)
				{
#if SHARPZIPLIB
				byte[] version = ReadFileFromZipEntry(mainArchive,entry);
#else
					byte[] version = ReadFileFromZipEntry(entry);
#endif
					HandleMatchedFile(entry.FullName, path, version);

				}
			}
		}
		void Analyze7ZipFile(string path, SevenZipArchive mainArchive, Regex searchFile)
		{
			foreach (SevenZipArchiveEntry entry in mainArchive.Entries)
			{
				if (entry.Key.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || entry.Key.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using (Stream zipStream = entry.OpenEntryStream())
						{
							using (ZipArchive subArchive = new ZipArchive(zipStream))
							{
								AnalyzeZipFile(Path.Combine(path, entry.Key), subArchive, searchFile);
							}
						}
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {path}/{entry.Key}", exception);
					}
				}
				else if (entry.Key.EndsWith($".7z", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using (Stream zipStream = entry.OpenEntryStream())
						{
							using (SevenZipArchive mainArchive2 = SevenZipArchive.Open(zipStream))
							{
								Analyze7ZipFile(Path.Combine(path, entry.Key), mainArchive2, searchFile);
							}
						}
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {Path.Combine(path, entry.Key)}", exception);
					}
				}
				else
				{

					bool isSharpCompressFormat = false;
					foreach (string format in SharpCompressFormats)
					{
						if (entry.Key.EndsWith(format, StringComparison.OrdinalIgnoreCase))
						{
							isSharpCompressFormat = true;
							break;
						}
					}
					if (isSharpCompressFormat)
					{
						try
						{
							// may be archive. may not.
							using (Stream stream = entry.OpenEntryStream())
							using (IReader reader2 = ReaderFactory.Open(stream))
							{
								AnalyzeSharpCompressReader(Path.Combine(path, entry.Key), reader2, searchFile);
							}
						}
						catch (Exception exception)
						{
							PrintAndThrow($"Error opening: {Path.Combine(path, entry.Key)}", exception);
						}
					}
				}

				if (searchFile.Match(Path.GetFileNameWithoutExtension(entry.Key)).Success)
				{
					byte[] version = ReadFileFrom7ZipEntry(entry);
					HandleMatchedFile(entry.Key, path, version);
				}
			}
		}

		void AnalyzeSharpCompressReader(string path, IReader reader, Regex searchFile)
		{
			while (reader.MoveToNextEntry())
			{
				if (!reader.Entry.IsDirectory)
				{
					SharpCompress.Common.IEntry entry = reader.Entry;
					//Debug.WriteLine(reader.Entry.Key);
					if (entry.Key.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || entry.Key.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							using (Stream zipStream = reader.OpenEntryStream())
							{
								using (ZipArchive subArchive = new ZipArchive(zipStream))
								{
									AnalyzeZipFile(Path.Combine(path, entry.Key), subArchive, searchFile);
								}
							}
						}
						catch (Exception exception)
						{
							PrintAndThrow($"Error opening: {path}/{entry.Key}", exception);
						}
					}
					else if (entry.Key.EndsWith($".7z", StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							using (Stream zipStream = reader.OpenEntryStream())
							{
								using (SevenZipArchive mainArchive2 = SevenZipArchive.Open(zipStream))
								{
									Analyze7ZipFile(Path.Combine(path, entry.Key), mainArchive2, searchFile);
								}
							}
						}
						catch (Exception exception)
						{
							PrintAndThrow($"Error opening: {Path.Combine(path, entry.Key)}", exception);
						}
					}
					else
					{

						bool isSharpCompressFormat = false;
						foreach (string format in SharpCompressFormats)
						{
							if (entry.Key.EndsWith(format, StringComparison.OrdinalIgnoreCase))
							{
								isSharpCompressFormat = true;
								break;
							}
						}
						if (isSharpCompressFormat)
						{
							try
							{
								// may be archive. may not.
								using (Stream stream = reader.OpenEntryStream())
								using (IReader reader2 = ReaderFactory.Open(stream))
								{
									AnalyzeSharpCompressReader(Path.Combine(path, entry.Key), reader2, searchFile);
								}
							}
							catch (Exception exception)
							{
								PrintAndThrow($"Error opening: {Path.Combine(path, entry.Key)}", exception);
							}
						}
					}


					if (searchFile.Match(Path.GetFileNameWithoutExtension(entry.Key)).Success)
					{
						byte[] version = ReadFileFromSharpCompressEntry(reader);
						HandleMatchedFile(entry.Key, path, version);

					}
				}
			}
			/*
			foreach (ZipArchiveEntry entry in mainArchive.Entries)
			{
				if (entry.Name.EndsWith($".zip", StringComparison.OrdinalIgnoreCase) || entry.Name.EndsWith($".pk3", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using (Stream zipStream = entry.Open())
						{
							using (ZipArchive subArchive = new ZipArchive(zipStream))
							{
								AnalyzeZipFile(Path.Combine(path, entry.Name), subArchive, searchFile);
							}
						}
					}
					catch (Exception exception)
					{
						PrintAndThrow($"Error opening: {path}/{entry.Name}", exception);
					}
				}
				if (searchFile.Match(entry.Name).Success)
				{
					byte[] version = ReadFileFromZipEntry(entry);
					HandleMatchedFile(entry.FullName, path, version);

				}
			}*/
		}

		private void PrintAndThrow(Exception exception)
		{
			Debug.WriteLine(exception.ToString());
			if (_rethrow)
				throw exception;
		}

		private void PrintAndThrow(string message, Exception exception)
		{
			Debug.WriteLine(message, exception.Message);
			bool allowRethrow = _generatingMinimap;
			_generatingMinimap = false;
			if (_rethrow && allowRethrow)
				throw new Exception(message, exception);
		}

		void HandleMatchedFile(string fileName, string path, byte[] version)
		{
			string entryNameLower = fileName.ToLower();
			entryNameLower = Path.GetFileName(entryNameLower);
			path = Path.GetDirectoryName(Path.Combine(path, fileName));
			bool doProcess = false;
			int index = 0;
			if (_trackVersions)
			{
				int indexHere = 0;
				if (!versions.ContainsKey(entryNameLower))
				{
					versions[entryNameLower] = new List<byte[]>();
				}
				if (!versionsWhere.ContainsKey(entryNameLower))
				{
					versionsWhere[entryNameLower] = new Dictionary<int, List<string>>();
				}
				if ((indexHere = findVersion(entryNameLower, ref version, ref versionsWhere, ref versions)) == -1)
				{
					doProcess = true;
					index = versions[entryNameLower].Count;
					versions[entryNameLower].Add(version);
					versionsWhere[entryNameLower][index] = new List<string>();
					//string targetFileName = entryNameLower;
					//if (indexHere != 0)
					//{
					//	targetFileName = $"{Path.GetFileNameWithoutExtension(entryNameLower)}_version{(indexHere + 1)}{Path.GetExtension(entryNameLower)}";
					//}
					//File.WriteAllBytes(targetFileName, version);
					//File.SetLastWriteTime(targetFileName, entry.LastWriteTime.DateTime);
				}
				versionsWhere[entryNameLower][indexHere].Add(path);
			}
			else
			{
				doProcess = true;
			}
			if (doProcess)
			{
				string relPath = Path.GetRelativePath(pathRoot, path);
				_generatingMinimap = true;
				_foundFileCallback(entryNameLower, version, relPath, _userData);
				_generatingMinimap = false;
			}
		}


		static int findVersion(string fileName, ref byte[] version, ref Dictionary<string, Dictionary<int, List<string>>> versionsWhere, ref Dictionary<string, List<byte[]>> versions)
		{
			List<byte[]> versionsHere = versions[fileName];
			for (int i = 0; i < versionsHere.Count; i++)
			{
				if (versionsHere[i].SequenceEqual(version))
				{
					return i;
				}
			}
			return -1;
		}

#if SHARPZIPLIB
	static byte[] ReadFileFromZipEntry(ZipFile mainFile, ZipEntry entry)
	{
		using (BinaryReader reader = new BinaryReader(mainFile.GetInputStream(entry)))
		{
			return reader.ReadBytes((int)entry.Size);
		}
	}
#else
		static byte[] ReadFileFromZipEntry(ZipArchiveEntry entry)
		{
			using (BinaryReader reader = new BinaryReader(entry.Open()))
			{
				return reader.ReadBytes((int)entry.Length);
			}
		}
#endif
		static byte[] ReadFileFromSharpCompressEntry(IReader sharpCompressReader)
		{
			using (BinaryReader reader = new BinaryReader(sharpCompressReader.OpenEntryStream()))
			{
				return reader.ReadBytes((int)sharpCompressReader.Entry.Size);
			}
		}
		static byte[] ReadFileFrom7ZipEntry(SevenZipArchiveEntry entry)
		{
			using (BinaryReader reader = new BinaryReader(entry.OpenEntryStream()))
			{
				return reader.ReadBytes((int)entry.Size);
			}
		}
	}
}
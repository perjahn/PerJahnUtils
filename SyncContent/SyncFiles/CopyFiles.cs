using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncFiles
{
	class CopyFiles
	{
		public static bool compareMetadata { get; set; }
		public static bool simulate { get; set; }
		public static string[] identifiers { get; set; }
		public static long maxsize { get; set; }
		public static string exclude { get; set; }

		static void Log(string message, bool verbose = false, ConsoleColor? color = null)
		{
			ConsoleColor oldcolor = Console.ForegroundColor;
			if (color.HasValue)
			{
				Console.ForegroundColor = color.Value;
			}
			LogWriter.WriteLine(message, verbose);
			if (color.HasValue)
			{
				Console.ResetColor();
			}
		}

		public static void SyncFiles(string sourcefile, string targetfile, string sourcepath, string targetpath)
		{
			string[] sourcefiles = GetLines(sourcefile);
			string[] targetfiles = GetLines(targetfile);

			if (sourcefiles == null || targetfiles == null)
			{
				return;
			}

			//string source = sourcepath.StartsWith(@"\\") ? sourcepath.TrimStart('\\').Split('\\')[0].Split('.')[0] : sourcepath;
			//string target = sourcepath.StartsWith(@"\\") ? targetpath.TrimStart('\\').Split('\\')[0].Split('.')[0] : targetpath;
			Log("Syncing: " + sourcepath + " -> " + targetpath, false, ConsoleColor.Magenta);

			Log("Source files: " + sourcefiles.Length);
			Log("Target files: " + targetfiles.Length);

			if (!string.IsNullOrEmpty(exclude))
			{
				Log("Exclude file pattern: '" + exclude + "'");
			}
			if (identifiers != null)
			{
				Log("Identifiers: " + identifiers.Length);
			}
			if (maxsize > 0)
			{
				Log("Max file size: " + maxsize);
			}
			if (simulate)
			{
				Log("Running in simulation mode.");
			}


			Log("Calculating files to copy...");
			string[] filestocopy = ExcludeFiles(sourcefiles, targetfiles);
			Log("Files to copy: " + filestocopy.Length);

			ShowFastStatistics(targetfiles, filestocopy);


			string[] missingfolders = filestocopy.Select(f => Path.GetDirectoryName(f.Split('\t')[2])).Distinct().ToArray();
			Log("Potentially missing target folders: " + missingfolders.Length);

			//ShowSlowStatistics(targetpath, filestocopy);

			if (Environment.UserInteractive)
			{
				Log(Environment.NewLine + "Press Enter to start copying...");
				Console.ReadLine();
			}

			CreateFolders(targetpath, missingfolders);

			Copy(sourcepath, targetpath, filestocopy);
		}

		static string[] GetLines(string filename)
		{
			try
			{
				return File.ReadAllLines(filename).Where(l => l != string.Empty).ToArray();
			}
			catch (FileNotFoundException ex)
			{
				LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
				return null;
			}
			catch (DirectoryNotFoundException ex)
			{
				LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
				return null;
			}
			catch (UnauthorizedAccessException ex)
			{
				LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
				return null;
			}
			catch (IOException ex)
			{
				LogWriter.WriteConsoleColor(ex.Message, ConsoleColor.Red);
				return null;
			}
		}

		static bool ShouldCopyProductFile(string path)
		{
			if (path.StartsWith("ProductContent"))
			{
				if (identifiers.Any(path.Contains))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		static string[] ExcludeFiles(string[] filerows, string[] targetfiles)
		{
			int count1 = filerows.Length;
			string[] filestocopy = filerows.Except(targetfiles).ToArray();
			int count2 = filestocopy.Length;
			Log("Excluded files (exists in target): " + (count1 - count2));


			if (true)
			{
				count1 = filestocopy.Length;
				filestocopy = filestocopy.Where(f => !Regex.IsMatch(Path.GetFileName(f.Split('\t')[2]), ".*DS_Store", RegexOptions.IgnoreCase)).ToArray();
				count2 = filestocopy.Length;
				Log("Excluded files (DS_Store): " + (count1 - count2));

				count1 = filestocopy.Length;
				filestocopy = filestocopy.Where(f => !Regex.IsMatch(Path.GetFileName(f.Split('\t')[2]), "thumbs.db", RegexOptions.IgnoreCase)).ToArray();
				count2 = filestocopy.Length;
				Log("Excluded files (thumbs.db): " + (count1 - count2));
			}


			if (!string.IsNullOrEmpty(exclude))
			{
				if (LogWriter.verbose)
				{
					string[] junk = filestocopy.Where(f => Regex.IsMatch(f.Split('\t')[2], exclude, RegexOptions.IgnoreCase)).ToArray();
					Log("Excluded files (file pattern): " + junk.Length, true);
					foreach (string filerow in junk)
					{
						Log("Exclude file: '" + filerow + "'", true);
					}
				}

				count1 = filestocopy.Length;
				filestocopy = filestocopy.Where(f => !Regex.IsMatch(f.Split('\t')[2], exclude, RegexOptions.IgnoreCase)).ToArray();
				count2 = filestocopy.Length;
				Log("Excluded files (file pattern): " + (count1 - count2));
			}


			if (identifiers != null)
			{
				if (LogWriter.verbose)
				{
					string[] junk = filestocopy.Where(f => f.Split('\t')[2].StartsWith("ProductContent") && !identifiers.Any(i => f.Split('\t')[2].StartsWith(@"ProductContent\" + i))).ToArray();
					Log("Excluded files (identifiers): " + junk.Length, true);
					foreach (string filerow in junk)
					{
						Log("Exclude file: '" + filerow + "'", true);
					}
				}

				count1 = filestocopy.Length;
				DateTime t1 = DateTime.Now;
				filestocopy = filestocopy.Where(f => !f.Split('\t')[2].StartsWith("ProductContent") || identifiers.Any(i => f.Split('\t')[2].StartsWith(@"ProductContent\" + i))).ToArray();
				DateTime t2 = DateTime.Now;
				count2 = filestocopy.Length;
				Log("Excluded files (identifiers): " + (count1 - count2) + " (Calc time: " + (t2 - t1) + ")");
			}


			if (maxsize > 0)
			{
				if (LogWriter.verbose)
				{
					string[] junk = filestocopy.Where(f => long.Parse(f.Split('\t')[1]) > maxsize).ToArray();
					Log("Excluded files (max file size): " + junk.Length, true);
					foreach (string filerow in junk)
					{
						Log("Exclude file: '" + filerow + "'", true);
					}
				}

				count1 = filestocopy.Length;
				filestocopy = filestocopy.Where(f => long.Parse(f.Split('\t')[1]) <= maxsize).ToArray();
				count2 = filestocopy.Length;
				Log("Excluded files (max file size): " + (count1 - count2));
			}


			return filestocopy;
		}

		static void ShowFastStatistics(string[] targetfiles, string[] filestocopy)
		{
			long copysize = filestocopy.Sum(s => long.Parse(s.Split('\t')[1]));
			Log("Files to copy size: " + copysize / 1024 / 1024 + " mb");


			int overwritecount = 0;
			int newcopycount = 0;
			long additionalsize = 0;

			Dictionary<string, long> dic = new Dictionary<string, long>();

			foreach (string filerow in targetfiles)
			{
				string[] tokens = filerow.Split('\t');
				dic.Add(tokens[2], long.Parse(tokens[1]));
			}

			foreach (string filerow in filestocopy)
			{
				string[] tokens = filerow.Split('\t');
				string path = tokens[2];
				long size = long.Parse(tokens[1]);
				if (dic.ContainsKey(path))
				{
					overwritecount++;
					additionalsize += size - dic[path];
				}
				else
				{
					newcopycount++;
					additionalsize += size;
				}
			}

			Log("Free space required: " + additionalsize / 1024 / 1024 + " mb");

			Log("Missing files in target, will be copied: " + newcopycount);
			Log("Different, will be overwritten: " + overwritecount);

			return;
		}

		static void ShowSlowStatistics(string targetpath, string[] missingfiles)
		{
			int existcopycount = 0;
			int copycount = 0;

			foreach (string row in missingfiles)
			{
				string filename = row.Split('\t')[2];
				string targetpath2 = Path.Combine(targetpath, filename);

				if (File.Exists(targetpath2))
				{
					// file already exist, probably different
					existcopycount++;
				}
				else
				{
					// file didn't exist in target
					copycount++;
				}
			}

			Log("Missing files in target, will be copied: " + copycount);
			Log("Different, will be overwritten: " + existcopycount);
		}

		static void CreateFolders(string targetpath, string[] targetfolders)
		{
			foreach (string targetfolder in targetfolders)
			{
				string targetfolderfullpath = Path.Combine(targetpath, targetfolder);

				if (!Directory.Exists(targetfolderfullpath))
				{
					Log("Creating folder: '" + targetfolderfullpath + "'");
					if (!simulate)
					{
						Directory.CreateDirectory(targetfolderfullpath);
					}
				}
			}
		}

		static void Copy(string sourcepath, string targetpath, string[] filestocopy)
		{
			DateTime t1 = DateTime.Now;
			int copiedfiles = 0;
			long copiedsize = 0;
			int errors = 0;
			int equalfilecount = 0;

			foreach (string row in filestocopy)
			{
				long filesize = long.Parse(row.Split('\t')[1]);
				string filename = row.Split('\t')[2];
				string sourcepath2 = Path.Combine(sourcepath, filename);
				string targetpath2 = Path.Combine(targetpath, filename);

				FileInfo fiSource = null;
				FileInfo fiTarget = null;

				try
				{
					if (compareMetadata && File.Exists(targetpath2))
					{
						fiSource = new FileInfo(sourcepath2);
						fiTarget = new FileInfo(targetpath2);
					}
				}
				catch (FileNotFoundException ex)
				{
					Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "'");
					Log(ex.Message);
					errors++;
					continue;
				}
				catch (DirectoryNotFoundException ex)
				{
					Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "'");
					Log(ex.Message);
					errors++;
					continue;
				}
				catch (UnauthorizedAccessException ex)
				{
					Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "'");
					Log(ex.Message);
					errors++;
					continue;
				}
				catch (IOException ex)
				{
					Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "'");
					Log(ex.Message);
					errors++;
					continue;
				}


				try
				{
					if (fiSource != null && fiTarget != null)
					{
						if (fiSource.LastWriteTime == fiTarget.LastWriteTime && fiSource.Length == fiTarget.Length)
						{
							Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "': Files appears equal after all.", true, ConsoleColor.Yellow);
							equalfilecount++;
							continue;
						}

						RemoveRO(targetpath2);
					}
					else
					{
						if (File.Exists(targetpath2))
						{
							RemoveRO(targetpath2);
						}
					}

					Log("Copying: '" + sourcepath2 + "' -> '" + targetpath2 + "'");
					if (!simulate)
					{
						File.Copy(sourcepath2, targetpath2, true);
					}
					copiedfiles++;
					copiedsize += filesize;
				}
				catch (FileNotFoundException ex)
				{
					Log(ex.Message);
					errors++;
				}
				catch (DirectoryNotFoundException ex)
				{
					Log(ex.Message);
					errors++;
				}
				catch (UnauthorizedAccessException ex)
				{
					Log(ex.Message);
					errors++;
				}
				catch (IOException ex)
				{
					Log(ex.Message);
					errors++;
				}
			}

			TimeSpan ts = DateTime.Now - t1;

			long copysize = filestocopy.Sum(s => long.Parse(s.Split('\t')[1]));

			Log("Copied files: " + copiedfiles + " (of " + filestocopy.Length + ")");
			Log("Copied size: " + copiedsize / 1024 / 1024 + " mb (of " + copysize / 1024 / 1024 + " mb)");
			Log("Unexpected identical files: " + equalfilecount);
			Log("Time: " + ts);
			Log("Speed: " + (ts == TimeSpan.Zero ? 0 : (copysize / 1024 / 1024 / ts.TotalSeconds)) + " mb/s");
			Log("Errors: " + errors);
		}

		static void RemoveRO(string filename)
		{
			FileAttributes fa = File.GetAttributes(filename);
			if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
			{
				if (!simulate)
				{
					File.SetAttributes(filename, fa & ~FileAttributes.ReadOnly);
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CopyFullPath
{
	class Program
	{
		// Program identifier
		static string Identifier => "CopyFullPath";
		static string OldIdentifier => "Copy Full Path";

		// Context menu data
		static string ContextMenuLabel => "Copy full path";
		static string ContextMenuIcon => $"\"{Application.ExecutablePath}\",0";

		// Temp file timeout
		static TimeSpan TempFileTimeout = TimeSpan.FromMilliseconds(500);



		// Registry settings
		readonly struct Reg
		{
			// Path in registry to install to
			public static readonly RegistryKey RootKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\");

			// Base locations for context menu entries
			public static string[] Entries => new string[]
			{
				"*",						// all file types
				"Directory",				// selected directory
				@"Directory\Background"		// current directory
			};
		}



		// Display usage info
		static void PrintHelp()
		{
			Console.WriteLine("  Sets the full path for files/directories to the clipboard.");
			Console.WriteLine("");
			Console.WriteLine("Usage:");
			Console.WriteLine($"  {Identifier} [options]... [paths]...");
			Console.WriteLine("");
			Console.WriteLine("Options:");
			Console.WriteLine("  --help       Prints this help message");
			Console.WriteLine("  --install    Adds 'Copy full path' to the Windows context menu");
			Console.WriteLine("  --uninstall  Removes 'Copy full path' from the Windows context menu");
		}



		// Install by adding context menu entries to the registry
		public static void Install()
		{
			// Remove old entries
			Uninstall();

			// Add to registry
			foreach (string entry in Reg.Entries)
			{
				try
				{
					RegistryKey key = Reg.RootKey.CreateSubKey($@"{entry}\shell\{Identifier}");
					key.SetValue("", ContextMenuLabel);
					key.SetValue("Icon", ContextMenuIcon);
					if(entry != @"Directory\Background")
						key.SetValue("MultiSelectModel", "Player");

					RegistryKey commandKey = key.CreateSubKey("command");
					commandKey.SetValue("", $"\"{Application.ExecutablePath}\" %v");

					Console.WriteLine($"Installed to {key}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error installing {Identifier}");
					Console.WriteLine($"Error 0x{ex.HResult:x}: {ex.Message}");
					if (ex.Source != null)
						Console.WriteLine($"  Source: {ex.Source}");
					if (ex.StackTrace != null)
						Console.WriteLine($"  Stacktrace: {ex.StackTrace}");
					Console.WriteLine("");
					Console.WriteLine("Press any key to continue...");
					Console.ReadKey();
				}
			}
		}



		// Uninstall by removing context menu entries from the registry
		public static void Uninstall()
		{
			static void RemoveFromRegistry(string identifier)
			{
				foreach (string entry in Reg.Entries)
					Reg.RootKey.DeleteSubKeyTree($@"{entry}\shell\{identifier}", false);
			}

			RemoveFromRegistry(Identifier);
			RemoveFromRegistry(OldIdentifier);
		}



		// Parse command line argument. Returns true on success, false otherwise.
		static bool ParseCommandArgument(string arg)
		{
			switch (arg)
			{
			case "--help":
				PrintHelp();
				return true;

			case "--install":
				Install();
				return true;

			case "--uninstall":
				Uninstall();
				return true;

			default:
				break;
			}

			return false;
		}



		// Prepend paths from temp file if written to just before launching this instance.
		static void CheckTempFile(ref List<string> paths)
		{
			string tmpFilePath = Path.GetTempPath() + Identifier;

			// Temp file is recently written to, prepend paths
			if(File.Exists(tmpFilePath) && (DateTime.Now - new FileInfo(tmpFilePath).LastWriteTime) < TempFileTimeout)
			{
				string[] oldPaths = File.ReadAllLines(tmpFilePath);
				for (int i = 0; i < oldPaths.Length; i++)
					paths.Insert(i, oldPaths[i]);
			}

			// Write paths to tmp file
			File.WriteAllLines(tmpFilePath, paths);
		}



		static void Main(string[] args)
		{
			// Print usage info if no arguments are provided
			if (args.Length == 0)
			{
				PrintHelp();
				return;
			}

			// Parse command line arguments
			List<string> paths = new();
			foreach (string a in args)
			{
				if(!ParseCommandArgument(a) && (File.Exists(a) || Directory.Exists(a)))
				{
					string p = Path.GetFullPath(a);
					p = p.Replace('\\', '/');
					if(p.Contains(' '))
						p = '"' + p + '"';
					paths.Add(p);
				}
			}

			// Use mutex to only allow a single instance to run at the same time
			using Mutex mtx = new(false, Identifier);
			mtx.WaitOne(Timeout.Infinite, false);

			// Check temp file
			CheckTempFile(ref paths);

			// Assign paths to the clipboard
			if (paths.Count > 0)
			{
				string clipboardText = string.Join(' ', paths);
				foreach(string p in paths)
					Console.WriteLine(p);

				Thread thread = new(() => Clipboard.SetText(clipboardText));
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				thread.Join();
			}
		}
	}
}

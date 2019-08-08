using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace CopyFullPath
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				foreach (string a in args)
				{
					if (a.ToLower() == "-install")
					{
						string cmd = "\\shell\\Copy full path\\command";
						string cmdVal = $"{System.Reflection.Assembly.GetExecutingAssembly().Location} \"%1\"";
						if(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) // is admin
						{
							foreach(string key in new string[]{ "Directory\\Background", "Directory", "*" })
							{
								Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(key + cmd);
								k.SetValue("", cmdVal);
								k.Close();
							}
						}
						else
						{
							foreach(string key in new string[]{ "Software\\Classes\\directory\\Background", "Software\\Classes\\directory", "Software\\Classes\\*" })
							{
								Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(key + cmd);
								k.SetValue("", cmdVal);
								k.Close();
							}
						}
						return;
					}
				}

				Thread thread = new Thread(() => Clipboard.SetText(System.IO.Path.GetFullPath(args[0]).Replace('\\', '/')));
				thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
				thread.Start();
				thread.Join();
			}
		}
	}
}

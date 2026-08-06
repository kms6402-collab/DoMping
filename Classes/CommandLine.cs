using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using DoMping.Views;

namespace DoMping.Classes;

internal class CommandLine
{
	public static List<string> ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = string.Empty;
		List<string> list = new List<string>();
		for (int i = 1; i < commandLineArgs.Length; i++)
		{
			switch (commandLineArgs[i].ToLower())
			{
			case "/i":
			case "-i":
			{
				if (i + 1 < commandLineArgs.Length && int.TryParse(commandLineArgs[i + 1], out var result2) && result2 >= 1 && result2 <= 86400)
				{
					ApplicationOptions.PingInterval = result2 * 1000;
					i++;
				}
				else
				{
					text += $"For switch -i you must specify the number of seconds between {1} and {86400}.{Environment.NewLine}";
				}
				break;
			}
			case "/w":
			case "-w":
			{
				if (commandLineArgs.Length > i + 1 && int.TryParse(commandLineArgs[i + 1], out var result) && result >= 1 && result <= 60)
				{
					ApplicationOptions.PingTimeout = result * 1000;
					i++;
				}
				else
				{
					text += $"For switch -w you must specify the number of seconds between {1} and {60}.{Environment.NewLine}";
				}
				break;
			}
			case "/?":
			case "-?":
			case "-h":
			case "--help":
			{
				DialogWindow dialogWindow = new DialogWindow(DialogWindow.DialogIcon.Info, "Command Line Usage", "DoMping [-i interval] [-w timeout] [<target_host>...] [<path_to_list_of_hosts>...]", "OK", isCancelButtonVisible: false);
				dialogWindow.Topmost = true;
				dialogWindow.ShowDialog();
				Application.Current.Shutdown();
				break;
			}
			default:
				if (File.Exists(commandLineArgs[i]))
				{
					list.AddRange(ReadHostsFromFile(commandLineArgs[i]));
				}
				else
				{
					list.Add(commandLineArgs[i]);
				}
				break;
			}
		}
		if (text.Length > 0)
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow(text + Environment.NewLine + "Command line usage:" + Environment.NewLine + "DoMping [-i interval] [-w timeout] [<target_host>...] [<path_to_list_of_hosts>...]");
			dialogWindow2.Topmost = true;
			dialogWindow2.ShowDialog();
			Application.Current.Shutdown();
		}
		return list;
	}

	private static List<string> ReadHostsFromFile(string path)
	{
		try
		{
			if (new FileInfo(path).Length > 10240)
			{
				throw new FileFormatException();
			}
			return (from x in new List<string>(File.ReadAllLines(path))
				where !string.IsNullOrWhiteSpace(x) && (char.IsLetterOrDigit(x[0]) || x[0] == '[')
				select x.Trim()).ToList();
		}
		catch (FileFormatException)
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow($"The file is too large and cannot be opened. The maximum file size is {10L} kb." + Environment.NewLine + Environment.NewLine + path);
			dialogWindow.Topmost = true;
			dialogWindow.ShowDialog();
			return new List<string>();
		}
		catch
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow("Failed parsing file." + Environment.NewLine + Environment.NewLine + path);
			dialogWindow2.Topmost = true;
			dialogWindow2.ShowDialog();
			return new List<string>();
		}
	}
}

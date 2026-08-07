using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using DoMping.Classes;
using WinForms = System.Windows.Forms;

namespace DoMping.Views;

public partial class MultiInputWindow : Window, IComponentConnector
{
	private const int GWL_STYLE = -16;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_MINIMIZEBOX = 131072;

	public List<ParsedAddress> ParsedEntries { get; private set; } = new List<ParsedAddress>();

	public List<string> Addresses => ParsedEntries.Select((ParsedAddress p) => p.Host).ToList();

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public MultiInputWindow(List<string> addresses = null)
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		};
		if (addresses != null && addresses.Count > 0 && !addresses.All((string x) => string.IsNullOrWhiteSpace(x)))
		{
			MyAddresses.Text = string.Join(Environment.NewLine, addresses);
		}
	}

	private void OK_Click(object sender, RoutedEventArgs e)
	{
		IEnumerable<string> rawLines = MyAddresses.Text.Trim().Split('\n', ',').Select((string x) => x.Trim());
		List<ParsedAddress> parsed = AddressListParser.Parse(rawLines, out List<string> warnings);
		if (warnings.Count > 0)
		{
			string message = "The following entries were skipped or merged:\n\n" + string.Join("\n", warnings.Take(10));
			if (warnings.Count > 10)
			{
				message += $"\n… and {warnings.Count - 10} more.";
			}
			message += $"\n\n{parsed.Count} address(es) will be added. Continue?";
			DialogWindow dialogWindow = DialogWindow.WarningWindow(message, "Continue");
			dialogWindow.Owner = this;
			if (dialogWindow.ShowDialog() != true)
			{
				return;
			}
		}
		ParsedEntries = parsed;
		base.DialogResult = true;
	}

	private void MyAddresses_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Effects = DragDropEffects.Copy;
		e.Handled = true;
	}

	private void MyAddresses_Drop(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return;
		}
		string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
		ImportFile(files[0]);
	}

	private void ImportCsv_Click(object sender, RoutedEventArgs e)
	{
		using WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog();
		openFileDialog.Title = "Import addresses";
		openFileDialog.RestoreDirectory = true;
		openFileDialog.Multiselect = false;
		openFileDialog.Filter = "CSV or text files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
		if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
		{
			ImportFile(openFileDialog.FileName);
		}
	}

	private void ImportFile(string path)
	{
		try
		{
			if (new FileInfo(path).Length > 512000)
			{
				throw new FileFormatException();
			}
			string[] lines;
			if (string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
			{
				lines = ParseCsvLines(File.ReadAllLines(path));
			}
			else
			{
				lines = File.ReadAllLines(path).Where((string x) => !string.IsNullOrWhiteSpace(x) && (char.IsLetterOrDigit(x[0]) || x[0] == '[')).Select((string x) => x.Trim()).ToArray();
			}
			string existing = MyAddresses.Text.Trim();
			string imported = string.Join(Environment.NewLine, lines);
			MyAddresses.Text = string.IsNullOrEmpty(existing) ? imported : (existing + Environment.NewLine + imported);
		}
		catch (FileFormatException)
		{
			DialogWindow dialogWindow = DialogWindow.ErrorWindow("The file is too large and cannot be opened. The maximum file size is 500 kb.");
			dialogWindow.Owner = this;
			dialogWindow.ShowDialog();
		}
		catch
		{
			DialogWindow dialogWindow2 = DialogWindow.ErrorWindow("File could not be opened. Make sure the file is a plain text or CSV file.");
			dialogWindow2.Owner = this;
			dialogWindow2.ShowDialog();
		}
	}

	private static string[] ParseCsvLines(string[] rawLines)
	{
		List<string> result = new List<string>();
		bool skippedHeader = false;
		foreach (string rawLine in rawLines)
		{
			if (string.IsNullOrWhiteSpace(rawLine))
			{
				continue;
			}
			string[] columns = SplitCsvRow(rawLine);
			if (columns.Length == 0)
			{
				continue;
			}
			string host = columns[0].Trim();
			string alias = (columns.Length > 1) ? columns[1].Trim() : string.Empty;
			if (!skippedHeader)
			{
				skippedHeader = true;
				string hostLower = host.ToLowerInvariant();
				if (hostLower == "host" || hostLower == "hostname" || hostLower == "ip" || hostLower == "ip address" || hostLower == "address")
				{
					continue;
				}
			}
			if (string.IsNullOrWhiteSpace(host))
			{
				continue;
			}
			result.Add(string.IsNullOrWhiteSpace(alias) ? host : (host + "/" + alias));
		}
		return result.ToArray();
	}

	private static string[] SplitCsvRow(string line)
	{
		List<string> fields = new List<string>();
		StringBuilder current = new StringBuilder();
		bool inQuotes = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (inQuotes)
			{
				if (c == '"')
				{
					if (i + 1 < line.Length && line[i + 1] == '"')
					{
						current.Append('"');
						i++;
					}
					else
					{
						inQuotes = false;
					}
				}
				else
				{
					current.Append(c);
				}
			}
			else if (c == '"')
			{
				inQuotes = true;
			}
			else if (c == ',')
			{
				fields.Add(current.ToString());
				current.Clear();
			}
			else
			{
				current.Append(c);
			}
		}
		fields.Add(current.ToString());
		return fields.ToArray();
	}

	private void Window_SourceInitialized(object sender, EventArgs e)
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		SetWindowLong(handle, -16, GetWindowLong(handle, -16) & -65537 & -131073);
	}
}

using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using DoMping.Properties;

namespace DoMping.Classes;

internal class Util
{
	public static void SendEmail(string hostStatus, string hostName, string hostAlias)
	{
		string emailServer = ApplicationOptions.EmailServer;
		string emailUser = ApplicationOptions.EmailUser;
		string emailPassword = ApplicationOptions.EmailPassword;
		string emailPort = ApplicationOptions.EmailPort;
		string emailFromAddress = ApplicationOptions.EmailFromAddress;
		string text = "DoMping";
		string emailRecipient = ApplicationOptions.EmailRecipient;
		string subject = "[DoMping] " + hostName + " <> " + Strings.Email_Host + " " + hostStatus;
		hostAlias = ((hostAlias == null || hostAlias.Length <= 0) ? string.Empty : ("(" + hostAlias + ") "));
		string body = hostName + " " + hostAlias + Strings.Email_Verb + " " + hostStatus + "." + Environment.NewLine + DateTime.Now.ToLongDateString() + "  " + DateTime.Now.ToLongTimeString();
		MailMessage mailMessage = new MailMessage();
		try
		{
			SmtpClient smtpClient = new SmtpClient();
			MailAddress mailAddress = ((text.Length <= 0) ? new MailAddress(emailFromAddress) : new MailAddress(emailFromAddress, text));
			smtpClient.Host = emailServer;
			smtpClient.EnableSsl = ApplicationOptions.IsEmailSslEnabled;
			if (ApplicationOptions.IsEmailAuthenticationRequired)
			{
				smtpClient.Credentials = new NetworkCredential(emailUser, emailPassword);
			}
			if (emailPort.Length > 0)
			{
				smtpClient.Port = int.Parse(emailPort);
			}
			mailMessage.From = mailAddress;
			mailMessage.Subject = subject;
			mailMessage.Body = body;
			mailMessage.To.Add(emailRecipient);
			smtpClient.Send(mailMessage);
		}
		catch
		{
		}
		finally
		{
			mailMessage.Dispose();
		}
	}

	public static void SendTestEmail(string serverAddress, string serverPort, bool isSslEnabled, bool isAuthRequired, string username, SecureString password, string mailFrom, string mailRecipient)
	{
		string displayName = "DoMping";
		string subject = "[DoMping] Test Email Notification";
		string body = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + " - This is a test email notification sent by DoMping.";
		using SmtpClient smtpClient = new SmtpClient();
		smtpClient.Host = serverAddress;
		if (serverPort.Length > 0)
		{
			smtpClient.Port = int.Parse(serverPort);
		}
		MailAddress mailAddress = new MailAddress(mailFrom, displayName);
		if (isAuthRequired)
		{
			smtpClient.Credentials = new NetworkCredential(username, password);
		}
		smtpClient.EnableSsl = isSslEnabled;
		using MailMessage mailMessage = new MailMessage();
		mailMessage.From = mailAddress;
		mailMessage.Subject = subject;
		mailMessage.Body = body;
		mailMessage.To.Add(mailRecipient);
		smtpClient.Send(mailMessage);
	}

	public static void ShowError(string message)
	{
		MessageBox.Show(message, Strings.Error_WindowTitle, MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	public static bool IsValidHtmlColor(string htmlColor)
	{
		return new Regex("^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$").IsMatch(htmlColor);
	}

	private const int Pbkdf2Iterations = 100000;

	private static readonly HashAlgorithmName Pbkdf2Algorithm = HashAlgorithmName.SHA256;

	private static byte[] DeriveKey(int keySizeBytes)
	{
		return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes("DoMping/" + Environment.MachineName), Encoding.ASCII.GetBytes(Environment.UserName + "@@domping-salt@@"), Pbkdf2Iterations, Pbkdf2Algorithm, keySizeBytes);
	}

	public static string EncryptStringAES(string plainText)
	{
		if (string.IsNullOrEmpty(plainText))
		{
			throw new ArgumentNullException("plainText");
		}
		using Aes aes = Aes.Create();
		aes.Padding = PaddingMode.PKCS7;
		aes.Key = DeriveKey(aes.KeySize / 8);
		aes.GenerateIV();
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(BitConverter.GetBytes(aes.IV.Length), 0, 4);
		memoryStream.Write(aes.IV, 0, aes.IV.Length);
		using (ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV))
		using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
		{
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(plainText);
		}
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	private static readonly object ErrorLogLock = new object();

	// Appends an unexpected error with its full stack trace to
	// %LOCALAPPDATA%\DoMping\error.log (kept under ~1 MB) so a failure that was
	// swallowed to keep the app alive can still be diagnosed afterwards.
	public static void WriteErrorLog(string context, Exception ex)
	{
		try
		{
			string dir = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\DoMping");
			System.IO.Directory.CreateDirectory(dir);
			string path = System.IO.Path.Combine(dir, "error.log");
			lock (ErrorLogLock)
			{
				if (System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length > 1024 * 1024)
				{
					System.IO.File.Delete(path);
				}
				System.IO.File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version} {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
			}
		}
		catch
		{
		}
	}

	public static string GetSafeFilename(string filename)
	{
		char[] separator = new char[9] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
		return string.Join("_", filename.Split(separator));
	}

	public static string DecryptStringAES(string cipherText)
	{
		if (string.IsNullOrEmpty(cipherText))
		{
			throw new ArgumentNullException("cipherText");
		}
		using Aes aes = Aes.Create();
		aes.Padding = PaddingMode.PKCS7;
		using MemoryStream stream = new MemoryStream(Convert.FromBase64String(cipherText));
		aes.Key = DeriveKey(aes.KeySize / 8);
		aes.IV = ReadByteArray(stream);
		using ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
		using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using StreamReader streamReader = new StreamReader(stream2);
		return streamReader.ReadToEnd();
	}

	private static byte[] ReadByteArray(Stream stream)
	{
		byte[] array = new byte[4];
		if (stream.Read(array, 0, array.Length) != array.Length)
		{
			throw new SystemException("Stream did not contain properly formatted byte array");
		}
		byte[] array2 = new byte[BitConverter.ToInt32(array, 0)];
		if (stream.Read(array2, 0, array2.Length) != array2.Length)
		{
			throw new SystemException("Did not read byte array properly");
		}
		return array2;
	}
}

using System;
using System.Collections.Generic;
using System.Xml;
using DoMping.Properties;

namespace DoMping.Classes;

internal class Alias
{
	public static Dictionary<string, string> GetAliases()
	{
		if (!Configuration.Exists())
		{
			return new Dictionary<string, string>();
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/aliases/alias"))
			{
				dictionary.Add(item.Attributes["host"].Value.ToLower(), item.InnerText);
			}
			return dictionary;
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_LoadAlias + " " + ex.Message);
			return new Dictionary<string, string>();
		}
	}

	public static void AddAlias(string hostname, string alias)
	{
		if (!Configuration.IsReady())
		{
			return;
		}
		hostname = hostname.ToLower();
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/domping/aliases");
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/aliases/alias[@host=" + Configuration.GetEscapedXpath(hostname) + "]"))
			{
				xmlNode.RemoveChild(item);
			}
			XmlElement xmlElement = xmlDocument.CreateElement("alias");
			xmlElement.SetAttribute("host", hostname);
			xmlElement.InnerText = alias;
			xmlNode.AppendChild(xmlElement);
			xmlDocument.Save(Configuration.FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_AddAlias + " " + ex.Message);
		}
	}

	public static void DeleteAlias(string key)
	{
		if (!Configuration.Exists())
		{
			return;
		}
		key = key.ToLower();
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/domping/aliases");
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/aliases/alias[@host=" + Configuration.GetEscapedXpath(key) + "]"))
			{
				xmlNode.RemoveChild(item);
			}
			xmlDocument.Save(Configuration.FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_DeleteAlias + " " + ex.Message);
		}
	}

	public static bool IsNameInvalid(string name)
	{
		return string.IsNullOrWhiteSpace(name);
	}

	public static bool IsHostInvalid(string hostname)
	{
		return string.IsNullOrWhiteSpace(hostname);
	}
}

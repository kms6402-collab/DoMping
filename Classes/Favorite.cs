using System;
using System.Collections.Generic;
using System.Xml;
using DoMping.Properties;

namespace DoMping.Classes;

internal class Favorite
{
	public List<string> Hostnames { get; set; }

	public int ColumnCount { get; set; }

	public static bool TitleExists(string title)
	{
		if (!Configuration.Exists())
		{
			return false;
		}
		bool result = false;
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			if (xmlDocument.SelectSingleNode("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(title) + "]") != null)
			{
				result = true;
			}
		}
		catch (Exception ex)
		{
			Util.ShowError("Failed to read configuration file. " + ex.Message);
			result = false;
		}
		return result;
	}

	public static List<string> GetTitles()
	{
		if (!Configuration.Exists())
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/favorites/favorite"))
			{
				list.Add(item.Attributes["title"].Value);
			}
		}
		catch (Exception ex)
		{
			Util.ShowError("Failed to read configuration file. " + ex.Message);
		}
		list.Sort();
		return list;
	}

	public static Favorite GetContents(string favoriteTitle)
	{
		if (!Configuration.Exists())
		{
			return new Favorite();
		}
		Favorite favorite = new Favorite();
		try
		{
			favorite.Hostnames = new List<string>();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(favoriteTitle) + "]");
			if (xmlNode == null)
			{
				throw new KeyNotFoundException();
			}
			favorite.ColumnCount = int.Parse(xmlNode.Attributes["columncount"].Value);
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(favoriteTitle) + "]/host"))
			{
				favorite.Hostnames.Add(item.InnerText);
			}
		}
		catch (KeyNotFoundException)
		{
			Util.ShowError("The requested favorite was not found: " + favoriteTitle);
		}
		catch (Exception ex2)
		{
			Util.ShowError(Strings.Error_ReadConfig + " " + ex2.Message);
		}
		return favorite;
	}

	public static bool IsTitleInvalid(string title)
	{
		return string.IsNullOrWhiteSpace(title);
	}

	public static void Rename(string originalTitle, string newTitle)
	{
		if (!Configuration.IsReady())
		{
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			xmlDocument.SelectSingleNode("/domping/favorites");
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(originalTitle) + "]"))
			{
				item.Attributes["title"].Value = newTitle;
			}
			xmlDocument.Save(Configuration.FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_WriteConfig + " " + ex.Message);
		}
	}

	public static void Save(string title, List<string> hostnames, int columnCount)
	{
		if (!Configuration.IsReady())
		{
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/domping/favorites");
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(title) + "]"))
			{
				xmlNode.RemoveChild(item);
			}
			XmlElement xmlElement = xmlDocument.CreateElement("favorite");
			xmlElement.SetAttribute("title", title);
			xmlElement.SetAttribute("columncount", columnCount.ToString());
			foreach (string hostname in hostnames)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("host");
				xmlElement2.InnerText = hostname;
				xmlElement.AppendChild(xmlElement2);
			}
			xmlNode.AppendChild(xmlElement);
			xmlDocument.Save(Configuration.FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_WriteConfig + " " + ex.Message);
		}
	}

	public static void Delete(string title)
	{
		if (!Configuration.Exists())
		{
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(Configuration.FilePath);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/domping/favorites");
			foreach (XmlNode item in xmlDocument.SelectNodes("/domping/favorites/favorite[@title=" + Configuration.GetEscapedXpath(title) + "]"))
			{
				xmlNode.RemoveChild(item);
			}
			xmlDocument.Save(Configuration.FilePath);
		}
		catch (Exception ex)
		{
			Util.ShowError(Strings.Error_WriteConfig + " " + ex.Message);
		}
	}
}

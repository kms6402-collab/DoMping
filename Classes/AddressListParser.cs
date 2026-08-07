using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DoMping.Classes;

public class ParsedAddress
{
	public string Host { get; set; }

	public string Alias { get; set; }
}

public static class AddressListParser
{
	public const int MaxExpandedPerEntry = 4096;

	public static List<ParsedAddress> Parse(IEnumerable<string> rawLines, out List<string> warnings)
	{
		warnings = new List<string>();
		List<ParsedAddress> result = new List<ParsedAddress>();
		Dictionary<string, ParsedAddress> seen = new Dictionary<string, ParsedAddress>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawLine in rawLines)
		{
			string line = rawLine?.Trim();
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}
			if (line.StartsWith("D/") || line.StartsWith("T/") || line.StartsWith("S/"))
			{
				string prefix = line.Substring(0, 2);
				string body = line.Substring(2);
				SplitHostAndAlias(body, out string prefixedHost, out string prefixedAlias);
				AddUnique(result, seen, new ParsedAddress
				{
					Host = prefix + prefixedHost,
					Alias = prefixedAlias
				}, warnings);
				continue;
			}
			if (TryParseRange(line, out List<string> rangeIps, out string rangeError))
			{
				if (rangeError != null)
				{
					warnings.Add(rangeError);
					continue;
				}
				foreach (string ip in rangeIps)
				{
					AddUnique(result, seen, new ParsedAddress
					{
						Host = ip,
						Alias = null
					}, warnings);
				}
				continue;
			}
			int slashIdx = line.IndexOf('/');
			if (slashIdx > 0)
			{
				string left = line.Substring(0, slashIdx).Trim();
				string right = line.Substring(slashIdx + 1).Trim();
				if (int.TryParse(right, out int prefixLen) && prefixLen >= 0 && prefixLen <= 32 && IPAddress.TryParse(left, out IPAddress baseIp) && baseIp.AddressFamily == AddressFamily.InterNetwork)
				{
					if (TryExpandCidr(left, prefixLen, out List<string> cidrIps, out string cidrError))
					{
						if (cidrError != null)
						{
							warnings.Add(cidrError);
							continue;
						}
						foreach (string ip2 in cidrIps)
						{
							AddUnique(result, seen, new ParsedAddress
							{
								Host = ip2,
								Alias = null
							}, warnings);
						}
						continue;
					}
				}
				else if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
				{
					AddUnique(result, seen, new ParsedAddress
					{
						Host = left,
						Alias = right
					}, warnings);
					continue;
				}
			}
			SplitHostAndAlias(line, out string plainHost, out string plainAlias);
			AddUnique(result, seen, new ParsedAddress
			{
				Host = plainHost,
				Alias = plainAlias
			}, warnings);
		}
		return result;
	}

	private static void SplitHostAndAlias(string text, out string host, out string alias)
	{
		int spaceIdx = text.IndexOfAny(new char[] { ' ', '\t' });
		if (spaceIdx > 0)
		{
			string left = text.Substring(0, spaceIdx).Trim();
			string right = text.Substring(spaceIdx + 1).Trim();
			if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
			{
				host = left;
				alias = right;
				return;
			}
		}
		host = text.Trim();
		alias = null;
	}

	private static void AddUnique(List<ParsedAddress> result, Dictionary<string, ParsedAddress> seen, ParsedAddress addr, List<string> warnings)
	{
		if (seen.TryGetValue(addr.Host, out ParsedAddress existing))
		{
			if (string.IsNullOrEmpty(existing.Alias) && !string.IsNullOrEmpty(addr.Alias))
			{
				existing.Alias = addr.Alias;
				warnings.Add($"Duplicate host merged (alias applied to the existing entry): {addr.Host}");
			}
			else
			{
				warnings.Add($"Duplicate host skipped (already in the list): {addr.Host}");
			}
			return;
		}
		seen[addr.Host] = addr;
		result.Add(addr);
	}

	private static bool TryParseRange(string line, out List<string> ips, out string error)
	{
		ips = null;
		error = null;
		int dashIdx = line.IndexOf('-');
		if (dashIdx <= 0 || dashIdx == line.Length - 1)
		{
			return false;
		}
		string left = line.Substring(0, dashIdx).Trim();
		string right = line.Substring(dashIdx + 1).Trim();
		if (!IPAddress.TryParse(left, out IPAddress startIp) || startIp.AddressFamily != AddressFamily.InterNetwork)
		{
			return false;
		}
		byte[] startBytes = startIp.GetAddressBytes();
		byte[] endBytes;
		if (!right.Contains('.') && byte.TryParse(right, out byte lastOctet))
		{
			endBytes = (byte[])startBytes.Clone();
			endBytes[3] = lastOctet;
		}
		else if (right.Contains('.') && IPAddress.TryParse(right, out IPAddress endIpFull) && endIpFull.AddressFamily == AddressFamily.InterNetwork)
		{
			endBytes = endIpFull.GetAddressBytes();
		}
		else
		{
			return false;
		}
		uint startVal = ToUInt(startBytes);
		uint endVal = ToUInt(endBytes);
		if (endVal < startVal)
		{
			error = $"Invalid range (end is before start): {line}";
			ips = new List<string>();
			return true;
		}
		ulong count = endVal - startVal + 1;
		if (count > MaxExpandedPerEntry)
		{
			error = $"Range too large ({count:N0} addresses): {line} — the limit is {MaxExpandedPerEntry:N0} per entry.";
			ips = new List<string>();
			return true;
		}
		ips = new List<string>();
		for (uint v = startVal; ; v++)
		{
			ips.Add(FromUInt(v).ToString());
			if (v == endVal)
			{
				break;
			}
		}
		return true;
	}

	private static bool TryExpandCidr(string baseIpStr, int prefixLen, out List<string> ips, out string error)
	{
		ips = new List<string>();
		error = null;
		IPAddress baseIp = IPAddress.Parse(baseIpStr);
		uint baseVal = ToUInt(baseIp.GetAddressBytes());
		int hostBits = 32 - prefixLen;
		ulong totalAddresses = (hostBits >= 32) ? 4294967296uL : ((ulong)1 << hostBits);
		uint mask = (hostBits == 0) ? uint.MaxValue : ~(uint)((1uL << hostBits) - 1);
		uint network = baseVal & mask;
		bool excludeNetworkAndBroadcast = prefixLen < 31;
		ulong usableCount = excludeNetworkAndBroadcast ? Math.Max(totalAddresses - 2, 0) : totalAddresses;
		if (usableCount > MaxExpandedPerEntry)
		{
			error = $"Subnet too large ({usableCount:N0} addresses): {baseIpStr}/{prefixLen} — the limit is {MaxExpandedPerEntry:N0} per entry. Try a smaller subnet (/20 or higher).";
			return true;
		}
		if (!excludeNetworkAndBroadcast)
		{
			for (ulong i = 0; i < totalAddresses; i++)
			{
				ips.Add(FromUInt(network + (uint)i).ToString());
			}
		}
		else
		{
			for (uint i = 1; i < (uint)(totalAddresses - 1); i++)
			{
				ips.Add(FromUInt(network + i).ToString());
			}
		}
		return true;
	}

	private static uint ToUInt(byte[] bytes)
	{
		return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
	}

	private static IPAddress FromUInt(uint value)
	{
		byte[] array = new byte[4]
		{
			(byte)(value >> 24),
			(byte)(value >> 16),
			(byte)(value >> 8),
			(byte)value
		};
		return new IPAddress(array);
	}
}

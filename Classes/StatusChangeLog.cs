using System;
using DoMping.Properties;

namespace DoMping.Classes;

public class StatusChangeLog
{
	public DateTime Timestamp { get; set; }

	public string Hostname { get; set; }

	public string Alias { get; set; }

	public ProbeStatus Status { get; set; }

	public bool HasStatusBeenCleared { get; set; }

	public string AliasIfExistOrHostname
	{
		get
		{
			if (Alias == null || Alias.Length <= 0)
			{
				return Hostname;
			}
			return Alias;
		}
	}

	public string StatusAsString => Status switch
	{
		ProbeStatus.Down => Strings.StatusChange_Down, 
		ProbeStatus.Up => Strings.StatusChange_Up, 
		ProbeStatus.Error => Strings.StatusChange_Error, 
		ProbeStatus.Start => Strings.StatusChange_Start, 
		ProbeStatus.Stop => Strings.StatusChange_Stop, 
		ProbeStatus.LatencyHigh => Strings.StatusChange_LatencyHigh, 
		ProbeStatus.LatencyNormal => Strings.StatusChange_LatencyNormal, 
		_ => string.Empty, 
	};

	public string StatusAsGlyph
	{
		get
		{
			switch (Status)
			{
			case ProbeStatus.Down:
			case ProbeStatus.Error:
				return "u";
			case ProbeStatus.Up:
				return "t";
			default:
				return "h";
			}
		}
	}
}

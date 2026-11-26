using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DoMping.Classes;

public sealed class UpdateInfo
{
	public Version Version { get; init; }

	public string TagName { get; init; }

	public string AssetName { get; init; }

	public string DownloadUrl { get; init; }

	public long Size { get; init; }

	// "sha256:<hex>" as published by the GitHub releases API, or null if not provided.
	public string Digest { get; init; }

	public string ReleasePageUrl { get; init; }
}

// Checks the GitHub "latest release" of the repository for a newer DoMping build and,
// on request, downloads it and swaps it in for the running exe.
public static class UpdateService
{
	public const string RepositoryOwner = "kms6402-collab";

	public const string RepositoryName = "DoMping";

	public static readonly string ReleasesPageUrl = $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases/latest";

	private static readonly string LatestReleaseApiUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";

	private static readonly HttpClient Http = CreateHttpClient();

	private static HttpClient CreateHttpClient()
	{
		HttpClient client = new HttpClient
		{
			Timeout = TimeSpan.FromMinutes(5)
		};
		// The GitHub API rejects requests without a User-Agent.
		client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DoMping-Updater", CurrentVersion.ToString(3)));
		return client;
	}

	public static Version CurrentVersion
	{
		get
		{
			Version v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
			return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
		}
	}

	// Returns the newer release, or null when we are up to date (or the check failed:
	// no network, rate limited, ... - none of which should bother the user).
	public static async Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeout.CancelAfter(TimeSpan.FromSeconds(15));
			using HttpResponseMessage response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			string json = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
			return ParseRelease(json);
		}
		catch (Exception ex)
		{
			Trace.WriteLine("Update check failed: " + ex.Message);
			return null;
		}
	}

	private static UpdateInfo ParseRelease(string json)
	{
		using JsonDocument doc = JsonDocument.Parse(json);
		JsonElement root = doc.RootElement;
		if (root.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True)
		{
			return null;
		}
		if (root.TryGetProperty("prerelease", out JsonElement pre) && pre.ValueKind == JsonValueKind.True)
		{
			return null;
		}
		string tag = root.TryGetProperty("tag_name", out JsonElement tagEl) ? tagEl.GetString() : null;
		if (!TryParseVersion(tag, out Version latest) || latest <= CurrentVersion)
		{
			return null;
		}
		if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
		{
			return null;
		}
		// Prefer the asset that follows our own naming (DoMping_v1.2.3.exe), else any .exe.
		JsonElement[] exeAssets = assets.EnumerateArray().Where((JsonElement a) => a.TryGetProperty("name", out JsonElement n) && (n.GetString() ?? string.Empty).EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).ToArray();
		if (exeAssets.Length == 0)
		{
			return null;
		}
		JsonElement asset = exeAssets.FirstOrDefault((JsonElement a) => a.GetProperty("name").GetString().StartsWith("DoMping", StringComparison.OrdinalIgnoreCase));
		if (asset.ValueKind == JsonValueKind.Undefined)
		{
			asset = exeAssets[0];
		}
		string assetName = asset.GetProperty("name").GetString();
		// Never let a crafted asset name climb out of our folder.
		if (string.IsNullOrWhiteSpace(assetName) || assetName != Path.GetFileName(assetName) || assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			return null;
		}
		string url = asset.TryGetProperty("browser_download_url", out JsonElement u) ? u.GetString() : null;
		if (string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return new UpdateInfo
		{
			Version = latest,
			TagName = tag,
			AssetName = assetName,
			DownloadUrl = url,
			Size = asset.TryGetProperty("size", out JsonElement size) && size.TryGetInt64(out long sz) ? sz : 0,
			Digest = asset.TryGetProperty("digest", out JsonElement dg) && dg.ValueKind == JsonValueKind.String ? dg.GetString() : null,
			ReleasePageUrl = root.TryGetProperty("html_url", out JsonElement html) ? html.GetString() : ReleasesPageUrl
		};
	}

	public static bool TryParseVersion(string tag, out Version version)
	{
		version = null;
		if (string.IsNullOrWhiteSpace(tag))
		{
			return false;
		}
		string text = tag.Trim().TrimStart('v', 'V');
		int cut = text.IndexOfAny(new char[] { '-', '+', ' ' });
		if (cut > 0)
		{
			text = text.Substring(0, cut);
		}
		if (!Version.TryParse(text, out Version parsed))
		{
			return false;
		}
		version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build));
		return true;
	}

	// Downloads the release next to the running exe and verifies it. Returns the path of
	// the downloaded (still temporary-named) file.
	public static async Task<string> DownloadAsync(UpdateInfo info, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		string exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
		string tempPath = Path.Combine(exeDir, info.AssetName + ".download");
		try
		{
			using (HttpResponseMessage response = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();
				long total = response.Content.Headers.ContentLength ?? info.Size;
				await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
				await using FileStream target = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
				byte[] buffer = new byte[81920];
				long received = 0;
				int read;
				while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
				{
					await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
					received += read;
					if (total > 0)
					{
						progress?.Report(Math.Min(1.0, (double)received / total));
					}
				}
			}
			FileInfo file = new FileInfo(tempPath);
			if (info.Size > 0 && file.Length != info.Size)
			{
				throw new InvalidDataException($"Downloaded file size {file.Length:N0} does not match the release ({info.Size:N0} bytes).");
			}
			if (!string.IsNullOrEmpty(info.Digest) && info.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
			{
				string expected = info.Digest.Substring("sha256:".Length);
				string actual;
				using (FileStream fs = File.OpenRead(tempPath))
				{
					actual = Convert.ToHexString(await SHA256.HashDataAsync(fs, cancellationToken).ConfigureAwait(false));
				}
				if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidDataException("Downloaded file failed the SHA-256 check.");
				}
			}
			return tempPath;
		}
		catch
		{
			try
			{
				File.Delete(tempPath);
			}
			catch
			{
			}
			throw;
		}
	}

	// Starts a hidden helper that waits for this process to exit, moves the downloaded
	// exe into place, removes the old exe and launches the new one. The caller must shut
	// the application down right after this returns.
	public static void InstallAndRestart(UpdateInfo info, string downloadedPath)
	{
		string currentExe = Environment.ProcessPath;
		string exeDir = Path.GetDirectoryName(currentExe);
		string newExe = Path.Combine(exeDir, info.AssetName);
		int pid = Environment.ProcessId;
		static string Q(string s) => "'" + s.Replace("'", "''") + "'";
		StringBuilder script = new StringBuilder();
		script.AppendLine("$ErrorActionPreference = 'SilentlyContinue'");
		script.AppendLine($"Wait-Process -Id {pid} -Timeout 60");
		script.AppendLine("Start-Sleep -Milliseconds 300");
		// Retry: antivirus scanners or a slow shutdown can hold the files briefly.
		script.AppendLine("for ($i = 0; $i -lt 20; $i++) {");
		script.AppendLine($"  Move-Item -LiteralPath {Q(downloadedPath)} -Destination {Q(newExe)} -Force");
		script.AppendLine($"  if (-not (Test-Path -LiteralPath {Q(downloadedPath)})) {{ break }}");
		script.AppendLine("  Start-Sleep -Milliseconds 500");
		script.AppendLine("}");
		if (!string.Equals(currentExe, newExe, StringComparison.OrdinalIgnoreCase))
		{
			// The old version's exe (e.g. DoMping_v1.14.0.exe) is replaced by the new one.
			// Another DoMping instance still running from it keeps it locked; then it stays.
			script.AppendLine($"if (Test-Path -LiteralPath {Q(newExe)}) {{");
			script.AppendLine("  for ($i = 0; $i -lt 10; $i++) {");
			script.AppendLine($"    Remove-Item -LiteralPath {Q(currentExe)} -Force");
			script.AppendLine($"    if (-not (Test-Path -LiteralPath {Q(currentExe)})) {{ break }}");
			script.AppendLine("    Start-Sleep -Milliseconds 500");
			script.AppendLine("  }");
			script.AppendLine("}");
		}
		script.AppendLine($"if (Test-Path -LiteralPath {Q(newExe)}) {{ Start-Process -FilePath {Q(newExe)} -WorkingDirectory {Q(exeDir)} }}");
		script.AppendLine($"else {{ Start-Process -FilePath {Q(currentExe)} -WorkingDirectory {Q(exeDir)} }}");
		// -EncodedCommand (UTF-16LE base64) keeps non-ASCII folder names (e.g. Korean) intact.
		string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script.ToString()));
		ProcessStartInfo psi = new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encoded,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = exeDir
		};
		Process.Start(psi);
	}
}

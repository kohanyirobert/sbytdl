using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Sbytdl.Api.Models;

namespace Sbytdl.Api.Services;

public class DownloadService
{
    private readonly ConcurrentDictionary<string, DownloadJobStatus> _jobs = new();
    private readonly YtDlpService _ytDlpService;
    private static readonly Regex ProgressRegex = new(@"\[download\]\s+(?<pct>\d+(\.\d+)?)%", RegexOptions.Compiled);

    public DownloadService(YtDlpService ytDlpService)
    {
        _ytDlpService = ytDlpService;
    }

    public DownloadPreviewResponse Preview(DownloadRequest request)
    {
        var targets = ResolveTargetPaths(request);
        var existing = targets.Where(File.Exists).ToList();
        return new DownloadPreviewResponse(targets, existing);
    }

    public string Start(DownloadRequest request)
    {
        var id = Guid.NewGuid().ToString("N");
        var status = new DownloadJobStatus(id, "queued", 0, "Queued", DateTimeOffset.UtcNow, null);
        _jobs[id] = status;

        _ = Task.Run(async () => await RunDownloadAsync(request, id));
        return id;
    }

    public DownloadJobStatus? GetStatus(string jobId) => _jobs.TryGetValue(jobId, out var status) ? status : null;

    private async Task RunDownloadAsync(DownloadRequest request, string jobId)
    {
        try
        {
            if (request.CreateDirectory && !Directory.Exists(request.OutputDirectory))
            {
                Directory.CreateDirectory(request.OutputDirectory);
            }

            if (!Directory.Exists(request.OutputDirectory))
            {
                _jobs[jobId] = _jobs[jobId] with { State = "failed", Message = "Output directory does not exist", FinishedAt = DateTimeOffset.UtcNow };
                return;
            }

            var args = BuildArgs(request);
            var psi = new ProcessStartInfo("yt-dlp", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            _jobs[jobId] = _jobs[jobId] with { State = "running", Message = "Download started" };

            while (!process.HasExited)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line is null) continue;

                var match = ProgressRegex.Match(line);
                if (match.Success && double.TryParse(match.Groups["pct"].Value, out var pct))
                {
                    _jobs[jobId] = _jobs[jobId] with { ProgressPercent = pct, Message = line };
                }
                else
                {
                    _jobs[jobId] = _jobs[jobId] with { Message = line };
                }
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            if (process.ExitCode == 0)
            {
                _jobs[jobId] = _jobs[jobId] with { State = "completed", ProgressPercent = 100, Message = "Download completed", FinishedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                _jobs[jobId] = _jobs[jobId] with { State = "failed", Message = string.IsNullOrWhiteSpace(stderr) ? "yt-dlp failed" : stderr, FinishedAt = DateTimeOffset.UtcNow };
            }
        }
        catch (Exception ex)
        {
            _jobs[jobId] = _jobs[jobId] with { State = "failed", Message = ex.Message, FinishedAt = DateTimeOffset.UtcNow };
        }
    }

    private static string BuildArgs(DownloadRequest request)
    {
        var outputTemplate = ResolveOutputTemplate(request);
        var overwriteArg = request.OverwriteExisting ? "--force-overwrites" : "--no-overwrites";
        var urls = string.Join(" ", request.Urls.Select(url => $"\"{url}\""));
        return $"--newline {overwriteArg} -o \"{outputTemplate}\" {urls}";
    }

    private static List<string> ResolveTargetPaths(DownloadRequest request)
    {
        return request.Urls.Select((_, index) =>
        {
            var fileToken = request.Urls.Count == 1 && !string.IsNullOrWhiteSpace(request.FileName)
                ? request.FileName!
                : string.IsNullOrWhiteSpace(request.NameTemplate)
                    ? $"download-{index + 1}"
                    : request.NameTemplate!.Replace("{index}", (index + 1).ToString("D2"));

            return Path.Combine(request.OutputDirectory, fileToken + ".%(ext)s");
        }).ToList();
    }

    private static string ResolveOutputTemplate(DownloadRequest request)
    {
        if (request.Urls.Count == 1 && !string.IsNullOrWhiteSpace(request.FileName))
        {
            return Path.Combine(request.OutputDirectory, request.FileName + ".%(ext)s");
        }

        var template = string.IsNullOrWhiteSpace(request.NameTemplate)
            ? "%(title)s"
            : request.NameTemplate.Replace("{index}", "%(autonumber)02d");

        return Path.Combine(request.OutputDirectory, template + ".%(ext)s");
    }
}

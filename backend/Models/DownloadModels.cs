namespace Sbytdl.Api.Models;

public record DownloadRequest(
    List<string> Urls,
    string OutputDirectory,
    bool CreateDirectory,
    string? FileName,
    string? NameTemplate,
    bool OverwriteExisting = false);

public record DownloadPreviewResponse(List<string> ResolvedTargets, List<string> ExistingTargets);

public record StartDownloadResponse(string JobId);

public record DownloadJobStatus(
    string JobId,
    string State,
    double ProgressPercent,
    string Message,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);

public record UpdateYtDlpResponse(bool Success, string Message);

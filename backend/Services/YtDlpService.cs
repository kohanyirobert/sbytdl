using System.Diagnostics;
using Sbytdl.Api.Models;

namespace Sbytdl.Api.Services;

public class YtDlpService
{
    public async Task<UpdateYtDlpResponse> UpdateAsync()
    {
        try
        {
            var psi = new ProcessStartInfo("yt-dlp", "-U")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return new UpdateYtDlpResponse(true, stdout.Trim());
            }

            return new UpdateYtDlpResponse(false, string.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim());
        }
        catch (Exception ex)
        {
            return new UpdateYtDlpResponse(false, ex.Message);
        }
    }
}

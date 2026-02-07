using Microsoft.AspNetCore.Mvc;
using Sbytdl.Api.Models;
using Sbytdl.Api.Services;

namespace Sbytdl.Api;

[ApiController]
[Route("api/downloads")]
public class DownloadsController : ControllerBase
{
    private readonly DownloadService _downloadService;

    public DownloadsController(DownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    [HttpPost("preview")]
    public ActionResult<DownloadPreviewResponse> Preview([FromBody] DownloadRequest request)
    {
        if (request.Urls.Count == 0) return BadRequest("At least one URL is required");
        return Ok(_downloadService.Preview(request));
    }

    [HttpPost("start")]
    public ActionResult<StartDownloadResponse> Start([FromBody] DownloadRequest request)
    {
        if (request.Urls.Count == 0) return BadRequest("At least one URL is required");
        var jobId = _downloadService.Start(request);
        return Ok(new StartDownloadResponse(jobId));
    }

    [HttpGet("{jobId}")]
    public ActionResult<DownloadJobStatus> Status([FromRoute] string jobId)
    {
        var status = _downloadService.GetStatus(jobId);
        if (status is null) return NotFound();
        return Ok(status);
    }
}

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly YtDlpService _ytDlpService;

    public SystemController(YtDlpService ytDlpService)
    {
        _ytDlpService = ytDlpService;
    }

    [HttpPost("update-ytdlp")]
    public async Task<ActionResult<UpdateYtDlpResponse>> UpdateYtDlp()
    {
        var result = await _ytDlpService.UpdateAsync();
        if (!result.Success) return StatusCode(500, result);
        return Ok(result);
    }
}

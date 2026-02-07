using Sbytdl.Api.Models;
using Sbytdl.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DownloadService>();
builder.Services.AddSingleton<YtDlpService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();

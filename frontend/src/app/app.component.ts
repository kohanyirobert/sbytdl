import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnDestroy {
  apiBase = 'http://localhost:5000/api';
  urlsText = '';
  outputDirectory = '';
  createDirectory = true;
  fileName = '';
  nameTemplate = '';
  overwriteExisting = false;

  conflictMessage = '';
  latestMessage = '';
  progress = 0;
  status = 'idle';
  private pollHandle?: number;

  constructor(private http: HttpClient) {}

  ngOnDestroy(): void {
    if (this.pollHandle) window.clearInterval(this.pollHandle);
  }

  preview(): void {
    this.http.post<any>(`${this.apiBase}/downloads/preview`, this.buildRequest()).subscribe({
      next: result => {
        if (result.existingTargets.length > 0) {
          this.conflictMessage = `⚠️ ${result.existingTargets.length} file(s) already exist. Enable overwrite only if you really want to replace them.`;
        } else {
          this.conflictMessage = '✅ No overwrite conflicts detected.';
        }
      },
      error: err => this.conflictMessage = err?.error || 'Preview failed'
    });
  }

  startDownload(): void {
    this.status = 'starting';
    this.progress = 0;
    this.http.post<any>(`${this.apiBase}/downloads/start`, this.buildRequest()).subscribe({
      next: result => this.poll(result.jobId),
      error: err => {
        this.status = 'failed';
        this.latestMessage = err?.error || 'Failed to start download';
      }
    });
  }

  updateYtdlp(): void {
    this.latestMessage = 'Updating yt-dlp...';
    this.http.post<any>(`${this.apiBase}/system/update-ytdlp`, {}).subscribe({
      next: result => this.latestMessage = result.message,
      error: err => this.latestMessage = err?.error?.message || 'yt-dlp update failed'
    });
  }

  private poll(jobId: string): void {
    if (this.pollHandle) window.clearInterval(this.pollHandle);

    this.pollHandle = window.setInterval(() => {
      this.http.get<any>(`${this.apiBase}/downloads/${jobId}`).subscribe(result => {
        this.progress = result.progressPercent ?? 0;
        this.status = result.state;
        this.latestMessage = result.message;
        if (['completed', 'failed'].includes(result.state)) {
          if (this.pollHandle) window.clearInterval(this.pollHandle);
        }
      });
    }, 1000);
  }

  private buildRequest() {
    return {
      urls: this.urlsText.split(/\r?\n/).map(url => url.trim()).filter(Boolean),
      outputDirectory: this.outputDirectory,
      createDirectory: this.createDirectory,
      fileName: this.fileName || null,
      nameTemplate: this.nameTemplate || null,
      overwriteExisting: this.overwriteExisting
    };
  }
}

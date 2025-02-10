using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RAGPrompterWebApp.Data.Interfaces;
using System.IO.Compression;
using System.Net;

namespace RAGPrompterWebApp.Data.Services
{
    public class FileService : IFileService
    {
        private readonly IJSRuntime _jsRuntime;
        private const int BufferSize = 4096 * 1024; // 4MB buffer for good performance

        public FileService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task<string> GetBaseUrl()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("eval", "window.settings.apiBaseUrl");
            }
            catch
            {
                return "http://localhost:8000"; // Fallback
            }
        }

        public async Task<(bool success, string error)> UploadFilesAsync(
            string projectName,
            IReadOnlyList<IBrowserFile> files,
            IProgress<(int filesProcessed, int totalFiles, long bytesUploaded, long totalBytes, bool isZipping)> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                var tempZipPath = Path.GetTempFileName();
                try
                {
                    // Calculate total size
                    long totalBytes = files.Sum(f => f.Size);
                    progress.Report((0, files.Count, 0, totalBytes, true));

                    // Create ZIP file
                    using (var zipStream = new FileStream(tempZipPath, FileMode.Create))
                    using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        for (int i = 0; i < files.Count; i++)
                        {
                            if (cancellationToken.IsCancellationRequested) return (false, "Operation cancelled");

                            var file = files[i];
                            var entry = archive.CreateEntry(file.Name, System.IO.Compression.CompressionLevel.Fastest);

                            using var entryStream = entry.Open();
                            using var fileStream = file.OpenReadStream(maxAllowedSize: 5L * 1024 * 1024 * 1024);

                            // Stream the file in chunks
                            var buffer = new byte[BufferSize];
                            int bytesRead;
                            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                            {
                                await entryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                            }

                            progress.Report((i + 1, files.Count, 0, totalBytes, true));
                        }
                    }

                    // Get final ZIP size and prepare for upload
                    var fileInfo = new FileInfo(tempZipPath);
                    totalBytes = fileInfo.Length;
                    progress.Report((files.Count, files.Count, 0, totalBytes, false));

                    // Upload the ZIP
                    using var zipContent = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read);
                    using var multipartContent = new MultipartFormDataContent();

                    var progressContent = new ProgressStreamContent(zipContent, totalBytes, (uploaded, total) =>
                    {
                        progress.Report((files.Count, files.Count, uploaded, total, false));
                    });

                    multipartContent.Add(progressContent, "archive", "files.zip");

                    using var httpClient = new HttpClient();
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;

                    var baseUrl = await GetBaseUrl();
                    var response = await httpClient.PostAsync(
                        $"{baseUrl}/files/{projectName}/upload",
                        multipartContent,
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, $"Upload failed with status code: {response.StatusCode}");
                    }

                    return (true, string.Empty);
                }
                finally
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private class ProgressStreamContent : StreamContent
        {
            private readonly Stream _stream;
            private readonly Action<long, long> _onProgress;
            private readonly long _totalBytes;

            public ProgressStreamContent(Stream stream, long totalBytes, Action<long, long> onProgress)
                : base(stream)
            {
                _stream = stream;
                _totalBytes = totalBytes;
                _onProgress = onProgress;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                var buffer = new byte[BufferSize];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await _stream.ReadAsync(buffer)) != 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;
                    _onProgress(totalBytesRead, _totalBytes);
                }
            }
        }
    }
}

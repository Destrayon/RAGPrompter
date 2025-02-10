using Microsoft.AspNetCore.Components.Forms;

namespace RAGPrompterWebApp.Data.Interfaces
{
    public interface IFileService
    {
        Task<(bool success, string error)> UploadFilesAsync(
            string projectName,
            IReadOnlyList<IBrowserFile> files,
            IProgress<(int filesProcessed, int totalFiles, long bytesUploaded, long totalBytes, bool isZipping)> progress,
            CancellationToken cancellationToken);
    }
}

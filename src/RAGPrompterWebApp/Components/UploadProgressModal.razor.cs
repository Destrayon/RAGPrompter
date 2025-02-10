using Microsoft.AspNetCore.Components;

namespace RAGPrompterWebApp.Components
{
    public partial class UploadProgressModal
    {
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public bool IsProcessing { get; set; }
        [Parameter] public bool IsZipping { get; set; }
        [Parameter] public bool IsUploading { get; set; }
        [Parameter] public int TotalFiles { get; set; }
        [Parameter] public int ProcessedFiles { get; set; }
        [Parameter] public long TotalBytes { get; set; }
        [Parameter] public long UploadedBytes { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        private async Task HandleCancel()
        {
            await OnCancel.InvokeAsync();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
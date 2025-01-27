using Microsoft.AspNetCore.Components;

namespace RAGPrompterWebApp.Components
{
    public partial class UploadProgressModal
    {
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public bool IsProcessing { get; set; }
        [Parameter] public int TotalFiles { get; set; }
        [Parameter] public int ProcessedFiles { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        private async Task HandleCancel()
        {
            await OnCancel.InvokeAsync();
        }
    }
}
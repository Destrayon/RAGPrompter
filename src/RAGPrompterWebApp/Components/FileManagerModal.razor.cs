using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace RAGPrompterWebApp.Components
{
    public partial class FileManagerModal
    {
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public List<IBrowserFile> Files { get; set; } = new();

        private async Task HandleClose()
        {
            await OnClose.InvokeAsync();
        }
    }
}
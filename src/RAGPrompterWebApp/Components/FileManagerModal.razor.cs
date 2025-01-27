using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace RAGPrompterWebApp.Components
{
    public partial class FileManagerModal
    {
        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public List<IBrowserFile> Files { get; set; } = new();
        [Parameter] public EventCallback<int> OnDeleteFile { get; set; }
        [Parameter] public EventCallback OnClearAll { get; set; }

        private int PageSize = 10;
        private int CurrentPage = 1;
        private int TotalPages => (int)Math.Ceiling(Files.Count / (double)PageSize);

        private IEnumerable<IBrowserFile> CurrentPageFiles => Files
        .Skip((CurrentPage - 1) * PageSize)
        .Take(PageSize);

        private async Task HandleClose()
        {
            await OnClose.InvokeAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("addEventListener", "keydown", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public async Task HandleGlobalKeyPress(string key)
        {
            if (key == "Escape" && IsOpen)
            {
                await HandleClose();
                StateHasChanged();
            }
        }
        private async Task HandleDelete(int index)
        {
            await OnDeleteFile.InvokeAsync(index);
            if (CurrentPageFiles.Count() == 0 && CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private async Task HandleClearAll()
        {
            await OnClearAll.InvokeAsync();
        }

        private void ChangePage(int page)
        {
            CurrentPage = Math.Max(1, Math.Min(page, TotalPages));
        }
    }
}
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace RAGPrompterWebApp.Pages
{
    public partial class Home
    {
        private ElementReference dropdownRef;
        private List<string> projects = new() { "Default" };
        private string selectedProject = "Default";
        private bool showDropdown;
        private List<IBrowserFile> files = new();
        private string question = "";
        private string generatedPrompt = "";
        private bool showFileManager;
        private bool showNewProjectInput = false;
        private string newProjectName = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("dropdownClickHandler.initialize", dropdownRef);
            }
        }

        [JSInvokable]
        public static Task CloseDropdown()
        {
            OnDropdownClose?.Invoke();
            return Task.CompletedTask;
        }

        private static event Action? OnDropdownClose;

        protected override void OnInitialized()
        {
            OnDropdownClose += HandleDropdownClose;
        }

        private void HandleDropdownClose()
        {
            showDropdown = false;
            StateHasChanged();
        }

        public void Dispose()
        {
            OnDropdownClose -= HandleDropdownClose;
        }

        private void ShowNewProjectInput()
        {
            showNewProjectInput = true;
            StateHasChanged();
        }

        private void ShowFileManager()
        {
            showFileManager = true;
        }

        private void ToggleDropdown()
        {
            showDropdown = !showDropdown;
        }

        private void SelectProject(string project)
        {
            selectedProject = project;
            showDropdown = false;
        }

        private void DeleteProject(string project)
        {
            projects.Remove(project);
            if (selectedProject == project)
            {
                selectedProject = projects[0];
            }
            showDropdown = false;
        }

        private void AddNewProject()
        {
            if (!string.IsNullOrWhiteSpace(newProjectName) && !projects.Contains(newProjectName))
            {
                projects.Add(newProjectName);
                selectedProject = newProjectName;
                CancelNewProject();
            }
        }

        private void CancelNewProject()
        {
            newProjectName = "";
            showNewProjectInput = false;
            StateHasChanged();
        }

        private void HandleNewProjectKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                AddNewProject();
            }
            else if (e.Key == "Escape")
            {
                CancelNewProject();
            }
        }

        private async Task UploadFiles(InputFileChangeEventArgs e)
        {
            // Implement file upload logic
        }

        private async Task UploadFolders(InputFileChangeEventArgs e)
        {
            // Implement folder upload logic
        }

        private void GeneratePrompt()
        {
            generatedPrompt = question;
        }

        private async Task CopyToClipboard()
        {
            await JS.InvokeVoidAsync("clipboardCopy.copyText", generatedPrompt);
        }
    }
}
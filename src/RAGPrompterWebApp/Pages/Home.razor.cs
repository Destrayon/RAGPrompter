using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace RAGPrompterWebApp.Pages
{
    public partial class Home
    {
        private const int maxFileAmount = 100000;
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
        private CancellationTokenSource? uploadCts;
        private bool isUploading = false;
        private int totalFiles = 0;
        private int processedFiles = 0;

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

        private async Task ProcessFiles(InputFileChangeEventArgs e, bool isFolder = false)
        {
                isUploading = true;
            StateHasChanged();

            uploadCts = new CancellationTokenSource();



            try
            {
                await Task.Run(async () => {
                    var newFiles = e.GetMultipleFiles(maxFileAmount);
                    totalFiles = newFiles.Count;
                    processedFiles = 0;
                    await InvokeAsync(StateHasChanged);

                    foreach (var file in newFiles)
                    {
                        if (uploadCts.Token.IsCancellationRequested) break;

                        string fileName = isFolder ? file.Name.Split('/').Last() : file.Name;

                        if (!files.Any(f => f.Name == fileName && f.Size == file.Size))
                        {
                            files.Add(file);
                            processedFiles++;

                            if (processedFiles % 10 == 0)
                            {
                                await InvokeAsync(StateHasChanged);
                                await Task.Delay(1);
                            }
                        }
                    }
                }, uploadCts.Token);
            }
            catch (OperationCanceledException)
            {
                files.Clear();
            }
            finally
            {
                isUploading = false;
                uploadCts.Dispose();
                uploadCts = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task UploadFiles(InputFileChangeEventArgs e) =>
            await ProcessFiles(e);

        private async Task UploadFolders(InputFileChangeEventArgs e) =>
            await ProcessFiles(e, true);

        private void CancelUpload()
        {
            uploadCts?.Cancel();
        }

        private void HandleClearAll()
        {
            files.Clear();
            StateHasChanged();
        }

        private void HandleDeleteFile(int index)
        {
            if (index >= 0 && index < files.Count)
            {
                files.RemoveAt(index);
                StateHasChanged();
            }
        }

        private void GeneratePrompt()
        {
            generatedPrompt = question;
            question = "";
        }

        private async Task CopyToClipboard()
        {
            await JS.InvokeVoidAsync("clipboardCopy.copyText", generatedPrompt);
        }
    }
}
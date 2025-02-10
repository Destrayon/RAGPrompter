using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RAGPrompterWebApp.Data.Interfaces;
using RAGPrompterWebApp.Data.Services;

namespace RAGPrompterWebApp.Pages
{
    public partial class Home : IDisposable
    {
        [Inject] private IFileService FileService { get; set; } = default!;
        [Inject] private IProjectService ProjectService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private const int maxFileAmount = 5000;
        private ElementReference dropdownRef;
        private string selectedProject = "Default";
        private bool showDropdown;
        private List<IBrowserFile> files = new();
        private string question = "";
        private string generatedPrompt = "";
        private bool showFileManager;
        private bool showNewProjectInput = false;
        private string newProjectName = "";
        private CancellationTokenSource? uploadCts;

        // Upload progress tracking
        private bool isUploading;
        private bool isZipping;
        private int totalFiles;
        private int processedFiles;
        private long totalBytes;
        private long uploadedBytes;

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

        public void Dispose()
        {
            OnDropdownClose -= HandleDropdownClose;
        }

        private void HandleDropdownClose()
        {
            showDropdown = false;
            StateHasChanged();
        }

        private async Task ProcessFiles(InputFileChangeEventArgs e, bool isFolder = false)
        {
            isUploading = true;
            uploadCts = new CancellationTokenSource();

            try
            {
                var newFiles = e.GetMultipleFiles(maxFileAmount);
                var progress = new Progress<(int filesProcessed, int totalFiles, long bytesUploaded, long totalBytes, bool isZipping)>(report =>
                {
                    (processedFiles, totalFiles, uploadedBytes, totalBytes, isZipping) = report;
                    StateHasChanged();
                });

                var (success, error) = await FileService.UploadFilesAsync(
                    selectedProject,
                    newFiles,
                    progress,
                    uploadCts.Token);

                if (success && !uploadCts.Token.IsCancellationRequested)
                {
                    foreach (var file in newFiles)
                    {
                        if (!files.Any(f => f.Name == file.Name && f.Size == file.Size))
                        {
                            files.Add(file);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    // Handle error (maybe show to user)
                    Console.WriteLine($"Upload error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                files.Clear();
            }
            finally
            {
                isUploading = false;
                uploadCts?.Dispose();
                uploadCts = null;
                StateHasChanged();
            }
        }

        private async Task UploadFiles(InputFileChangeEventArgs e) =>
            await ProcessFiles(e);

        private void CancelUpload()
        {
            uploadCts?.Cancel();
        }

        #region Project Management
        private void ShowNewProjectInput()
        {
            showNewProjectInput = true;
            StateHasChanged();
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
            ProjectService.DeleteProject(project);
            if (selectedProject == project)
            {
                selectedProject = ProjectService.GetProjects()[0];
            }
            showDropdown = false;
        }

        private void AddNewProject()
        {
            if (ProjectService.IsValidProjectName(newProjectName))
            {
                ProjectService.AddProject(newProjectName);
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
        #endregion

        #region File Management
        private void ShowFileManager()
        {
            showFileManager = true;
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
        #endregion

        #region Prompt Management
        private void GeneratePrompt()
        {
            generatedPrompt = question;
            question = "";
        }

        private async Task CopyToClipboard()
        {
            await JS.InvokeVoidAsync("clipboardCopy.copyText", generatedPrompt);
        }
        #endregion
    }
}
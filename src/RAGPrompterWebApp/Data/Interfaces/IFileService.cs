using Microsoft.AspNetCore.Components.Forms;

namespace RAGPrompterWebApp.Data.Interfaces
{
    public interface IFileService
    {
        Task<List<string>> UploadFiles(IEnumerable<IBrowserFile> files);
        Task DeleteFile(string filename);
    }
}

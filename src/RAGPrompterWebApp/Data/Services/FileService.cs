using Microsoft.AspNetCore.Components.Forms;
using RAGPrompterWebApp.Data.Interfaces;

namespace RAGPrompterWebApp.Data.Services
{
    public class FileService : IFileService
    {
        public Task DeleteFile(string filename)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> UploadFiles(IEnumerable<IBrowserFile> files)
        {
            throw new NotImplementedException();
        }
    }
}

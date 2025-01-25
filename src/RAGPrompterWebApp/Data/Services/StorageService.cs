using RAGPrompterWebApp.Data.Interfaces;

namespace RAGPrompterWebApp.Data.Services
{
    public class StorageService : IStorageService
    {
        public Task<T> GetItem<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveItem(string key)
        {
            throw new NotImplementedException();
        }

        public Task SetItem<T>(string key, T value)
        {
            throw new NotImplementedException();
        }
    }
}

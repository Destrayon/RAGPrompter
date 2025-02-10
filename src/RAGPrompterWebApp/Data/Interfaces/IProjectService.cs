namespace RAGPrompterWebApp.Data.Interfaces
{
    public interface IProjectService
    {
        List<string> GetProjects();
        void AddProject(string projectName);
        void DeleteProject(string projectName);
        bool IsValidProjectName(string projectName);
    }
}

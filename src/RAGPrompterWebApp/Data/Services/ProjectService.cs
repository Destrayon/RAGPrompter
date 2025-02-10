using RAGPrompterWebApp.Data.Interfaces;

namespace RAGPrompterWebApp.Data.Services
{
    public class ProjectService : IProjectService
    {
        private List<string> _projects = new() { "Default" };

        public List<string> GetProjects() => _projects.ToList();

        public void AddProject(string projectName)
        {
            if (!IsValidProjectName(projectName)) return;
            if (!_projects.Contains(projectName))
            {
                _projects.Add(projectName);
            }
        }

        public void DeleteProject(string projectName)
        {
            _projects.Remove(projectName);
        }

        public bool IsValidProjectName(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(projectName, @"^[\w\-]+$");
        }
    }
}

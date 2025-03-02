using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface ITaskService
    {
        Task<Models.Task?> GetTaskByIdAsync(int id);
        Task<IEnumerable<Models.Task>> GetAllTasksAsync();
        Task<IEnumerable<Models.Task>> GetEnabledTasksAsync();
        Task<bool> CreateTaskAsync(Models.Task task);
        Task<bool> UpdateTaskAsync(Models.Task task);
        Task<bool> DeleteTaskAsync(int id);
        Task<bool> EnableTaskAsync(int id);
        Task<bool> DisableTaskAsync(int id);
        Task<DataExtractRule?> GetRuleByIdAsync(int id);
        Task<IEnumerable<DataExtractRule>> GetRulesByTaskIdAsync(int taskId);
        Task<bool> CreateRuleAsync(DataExtractRule rule);
        Task<bool> UpdateRuleAsync(DataExtractRule rule);
        Task<bool> DeleteRuleAsync(int id);
    }
}

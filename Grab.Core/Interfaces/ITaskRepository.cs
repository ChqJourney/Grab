using Grab.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Core.Interfaces
{
    public interface ITaskRepository
    {
        Task<Models.Task?> GetByIdAsync(int id);
        Task<IEnumerable<Models.Task>> GetAllAsync();
        Task<IEnumerable<Models.Task>> GetEnabledTasksAsync();
        Task<bool> AddAsync(Models.Task task);
        Task<bool> UpdateAsync(Models.Task task);
        Task<bool> DeleteAsync(int id);
        Task<DataExtractRule?> GetRuleByIdAsync(int id);
        Task<IEnumerable<DataExtractRule>> GetRulesByTaskIdAsync(int taskId);
        Task<bool> AddRuleAsync(DataExtractRule rule);
        Task<bool> UpdateRuleAsync(DataExtractRule rule);
        Task<bool> DeleteRuleAsync(int id);
    }
}

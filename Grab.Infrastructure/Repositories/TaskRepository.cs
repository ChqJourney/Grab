using Grab.Core.Interfaces;
using Grab.Core.Models;
using Grab.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly GrabDbContext _context;

        public TaskRepository(GrabDbContext context)
        {
            _context = context;
        }

        public async Task<Core.Models.Task?> GetByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.ExtractRules)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Core.Models.Task>> GetAllAsync()
        {
            return await _context.Tasks
                .Include(t => t.ExtractRules)
                .ToListAsync();
        }

        public async Task<IEnumerable<Core.Models.Task>> GetEnabledTasksAsync()
        {
            return await _context.Tasks
                .Include(t => t.ExtractRules)
                .Where(t => t.Enabled)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(Core.Models.Task task)
        {
            _context.Tasks.Add(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Core.Models.Task task)
        {
            _context.Tasks.Update(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return false;

            _context.Tasks.Remove(task);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<DataExtractRule?> GetRuleByIdAsync(int id)
        {
            return await _context.DataExtractRules.FindAsync(id);
        }

        public async Task<IEnumerable<DataExtractRule>> GetRulesByTaskIdAsync(int taskId)
        {
            return await _context.DataExtractRules
                .Where(r => r.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<bool> AddRuleAsync(DataExtractRule rule)
        {
            _context.DataExtractRules.Add(rule);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRuleAsync(DataExtractRule rule)
        {
            _context.DataExtractRules.Update(rule);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            var rule = await _context.DataExtractRules.FindAsync(id);
            if (rule == null)
                return false;

            _context.DataExtractRules.Remove(rule);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

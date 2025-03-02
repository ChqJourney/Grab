using Grab.Core.Interfaces;
using Grab.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository taskRepository, ILogger<TaskService> logger)
        {
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task<Core.Models.Task?> GetTaskByIdAsync(int id)
        {
            try
            {
                return await _taskRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by id: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Core.Models.Task>> GetAllTasksAsync()
        {
            try
            {
                return await _taskRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks");
                throw;
            }
        }

        public async Task<IEnumerable<Core.Models.Task>> GetEnabledTasksAsync()
        {
            try
            {
                return await _taskRepository.GetEnabledTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled tasks");
                throw;
            }
        }

        public async Task<bool> CreateTaskAsync(Core.Models.Task task)
        {
            try
            {
                return await _taskRepository.AddAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task: {Name}", task.Name);
                throw;
            }
        }

        public async Task<bool> UpdateTaskAsync(Core.Models.Task task)
        {
            try
            {
                return await _taskRepository.UpdateAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {Id}", task.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            try
            {
                return await _taskRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task: {Id}", id);
                throw;
            }
        }

        public async Task<bool> EnableTaskAsync(int id)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(id);
                if (task == null)
                    return false;

                task.Enabled = true;
                return await _taskRepository.UpdateAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling task: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DisableTaskAsync(int id)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(id);
                if (task == null)
                    return false;

                task.Enabled = false;
                return await _taskRepository.UpdateAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling task: {Id}", id);
                throw;
            }
        }

        public async Task<DataExtractRule?> GetRuleByIdAsync(int id)
        {
            try
            {
                return await _taskRepository.GetRuleByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rule by id: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DataExtractRule>> GetRulesByTaskIdAsync(int taskId)
        {
            try
            {
                return await _taskRepository.GetRulesByTaskIdAsync(taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rules for task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> CreateRuleAsync(DataExtractRule rule)
        {
            try
            {
                return await _taskRepository.AddRuleAsync(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rule for task: {TaskId}", rule.TaskId);
                throw;
            }
        }

        public async Task<bool> UpdateRuleAsync(DataExtractRule rule)
        {
            try
            {
                return await _taskRepository.UpdateRuleAsync(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rule: {Id}", rule.Id);
                throw;
            }
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            try
            {
                return await _taskRepository.DeleteRuleAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rule: {Id}", id);
                throw;
            }
        }
    }
}

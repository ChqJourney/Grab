using Grab.API.DTOs;
using Grab.Core.Interfaces;
using Grab.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grab.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetAllTasks()
        {
            _logger.LogInformation("Getting all tasks");
            
            var tasks = await _taskService.GetAllTasksAsync();
            
            return Ok(tasks.Select(MapTaskToDto));
        }

        [HttpGet("enabled")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetEnabledTasks()
        {
            _logger.LogInformation("Getting enabled tasks");
            
            var tasks = await _taskService.GetEnabledTasksAsync();
            
            return Ok(tasks.Select(MapTaskToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTaskById(int id)
        {
            _logger.LogInformation("Getting task by id: {Id}", id);
            
            var task = await _taskService.GetTaskByIdAsync(id);
            
            if (task == null)
                return NotFound();

            return Ok(MapTaskToDto(task));
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            _logger.LogInformation("Creating new task: {Name}", createTaskDto.Name);
            
            var task = new Core.Models.Task
            {
                Name = createTaskDto.Name,
                Description = createTaskDto.Description,
                SourcePath = createTaskDto.SourcePath,
                Enabled = createTaskDto.Enabled,
                TargetFileType = createTaskDto.TargetFileType
            };

            bool success = await _taskService.CreateTaskAsync(task);
            
            if (!success)
                return BadRequest("Failed to create task");

            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, MapTaskToDto(task));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            _logger.LogInformation("Updating task: {Id}", id);
            
            var task = await _taskService.GetTaskByIdAsync(id);
            
            if (task == null)
                return NotFound();

            if (updateTaskDto.Name != null)
                task.Name = updateTaskDto.Name;
                
            if (updateTaskDto.Description != null)
                task.Description = updateTaskDto.Description;
                
            if (updateTaskDto.SourcePath != null)
                task.SourcePath = updateTaskDto.SourcePath;
                
            if (updateTaskDto.Enabled.HasValue)
                task.Enabled = updateTaskDto.Enabled.Value;
                
            if (updateTaskDto.TargetFileType.HasValue)
                task.TargetFileType = updateTaskDto.TargetFileType.Value;

            bool success = await _taskService.UpdateTaskAsync(task);
            
            if (!success)
                return BadRequest("Failed to update task");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            _logger.LogInformation("Deleting task: {Id}", id);
            
            var task = await _taskService.GetTaskByIdAsync(id);
            
            if (task == null)
                return NotFound();

            bool success = await _taskService.DeleteTaskAsync(id);
            
            if (!success)
                return BadRequest("Failed to delete task");

            return NoContent();
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> EnableTask(int id)
        {
            _logger.LogInformation("Enabling task: {Id}", id);
            
            var task = await _taskService.GetTaskByIdAsync(id);
            
            if (task == null)
                return NotFound();

            bool success = await _taskService.EnableTaskAsync(id);
            
            if (!success)
                return BadRequest("Failed to enable task");

            return NoContent();
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> DisableTask(int id)
        {
            _logger.LogInformation("Disabling task: {Id}", id);
            
            var task = await _taskService.GetTaskByIdAsync(id);
            
            if (task == null)
                return NotFound();

            bool success = await _taskService.DisableTaskAsync(id);
            
            if (!success)
                return BadRequest("Failed to disable task");

            return NoContent();
        }

        [HttpGet("{taskId}/rules")]
        public async Task<ActionResult<IEnumerable<DataExtractRuleDto>>> GetRulesByTaskId(int taskId)
        {
            _logger.LogInformation("Getting rules for task: {TaskId}", taskId);
            
            var task = await _taskService.GetTaskByIdAsync(taskId);
            
            if (task == null)
                return NotFound();

            var rules = await _taskService.GetRulesByTaskIdAsync(taskId);
            
            return Ok(rules.Select(MapRuleToDto));
        }

        [HttpGet("rules/{id}")]
        public async Task<ActionResult<DataExtractRuleDto>> GetRuleById(int id)
        {
            _logger.LogInformation("Getting rule by id: {Id}", id);
            
            var rule = await _taskService.GetRuleByIdAsync(id);
            
            if (rule == null)
                return NotFound();

            return Ok(MapRuleToDto(rule));
        }

        [HttpPost("rules")]
        public async Task<ActionResult<DataExtractRuleDto>> CreateRule(CreateDataExtractRuleDto createRuleDto)
        {
            _logger.LogInformation("Creating new rule for task: {TaskId}", createRuleDto.TaskId);
            
            var task = await _taskService.GetTaskByIdAsync(createRuleDto.TaskId);
            
            if (task == null)
                return NotFound("Task not found");

            var rule = new DataExtractRule
            {
                FieldName = createRuleDto.FieldName,
                FileType = createRuleDto.FileType,
                Location = createRuleDto.Location,
                ValidationRule = createRuleDto.ValidationRule,
                TaskId = createRuleDto.TaskId
            };

            bool success = await _taskService.CreateRuleAsync(rule);
            
            if (!success)
                return BadRequest("Failed to create rule");

            return CreatedAtAction(nameof(GetRuleById), new { id = rule.Id }, MapRuleToDto(rule));
        }

        [HttpPut("rules/{id}")]
        public async Task<IActionResult> UpdateRule(int id, UpdateDataExtractRuleDto updateRuleDto)
        {
            _logger.LogInformation("Updating rule: {Id}", id);
            
            var rule = await _taskService.GetRuleByIdAsync(id);
            
            if (rule == null)
                return NotFound();

            if (updateRuleDto.FieldName != null)
                rule.FieldName = updateRuleDto.FieldName;
                
            if (updateRuleDto.FileType.HasValue)
                rule.FileType = updateRuleDto.FileType.Value;
                
            if (updateRuleDto.Location != null)
                rule.Location = updateRuleDto.Location;
                
            if (updateRuleDto.ValidationRule != null)
                rule.ValidationRule = updateRuleDto.ValidationRule;

            bool success = await _taskService.UpdateRuleAsync(rule);
            
            if (!success)
                return BadRequest("Failed to update rule");

            return NoContent();
        }

        [HttpDelete("rules/{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            _logger.LogInformation("Deleting rule: {Id}", id);
            
            var rule = await _taskService.GetRuleByIdAsync(id);
            
            if (rule == null)
                return NotFound();

            bool success = await _taskService.DeleteRuleAsync(id);
            
            if (!success)
                return BadRequest("Failed to delete rule");

            return NoContent();
        }

        private static TaskDto MapTaskToDto(Core.Models.Task task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Name = task.Name,
                Description = task.Description,
                SourcePath = task.SourcePath,
                Enabled = task.Enabled,
                TargetFileType = task.TargetFileType,
                ExtractRules = task.ExtractRules.Select(MapRuleToDto).ToList()
            };
        }

        private static DataExtractRuleDto MapRuleToDto(DataExtractRule rule)
        {
            return new DataExtractRuleDto
            {
                Id = rule.Id,
                FieldName = rule.FieldName,
                FileType = rule.FileType,
                Location = rule.Location,
                ValidationRule = rule.ValidationRule,
                TaskId = rule.TaskId
            };
        }
    }
}

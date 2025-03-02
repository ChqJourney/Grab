using Grab.API.DTOs;
using Grab.Core.Interfaces;
using Grab.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grab.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly IExtractedDataRepository _extractedDataRepository;
        private readonly ITaskService _taskService;
        private readonly ILogger<DataController> _logger;

        public DataController(
            IExtractedDataRepository extractedDataRepository,
            ITaskService taskService,
            ILogger<DataController> logger)
        {
            _extractedDataRepository = extractedDataRepository;
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExtractedDataDto>>> GetExtractedData([FromQuery] ExtractedDataFilterDto filter)
        {
            _logger.LogInformation("Getting extracted data with filter");

            IEnumerable<ExtractedData> data;

            if (filter.TaskId.HasValue)
            {
                // 检查任务是否存在
                var task = await _taskService.GetTaskByIdAsync(filter.TaskId.Value);
                if (task == null)
                {
                    return NotFound("Task not found");
                }

                data = await _extractedDataRepository.GetByTaskIdAsync(filter.TaskId.Value);
            }
            else
            {
                // 如果没有指定任务ID，先检查是否存在文件路径过滤器
                if (!string.IsNullOrEmpty(filter.FieldName))
                {
                    // 获取满足字段名称条件的所有数据
                    // 注意: 在实际实现中，你可能需要在 IExtractedDataRepository 中添加一个更复杂的 GetByFilterAsync 方法
                    // 这里仅进行简化处理
                    data = (await _extractedDataRepository.GetByTaskIdAsync(0)).Where(d => d.FieldName == filter.FieldName);
                }
                else
                {
                    // 在实际实现中，可能需要限制返回的数据量，避免返回所有数据
                    return BadRequest("At least specify TaskId or FieldName filter");
                }
            }

            // 应用其他过滤条件
            if (filter.RuleId.HasValue)
            {
                data = data.Where(d => d.RuleId == filter.RuleId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                data = data.Where(d => d.ExtractedTime >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                data = data.Where(d => d.ExtractedTime <= filter.ToDate.Value);
            }

            if (filter.IsValid.HasValue)
            {
                data = data.Where(d => d.IsValid == filter.IsValid.Value);
            }

            return Ok(data.Select(MapExtractedDataToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExtractedDataDto>> GetDataById(int id)
        {
            _logger.LogInformation("Getting extracted data by id: {Id}", id);

            var data = await _extractedDataRepository.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(MapExtractedDataToDto(data));
        }

        [HttpGet("file")]
        public async Task<ActionResult<IEnumerable<ExtractedDataDto>>> GetDataByFilePath(string filePath)
        {
            _logger.LogInformation("Getting extracted data for file: {FilePath}", filePath);

            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path is required");

            var data = await _extractedDataRepository.GetByFilePathAsync(filePath);

            return Ok(data.Select(MapExtractedDataToDto));
        }

        private static ExtractedDataDto MapExtractedDataToDto(ExtractedData data)
        {
            return new ExtractedDataDto
            {
                Id = data.Id,
                FilePath = data.FilePath,
                TaskId = data.TaskId,
                RuleId = data.RuleId,
                FieldName = data.FieldName,
                Value = data.Value,
                ExtractedTime = data.ExtractedTime,
                IsValid = data.IsValid,
                ValidationMessage = data.ValidationMessage
            };
        }
    }
}

using Grab.API.DTOs;
using Grab.Core.Interfaces;
using Grab.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grab.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanController : ControllerBase
    {
        private readonly IScanService _scanService;
        private readonly ITaskService _taskService;
        private readonly IDirectoryRepository _directoryRepository;
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<ScanController> _logger;

        public ScanController(
            IScanService scanService,
            ITaskService taskService,
            IDirectoryRepository directoryRepository,
            IFileRepository fileRepository,
            ILogger<ScanController> logger)
        {
            _scanService = scanService;
            _taskService = taskService;
            _directoryRepository = directoryRepository;
            _fileRepository = fileRepository;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<ActionResult<string>> StartScan(ScanRequestDto request)
        {
            _logger.LogInformation("Starting scan of path: {Path}", request.RootPath);

            if (string.IsNullOrEmpty(request.RootPath) || !System.IO.Directory.Exists(request.RootPath))
            {
                return BadRequest("Invalid root path");
            }

            if (request.TaskId.HasValue)
            {
                var task = await _taskService.GetTaskByIdAsync(request.TaskId.Value);
                if (task == null)
                {
                    return NotFound("Task not found");
                }

                if (!task.Enabled)
                {
                    return BadRequest("Task is disabled");
                }
            }

            // 异步启动扫描过程
            _ = Task.Run(async () =>
            {
                await _scanService.ScanDirectoriesAsync(request.RootPath);
            });

            return Ok(new { Message = "Scan started successfully" });
        }

        [HttpGet("directories")]
        public async Task<ActionResult<IEnumerable<DirectoryDto>>> GetDirectories([FromQuery] string? status)
        {
            _logger.LogInformation("Getting directories with status: {Status}", status ?? "all");

            IEnumerable<Directory> directories;

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DirectoryStatus>(status, true, out var directoryStatus))
            {
                directories = await _directoryRepository.GetByStatusAsync(directoryStatus);
            }
            else
            {
                directories = await _directoryRepository.GetAllAsync();
            }

            return Ok(directories.Select(MapDirectoryToDto));
        }

        [HttpGet("directories/{path}")]
        public async Task<ActionResult<DirectoryDto>> GetDirectoryByPath(string path)
        {
            _logger.LogInformation("Getting directory by path: {Path}", path);

            var directory = await _directoryRepository.GetByPathAsync(path);

            if (directory == null)
                return NotFound();

            return Ok(MapDirectoryToDto(directory));
        }

        [HttpGet("files")]
        public async Task<ActionResult<IEnumerable<FileItemDto>>> GetFilesByDirectory(string directoryPath)
        {
            _logger.LogInformation("Getting files for directory: {DirectoryPath}", directoryPath);

            var directory = await _directoryRepository.GetByPathAsync(directoryPath);

            if (directory == null)
                return NotFound("Directory not found");

            var files = await _fileRepository.GetByDirectoryPathAsync(directoryPath);

            return Ok(files.Select(MapFileToDto));
        }

        [HttpGet("files/{status}")]
        public async Task<ActionResult<IEnumerable<FileItemDto>>> GetFilesByStatus(string status)
        {
            _logger.LogInformation("Getting files with status: {Status}", status);

            if (!Enum.TryParse<FileStatus>(status, true, out var fileStatus))
                return BadRequest("Invalid status");

            var files = await _fileRepository.GetByStatusAsync(fileStatus);

            return Ok(files.Select(MapFileToDto));
        }

        [HttpPost("files/process/{path}")]
        public async Task<IActionResult> ProcessFile(string path)
        {
            _logger.LogInformation("Processing file: {Path}", path);

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return BadRequest("Invalid file path");

            await _scanService.ProcessFileAsync(path);

            return Ok(new { Message = "File processing started" });
        }

        private static DirectoryDto MapDirectoryToDto(Directory directory)
        {
            return new DirectoryDto
            {
                Path = directory.Path,
                LastSignature = directory.LastSignature,
                LastCheckTime = directory.LastCheckTime,
                LastProcessTime = directory.LastProcessTime,
                Status = directory.Status
            };
        }

        private static FileItemDto MapFileToDto(FileItem file)
        {
            return new FileItemDto
            {
                Path = file.Path,
                DirectoryPath = file.DirectoryPath,
                FileSize = file.FileSize,
                ModifiedTime = DateTimeOffset.FromUnixTimeMilliseconds(file.ModifiedTime).DateTime,
                ProcessTime = file.ProcessTime,
                Status = file.Status,
                Hash = file.Hash
            };
        }
    }
}

using Grab.Core.Interfaces;
using Grab.Infrastructure.Data;
using Grab.Infrastructure.Repositories;
using Grab.Infrastructure.Services;
using Grab.Infrastructure.Services.DocumentParsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Grab.Infrastructure.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // 注册数据库上下文
            services.AddDbContext<GrabDbContext>();

            // 注册仓储
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IDirectoryRepository, DirectoryRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IExtractedDataRepository, ExtractedDataRepository>();

            // 注册服务
            services.AddScoped<IFileSystemService, FileSystemService>();
            services.AddScoped<IDocumentProcessorService, DocumentProcessorService>();

            // 注册文档解析器工厂
            services.AddSingleton<Func<string, ILogger, IDocumentParser>>(serviceProvider => 
                (filePath, logger) => DocumentParserFactory.CreateParser(filePath, logger));

            return services;
        }
    }
}

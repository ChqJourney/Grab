using Grab.API.Middlewares;
using Grab.Core.Interfaces;
using Grab.Infrastructure.Data;
using Grab.Infrastructure.Extensions;
using Grab.Infrastructure.Repositories;
using Grab.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 添加配置
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// 添加服务到容器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 配置跨域策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Grab API", Version = "v1" });
});

// 配置数据库
builder.Services.AddDbContext<GrabDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 使用基础设施服务扩展方法注册所有服务
builder.Services.AddInfrastructureServices();

// 注册其他服务
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<ITaskService, TaskService>();

// 配置后台服务
builder.Services.AddHostedService<ScanBackgroundService>();

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthorization();

// 添加全局异常处理中间件
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

// 确保数据库创建并应用迁移
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GrabDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

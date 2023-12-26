using sfms_rest_api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// IMvcBuilder chain
builder.Services
    // to register controllers on this assembly
    .AddControllers()
    // to register SfmsController
    .AddSmfsController();

// sfms dependent resource
const string DATA_DIR = "data";
if (Directory.Exists(DATA_DIR) == false)
{
    Directory.CreateDirectory(DATA_DIR);
}
const string DB_FILE = "container.db";
builder.Services
    .AddSingleton(new sfms.Container($"{DATA_DIR}/${DB_FILE}"));

// IServiceCollection chain
builder.Services
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

// IEndpointRouteBuilder chain
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app
        .UseSwagger()
        .UseSwaggerUI();
}

// IApplicationBuilder chain
app
    .UseHttpsRedirection()
    .UseAuthorization();

// WebApplication chain
app.Run();

using Carter;
using InnNou.Application.Abstractions;
using InnNou.Application.Common;
using InnNou.Infrastructure.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCarter();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}
app.UseExceptionHandler(app =>
{
    app.Run(async context =>
    {
        context.Response.StatusCode = 400;

        var response = ApiResponse<object>.FailureResponse(
            "INVALID_REQUEST",
            "Invalid request format",
            400
        );

        await context.Response.WriteAsJsonAsync(response);
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();
//app.UseAuthentication();
//app.UseAuthorization();
app.MapCarter();

app.MapGet("/ping", () => "pong").WithName("Ping").Produces<string>(200);

app.Run();

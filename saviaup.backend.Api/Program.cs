using SaviaUp.Backend.Api;
using SaviaUp.Backend.Core;
using SaviaUp.Backend.Domain;
using SaviaUp.Backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDomain()
    .AddCore()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();
app.UseApi();
app.Run();

public partial class Program;

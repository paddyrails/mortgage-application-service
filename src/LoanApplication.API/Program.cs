using Microsoft.EntityFrameworkCore;
using LoanApplication.API.Data;
using LoanApplication.API.Services;
using LoanApplication.API.Clients;
using LoanApplication.API.Orchestration;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("ApplicationDb"));

// Configure HTTP Clients with Polly
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Customer Service Client
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy).AddPolicyHandler(circuitBreakerPolicy);

// Property Service Client
builder.Services.AddHttpClient<IPropertyServiceClient, PropertyServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:PropertyService"] ?? "http://localhost:5002");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy).AddPolicyHandler(circuitBreakerPolicy);

// Loan Service Client
builder.Services.AddHttpClient<ILoanServiceClient, LoanServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:LoanService"] ?? "http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy).AddPolicyHandler(circuitBreakerPolicy);

// Payment Service Client
builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:PaymentService"] ?? "http://localhost:5004");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy).AddPolicyHandler(circuitBreakerPolicy);

// Add Services
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Loan Application Service API (Orchestrator)", 
        Version = "v1",
        Description = "Main orchestrator service for mortgage applications. Coordinates all other services."
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Urls.Add($"http://+:{port}");

Console.WriteLine($"Loan Application Service (Orchestrator) starting on port {port}...");
Console.WriteLine("Dependencies:");
Console.WriteLine($"  - Customer Service: {builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001"}");
Console.WriteLine($"  - Property Service: {builder.Configuration["ServiceUrls:PropertyService"] ?? "http://localhost:5002"}");
Console.WriteLine($"  - Loans Service: {builder.Configuration["ServiceUrls:LoanService"] ?? "http://localhost:5003"}");
Console.WriteLine($"  - Payments Service: {builder.Configuration["ServiceUrls:PaymentService"] ?? "http://localhost:5004"}");

app.Run();

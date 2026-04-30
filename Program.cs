using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Middleware;
using APIlog.Server.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Firebase Admin SDK
var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"]
    ?? throw new InvalidOperationException("Firebase:ServiceAccountPath is not configured.");
Environment.SetEnvironmentVariable(
    "GOOGLE_APPLICATION_CREDENTIALS",
    Path.GetFullPath(serviceAccountPath)
);
FirebaseApp.Create(new AppOptions
{
    Credential = await GoogleCredential.GetApplicationDefaultAsync()
});

// Firestore (same service account, project_id read from the key file)
var saJson = File.ReadAllText(Path.GetFullPath(serviceAccountPath));
var projectId = JsonDocument.Parse(saJson).RootElement.GetProperty("project_id").GetString()
    ?? throw new InvalidOperationException("project_id not found in service account JSON.");
var firestoreDb = new FirestoreDbBuilder { ProjectId = projectId }.Build();
builder.Services.AddSingleton(firestoreDb);

// Database
builder.Services.AddDbContext<BookstoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<IBookStoresService, BookStoresService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<IPurchasesService, PurchasesService>();
builder.Services.AddScoped<ISuppliesService, SuppliesService>();
builder.Services.AddScoped<IDictionariesService, DictionariesService>();
builder.Services.AddScoped<IEmployeesService, EmployeesService>();
builder.Services.AddScoped<IBranchesService, BranchesService>();
builder.Services.AddScoped<IChartsService, ChartsService>();

// Auth scheme — needed so UseAuthorization() has a DefaultForbidScheme/ChallengeScheme.
// Actual authentication is handled by FirebaseAuthMiddleware; this handler only
// converts Forbid → 403 and Challenge → 401 without touching the token logic.
builder.Services.AddAuthentication("Firebase")
    .AddScheme<AuthenticationSchemeOptions, APIlog.Server.Middleware.FirebaseAuthHandler>(
        "Firebase", null);

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var ex = feature?.Error;
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = ex switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ArgumentException => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
    await context.Response.WriteAsJsonAsync(new { error = ex?.Message });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

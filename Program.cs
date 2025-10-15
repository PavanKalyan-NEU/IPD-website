using USPTOQueryBuilder.Services;
using EnrollmentDashboard.Services;
using Northeastern_Personal_Workspace.Services; // Add this for Course Search

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Add USPTO Query Builder Services (existing)
builder.Services.AddHttpClient<PatentsViewApiService>();
builder.Services.AddSingleton<QueryBuilderService>();
builder.Services.AddSingleton<QueryStorageService>();
builder.Services.AddScoped<FileProcessingService>();
// Register the ProgramParsingService
builder.Services.AddSingleton<ProgramParsingService>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var pdfPath = Path.Combine(env.WebRootPath, "data", "Graduate_Catalog.pdf");
    return new ProgramParsingService(pdfPath);
});

// ===== ADD ENROLLMENT DASHBOARD SERVICES HERE =====
// Add HttpClient specifically for EnrollmentService
builder.Services.AddHttpClient<IEnrollmentService, EnrollmentService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set timeout for Google Sheets requests
});

// Register EnrollmentService
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
// ===== END OF ENROLLMENT DASHBOARD SERVICES =====

// ===== ADD COURSE SEARCH SERVICES HERE =====
// Register PdfParsingService as a singleton for Course Search
builder.Services.AddSingleton<PdfParsingService>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var pdfPath = Path.Combine(env.WebRootPath, "data", "course_catalog.pdf");
    return new PdfParsingService(pdfPath);
});
// ===== END OF COURSE SEARCH SERVICES =====

// Add memory cache for dashboard data (optional but recommended)
builder.Services.AddMemoryCache();

// Configure HttpClient for GoogleSheetsService with longer timeout
// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Optional: Add background service for cleanup
builder.Services.AddHostedService<CleanupService>();

// Add CORS if you need to call the API from JavaScript (optional)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy",
        builder =>
        {
            builder.WithOrigins("*")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add CORS middleware (if added above)
app.UseCors("DashboardPolicy");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add specific route for Dashboard
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

// Add specific route for Course Search
app.MapControllerRoute(
    name: "course",
    pattern: "Course/{action=Index}/{id?}",
    defaults: new { controller = "Course" });

app.Run();
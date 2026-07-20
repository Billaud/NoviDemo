using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Global exception handler - μεταφράζει exceptions σε HTTP status codes σε ένα σημείο
// (βλ. GlobalExceptionHandler.cs), οι controllers δεν κάνουν πλέον try/catch.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// EF Core / SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis cache. Το maxmemory/eviction policy (512MB, allkeys-lru) ρυθμίζεται server-side
// στο Redis (redis.conf ή docker-compose command), όχι εδώ - ο client απλά συνδέεται.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "IpLookupApi:";
});
builder.Services.AddScoped<ICacheService, CacheService>();

// Repositories
builder.Services.AddScoped<IIpRepository, IpRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IJobHistoryRepository, JobHistoryRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// CQRS μέσω MediatR + FluentValidation, όλα καλωδιωμένα μέσα στο Application project
// (ApplicationServiceRegistration.cs) - handlers, LoggingBehavior, ValidationBehavior,
// validators. Εδώ μένει μόνο η μία γραμμή κλήσης.
builder.Services.AddApplication();

// IP2C HttpClient + retry/circuit breaker/timeout (Polly) + logging decorator - βλ. Ip2cServiceRegistration.cs
builder.Services.AddIp2cService();

// Task 2: periodic (hourly) job που ξαναρωτάει το IP2C για όλα τα αποθηκευμένα IPs.
// Το Quartz κρατάει το δικό του DI scope ανά job run, οπότε το IpInfoUpdateJob παίρνει
// scoped dependencies (DbContext κ.λπ.) κανονικά μέσω constructor injection.
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey(nameof(IpInfoUpdateJob));

    q.AddJob<IpInfoUpdateJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity(nameof(IpInfoUpdateJob) + "-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInHours(1)
            .RepeatForever()
            // Misfire (π.χ. η εφαρμογή ήταν κάτω όταν έπρεπε να τρέξει): τρέξε τη
            // δουλειά ΑΜΕΣΩΣ μία φορά μόλις ξανασηκωθεί, μετά συνέχισε κανονικά στο
            // ωριαίο πρόγραμμα. Δεν μας νοιάζει το ακριβές ωράριο, μας νοιάζει να μην
            // μείνουν IPs ασυγχρόνιστα για πολύ - βλ. IpInfoUpdateJob.
            .WithMisfireHandlingInstructionFireNow())
        .StartNow());
});
builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

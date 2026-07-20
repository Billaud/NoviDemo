using Microsoft.EntityFrameworkCore;

namespace IpLookupApi.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<Ip> Ips => Set<Ip>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<JobHistory> JobHistories => Set<JobHistory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");
            entity.HasKey(c => c.TwoLetterCode);
            entity.Property(c => c.TwoLetterCode).HasColumnType("char(2)").IsRequired();
            entity.Property(c => c.ThreeLetterCode).HasColumnType("char(3)").IsRequired();
            entity.HasIndex(c => c.ThreeLetterCode).IsUnique();
            entity.Property(c => c.CountryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2").IsRequired();
        });

        modelBuilder.Entity<Ip>(entity =>
        {
            entity.ToTable("IpAddress"); // table name στο schema, class name παραμένει "Ip"
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedOnAdd();
            entity.Property(i => i.Address).HasColumnType("varchar(15)").IsRequired();
            entity.HasIndex(i => i.Address).IsUnique();

            entity.Property(i => i.CountryTwoLetterCode)
                .HasColumnName("CountryTwoLetterCode")
                .HasColumnType("char(2)")
                .IsRequired();

            entity.Property(i => i.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2").IsRequired();
            entity.Property(i => i.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2").IsRequired();

            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(i => i.CountryTwoLetterCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Covering index για το report (GetReportQuery): GROUP BY CountryTwoLetterCode
            // με COUNT(*) / MAX(UpdatedAt). Χωρίς αυτό, το EF-generated FK index στο
            // CountryTwoLetterCode θα υπήρχε ούτως ή άλλως, αλλά η SQL Server θα χρειαζόταν
            // να πάει στο clustered index (Id) για να διαβάσει το UpdatedAt από κάθε γραμμή
            // (key lookup ανά row). Με το INCLUDE, το group-by/aggregate γίνεται index-only -
            // ούτε αγγίζει το clustered index. Εξυπηρετεί και το /api/reports (all countries,
            // full scan+group στο index) και το /api/reports/{countryCode} (index seek).
            entity.HasIndex(i => i.CountryTwoLetterCode)
                .IncludeProperties(i => i.UpdatedAtUtc)
                .HasDatabaseName("IX_IpAddress_CountryTwoLetterCode_UpdatedAt");
        });

        modelBuilder.Entity<JobHistory>(entity =>
        {
            entity.ToTable("JobHistory");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Id).ValueGeneratedOnAdd();
            entity.Property(j => j.StartedAtUtc).HasColumnName("StartedAt").HasColumnType("datetime2").IsRequired();
            entity.Property(j => j.FinishedAtUtc).HasColumnName("FinishedAt").HasColumnType("datetime2");
            entity.Property(j => j.Status).HasColumnName("Status").HasMaxLength(20)
                .HasConversion<string>().IsRequired();
            entity.Property(j => j.ProcessedRecords).HasColumnName("ProcessedRecords").IsRequired();
            entity.Property(j => j.UpdatedRecords).HasColumnName("UpdatedRecords").IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}

using KassaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<MyDebt> MyDebts => Set<MyDebt>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashBoxBalance> CashBoxBalances => Set<CashBoxBalance>();
    public DbSet<CashBoxTransaction> CashBoxTransactions => Set<CashBoxTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<CashBoxBalance>()
            .HasKey(c => c.Currency);

        modelBuilder.Entity<CashBoxBalance>().HasData(
            new CashBoxBalance { Currency = Currency.USD, Amount = 0 },
            new CashBoxBalance { Currency = Currency.RUB, Amount = 0 }
        );

        // Decimal dəqiqliyi
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,4)");
        }

        modelBuilder.Entity<Loan>()
            .HasOne(l => l.Client)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoanPayment>()
            .HasOne(p => p.Loan)
            .WithMany(l => l.Payments)
            .HasForeignKey(p => p.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MyDebt>()
            .HasOne(d => d.Client)
            .WithMany(c => c.MyDebts)
            .HasForeignKey(d => d.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Exchange>()
            .HasOne(e => e.Client)
            .WithMany(c => c.Exchanges)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Client>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Loan>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LoanPayment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MyDebt>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Exchange>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CashBoxBalance>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CashBoxTransaction>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConvertDeletesToSoftDeletes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ConvertDeletesToSoftDeletes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConvertDeletesToSoftDeletes()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(entry => entry.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
}

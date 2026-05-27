using FinTechApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FinTechApi.Data;

public class FinTechContext : DbContext
{
    public FinTechContext(DbContextOptions<FinTechContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Instrument> Instruments { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Trade> Trades { get; set; } = null!;
    public DbSet<Portfolio> Portfolios { get; set; } = null!;
    public DbSet<LedgerEntry> LedgerEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Customer -> Accounts
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Accounts)
            .WithOne()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Account
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Account>()
            .Property(a => a.Currency)
            .HasConversion<string>();

        // Transaction
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Currency)
            .HasConversion<string>();

        // Instrument
        modelBuilder.Entity<Instrument>()
            .HasKey(i => i.Symbol);

        modelBuilder.Entity<Instrument>()
            .Property(i => i.Currency)
            .HasConversion<string>();

        // Order
        modelBuilder.Entity<Order>()
            .Property(o => o.LimitPrice)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Order>()
            .Property(o => o.Side)
            .HasConversion<string>();

        // Trade
        modelBuilder.Entity<Trade>()
            .Property(t => t.Price)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Trade>()
            .Property(t => t.Commission)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Trade>()
            .Property(t => t.Side)
            .HasConversion<string>();

        // LedgerEntry
        modelBuilder.Entity<LedgerEntry>()
            .Property(l => l.Amount)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<LedgerEntry>()
            .Property(l => l.Currency)
            .HasConversion<string>();

        // Portfolio.Positions: store as JSON in a single column
        var dictConverter = new ValueConverter<Dictionary<string, Position>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, Position>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, Position>());

        modelBuilder.Entity<Portfolio>()
            .Property(p => p.Positions)
            .HasConversion(dictConverter)
            .HasColumnType("nvarchar(max)");

        base.OnModelCreating(modelBuilder);
    }
}
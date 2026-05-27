using Microsoft.EntityFrameworkCore;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

      public DbSet<User> Users { get; set; }
      public DbSet<Account> Accounts { get; set; }
      public DbSet<Transaction> Transactions { get; set; }
      public DbSet<Category> Categories { get; set; }
      public DbSet<FinancialGoal> FinancialGoals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany() 
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.InitialBalance)
                      .HasColumnType("decimal(18,2)");

                entity.Property(a => a.Balance)
                      .HasColumnType("decimal(18,2)");

                entity.Property(a => a.Type)
                      .HasConversion(
                          v => v.ToString(),
                          v => (AccountType)Enum.Parse(typeof(AccountType), v));
            });

            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasOne(t => t.Account)
                      .WithMany() 
                      .HasForeignKey(t => t.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Amount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(t => t.Type)
                      .HasConversion(
                          v => v.ToString(),
                          v => (TransactionType)Enum.Parse(typeof(TransactionType), v));

                entity.Property(t => t.Date)
                      .HasColumnType("date")
                      .HasConversion(
                        v => v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

                entity.HasOne(t => t.FinancialGoal)
                      .WithMany()
                      .HasForeignKey(t => t.FinancialGoalId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
                  modelBuilder.Entity<FinancialGoal>(entity =>
                  {
                        entity.ToTable("FinancialGoals");
                        entity.HasKey(fg => fg.Id);
                        entity.Property(fg => fg.TargetAmount).HasColumnType("decimal(18,2)");
                        entity.Property(fg => fg.CurrentAmount).HasColumnType("decimal(18,2)");
                        // Relacionamento 1-N: User -> FinancialGoals
                        entity.HasOne<User>()
                                .WithMany()
                                .HasForeignKey(fg => fg.UserId)
                                .OnDelete(DeleteBehavior.Cascade);
                  });
            }
    }
}

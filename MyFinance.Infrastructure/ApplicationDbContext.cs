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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da Entidade Account
            modelBuilder.Entity<Account>(entity =>
            {
                // Define a relação: 1 Usuário (User) pode ter MUITAS Contas (Accounts)
                // A chave estrangeira é UserId
                entity.HasOne(a => a.User)
                      .WithMany() // User não precisa de uma lista de Accounts por enquanto
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Se o User for deletado, suas contas também são.

                // Configura a propriedade 'InitialBalance' para ser do tipo 'decimal'
                entity.Property(a => a.InitialBalance)
                      .HasColumnType("decimal(18,2)");

                // Configura o Enum 'Type' para ser salvo como string no BD (mais legível)
                // Se preferir salvar como número (int), remova estas 3 linhas.
                entity.Property(a => a.Type)
                      .HasConversion(
                          v => v.ToString(),
                          v => (AccountType)Enum.Parse(typeof(AccountType), v));
            });

            // BÔNUS: Adiciona uma restrição de "Email Único" que faltou na 1ª migration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });
        }
    }
}

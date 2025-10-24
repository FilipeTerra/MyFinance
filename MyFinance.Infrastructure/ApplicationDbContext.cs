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

            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                // Define a relação: 1 Conta (Account) pode ter MUITAS Transações (Transactions)
                // A chave estrangeira é AccountId
                entity.HasOne(t => t.Account)
                      .WithMany() // Account não precisa ter uma lista de Transactions por enquanto
                      .HasForeignKey(t => t.AccountId)
                      // IMPORTANTE: Evita ciclos ou múltiplos caminhos de cascade.
                      // A deleção já está configurada como Cascade no lado do User -> Account.
                      // Se Account for deletada, as Transactions irão junto.
                      // Se tentarmos deletar uma Transaction diretamente, não afeta a Account.
                      .OnDelete(DeleteBehavior.Restrict);

                // Configura a propriedade 'Amount' para ser do tipo 'decimal'
                entity.Property(t => t.Amount)
                      .HasColumnType("decimal(18,2)");

                // Configura o Enum 'Type' para ser salvo como string no BD
                entity.Property(t => t.Type)
                      .HasConversion(
                          v => v.ToString(),
                          v => (TransactionType)Enum.Parse(typeof(TransactionType), v));

                // Configura a propriedade Date para ser apenas Date no banco (sem hora)
                // Opcional, mas pode ser útil dependendo do SGBD e consultas
                entity.Property(t => t.Date)
                      .HasColumnType("date");
            });
        }
    }
}

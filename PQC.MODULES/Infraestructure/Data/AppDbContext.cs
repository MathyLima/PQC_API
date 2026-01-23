using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Domain.Entities;
using PQC.MODULES.Users.Domain.Entities;

namespace PQC.MODULES.Infraestructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets - Tabelas
        public DbSet<User> Usuarios { get; set; }
        public DbSet<StoredDocument> Documentos { get; set; }
        public DbSet<Signature> Assinaturas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela Usuario
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("usuarios");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasMaxLength(36)
                    .IsRequired();

                entity.Property(e => e.Nome)
                    .HasColumnName("nome")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Cpf)
                    .HasColumnName("cpf")
                    .HasMaxLength(11)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Telefone)
                    .HasColumnName("telefone")
                    .HasMaxLength(20);

                entity.Property(e => e.Login)
                    .HasColumnName("login")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Senha)
                    .HasColumnName("senha")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.CodigoAlgoritmo)
                    .HasColumnName("codigo_algoritmo")
                    .HasMaxLength(50)
                    .IsRequired();

                // Índices
                entity.HasIndex(e => e.Cpf).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Login).IsUnique();
            });

            // Configuração da tabela Documento
            modelBuilder.Entity<StoredDocument>(entity =>
            {
                entity.ToTable("documentos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasMaxLength(36)
                    .IsRequired();

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Nome)
                    .HasColumnName("nome")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.IdUsuario)
                    .HasColumnName("id_usuario")
                    .HasMaxLength(36)
                    .IsRequired();

                entity.Property(e => e.Assinado_em)
                    .HasColumnName("assinado_em")
                    .IsRequired();
              
                entity.Property(e => e.TipoArquivo)
                    .HasColumnName("tipo_arquivo")
                    .IsRequired();
                
                entity.Property(e => e.AssinaturaDigital)
                    .HasColumnName("assinatura")
                    .IsRequired();
                
                entity.Property(e=>e.AlgoritmoAssinatura)
                    .HasColumnName("algoritmo_assinatura")
                    .IsRequired();
                
                entity.Property(e => e.Tamanho)
                    .HasColumnName("tamanho")
                    .IsRequired();


                // Relacionamento: 1 Usuario -> N Documentos
                entity.HasOne(d => d.Usuario)   
                  .WithMany(u => u.Documentos)
                  .HasForeignKey(d => d.IdUsuario)
                  .OnDelete(DeleteBehavior.Restrict);

                // Índices
                entity.HasIndex(e => e.IdUsuario);
                entity.HasIndex(e => e.Assinado_em);
            });

           
        }
    }
}

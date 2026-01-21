using Microsoft.EntityFrameworkCore;
using PQC.MODULES.Documents.Domain.Entities;
using PQC.MODULES.Signatures.Domain.Entities;
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
        public DbSet<Document> Documentos { get; set; }
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
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("documento");

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

                entity.Property(e => e.UploadEm)
                    .HasColumnName("upload_em")
                    .IsRequired();

                // Relacionamento: 1 Usuario -> N Documentos
                entity.HasOne(d => d.Usuario)
                    .WithMany(u => u.Documentos)
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices
                entity.HasIndex(e => e.IdUsuario);
                entity.HasIndex(e => e.UploadEm);
            });

            // Configuração da tabela Assinatura
            modelBuilder.Entity<Signature>(entity =>
            {
                entity.ToTable("assinaturas");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasMaxLength(36)
                    .IsRequired();

                entity.Property(e => e.IdDocumento)
                    .HasColumnName("id_documento")
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

                entity.Property(e => e.AssinaturaDigital)
                    .HasColumnName("assinatura_digital")
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(e => e.AssinadoEm)
                    .HasColumnName("assinado_em")
                    .IsRequired();

                // Relacionamento: 1 Documento -> N Assinaturas
                entity.HasOne(a => a.Documento)
                    .WithMany(d => d.Assinaturas)
                    .HasForeignKey(a => a.IdDocumento)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices
                entity.HasIndex(e => e.IdDocumento);
                entity.HasIndex(e => e.Cpf);
                entity.HasIndex(e => e.AssinadoEm);
            });
        }
    }
}

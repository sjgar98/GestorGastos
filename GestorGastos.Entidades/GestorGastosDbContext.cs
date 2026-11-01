using GestorGastos.DTOs.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Entidades
{
    public class GestorGastosDbContext : DbContext
    {
        public GestorGastosDbContext(DbContextOptions<GestorGastosDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Comprobante> Comprobantes { get; set; }
        public DbSet<HistorialEstado> HistorialEstados { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
            configurationBuilder.Properties<EstadoComprobante>().HaveConversion<string>().HaveMaxLength(30);
            configurationBuilder.Properties<TipoDocumento>().HaveConversion<string>().HaveMaxLength(30);
            configurationBuilder.Properties<Categoria>().HaveConversion<string>().HaveMaxLength(30);
            configurationBuilder.Properties<Moneda>().HaveConversion<string>().HaveMaxLength(3).AreFixedLength().AreUnicode(false);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(usuario =>
            {
                usuario.Property(u => u.Id).HasMaxLength(36);
                usuario.Property(u => u.Nombre).HasMaxLength(200);
                usuario.Property(u => u.Email).HasMaxLength(256);
            });

            modelBuilder.Entity<Comprobante>(comprobante =>
            {
                comprobante.Property(c => c.NombreArchivoOriginal).HasMaxLength(255);
                comprobante.Property(c => c.BlobNombre).HasMaxLength(100);
                comprobante.Property(c => c.ContentType).HasMaxLength(50);
                comprobante.Property(c => c.Comercio).HasMaxLength(200);
                comprobante.Property(c => c.Descripcion).HasMaxLength(500);
                comprobante.Property(c => c.MensajeError).HasMaxLength(500);
                comprobante.Property(c => c.MotivoRechazo).HasMaxLength(500);
                comprobante.HasIndex(c => c.BlobNombre).IsUnique();
                comprobante.HasIndex(c => new { c.UsuarioId, c.Estado });
                comprobante.HasIndex(c => c.FechaCarga);

                comprobante.HasOne(c => c.Usuario)
                    .WithMany(u => u.Comprobantes)
                    .HasForeignKey(c => c.UsuarioId);
            });

            modelBuilder.Entity<HistorialEstado>(historial =>
            {
                historial.Property(h => h.Observacion).HasMaxLength(500);

                historial.HasOne(h => h.Comprobante)
                    .WithMany(c => c.Historial)
                    .HasForeignKey(h => h.ComprobanteId)
                    .OnDelete(DeleteBehavior.Cascade);

                // SQL Server no permite dos caminos de borrado en cascada hacia HistorialEstado
                // (Usuario -> Comprobante -> HistorialEstado y Usuario -> HistorialEstado).
                historial.HasOne(h => h.Usuario)
                    .WithMany()
                    .HasForeignKey(h => h.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

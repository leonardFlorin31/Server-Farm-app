using Microsoft.EntityFrameworkCore;
using Server_Licenta.Controllers;
using System.Data;

namespace Server_Licenta
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> User { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Polygon> Polygon { get; set; }
        public DbSet<PolygonPoint> PolygonPoints { get; set; }

        public DbSet<GrainParcelData> GrainParcelData { get; set; }

        public DbSet<AnimalParcelData> AnimalParcelData { get; set; }

        public DbSet<PolygonEntry> PolygonEntries { get; set; }

        public DbSet<Task> Tasks { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly set table names
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<Polygon>().ToTable("Polygon");
            modelBuilder.Entity<PolygonPoint>().ToTable("PolygonPoint");
            modelBuilder.Entity<PolygonEntry>().ToTable("PoligonEntries");

            // Configure PolygonPoint primary key
            modelBuilder.Entity<PolygonPoint>()
                .HasKey(pp => pp.PointId);

            // Configure PolygonPoint relationships
            modelBuilder.Entity<PolygonPoint>()
                .HasOne(pp => pp.Polygon)
                .WithMany(p => p.Points)
                .HasForeignKey(pp => pp.PolygonId);

            // Configure UserRole composite primary key
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure UserRole relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configure Polygon relationships
            modelBuilder.Entity<Polygon>()
        .HasMany(p => p.Points)
        .WithOne(pp => pp.Polygon)
        .HasForeignKey(pp => pp.PolygonId)
        .OnDelete(DeleteBehavior.Cascade);
            //15277e91-643b-4c66-88f2-53054d128b39 - keni : userid
            

            modelBuilder.Entity<PolygonEntry>()
       .ToTable("PolygonEntries")  // Asigură-te că numele tabelului este cel dorit
       .HasKey(pe => pe.PolygonEntryID);

            modelBuilder.Entity<Task>()
           .HasOne(t => t.CreatedByUser)
           .WithMany() // Un utilizator poate crea mai multe task-uri
           .HasForeignKey(t => t.CreatedByUserId)
           .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Task>()
                .HasOne(t => t.AssignedToUser)
                .WithMany() // Un utilizator poate avea mai multe task-uri asignate
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
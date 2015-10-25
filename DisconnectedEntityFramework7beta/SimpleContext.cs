using DisconnectedEntityFramework7beta.Model;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;

namespace DisconnectedEntityFramework7beta
{
    public class SimpleContext : DbContext
    {
        public SimpleContext(DbContextOptions options) : base(options)
        { }

        public DbSet<ParentEntity> ParentEntities { get; set; }
        public DbSet<ChildEntity> ChildEntities { get; set; }
        public DbSet<ChildEntityReferencingChildEntity> ChildEntityReferencingChildEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.ChildReferencingChildEntities);
            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.ChildEntities);

            modelBuilder.Entity<ChildEntityReferencingChildEntity>()
                .HasOne(cp => cp.ParentEntity);

            modelBuilder.Entity<ChildEntity>()
                .HasOne(sb => sb.ParentEntity);
            modelBuilder.Entity<ChildEntity>()
                .HasMany(sb => sb.ChildEntityReferencingChildEntities);

        }
    }
}

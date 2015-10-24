using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using DisconnectedEntityFramework.Model;

namespace DisconnectedEntityFramework
{
    public class SimpleContext : DbContext
    {
        public SimpleContext() : base("SimpleContext")
        {
        }

        public DbSet<ParentEntity> ParentEntities { get; set; }
        public DbSet<ChildEntity> ChildEntities { get; set; }
        public DbSet<ChildEntityReferencingChildEntity> ChildEntityReferencingChildEntities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.ChildReferencingChildEntities);
            modelBuilder.Entity<ParentEntity>()
                .HasMany(p => p.ChildEntities);

            modelBuilder.Entity<ChildEntityReferencingChildEntity>()
                .HasRequired(cp => cp.ParentEntity);
            modelBuilder.Entity<ChildEntityReferencingChildEntity>()
                .HasOptional(cp => cp.ChildEntity);

            modelBuilder.Entity<ChildEntity>()
                .HasRequired(sb => sb.ParentEntity);
            modelBuilder.Entity<ChildEntity>()
                .HasMany(sb => sb.ChildEntityReferencingChildEntities);
        }
    }
}

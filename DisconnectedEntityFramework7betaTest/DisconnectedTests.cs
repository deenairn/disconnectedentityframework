using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DisconnectedEntityFramework7beta;
using DisconnectedEntityFramework7beta.Model;
using Microsoft.Data.Entity;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace DisconnectedEntityFrameworkTest
{
    [TestFixture]
    public class DisconnectedTests
    {
        public enum DatabaseType
        {
            InMemory,
            Sqlite,
            SqlServer
        }

        [Test, TestCase(DatabaseType.InMemory), TestCase(DatabaseType.Sqlite), TestCase(DatabaseType.SqlServer)]
        public void FullySetupRelationshipsTest(DatabaseType databaseType)
        {
            // Arrange
            // Create untracked entities equivaelent to our collection
            // and then attempt to save and check list
            var childEntities = new List<ChildEntity>
                {
                    new ChildEntity {Id = 1, Name = "ChildEntity 1", ParentEntityId = 1},
                    new ChildEntity {Id = default(long), Name = "ChildEntity 3", ParentEntityId = 1}
                };

            var childEntityReferencingChildEntities = new List<ChildEntityReferencingChildEntity>
                {
                    new ChildEntityReferencingChildEntity
                    {
                        Id = 1,
                        Name = "ChildEntityReferencingChildEntity 1",
                        ChildEntityId = 1,
                        ChildEntity = childEntities.Single(x => x.Id == 1),
                        ParentEntityId = 1
                    },
                    new ChildEntityReferencingChildEntity
                    {
                        Id = default(long),
                        Name = "ChildEntityReferencingChildEntity 3",
                        ChildEntityId = default(long),
                        ChildEntity = childEntities.Last(), // untracked and not yet added
                        ParentEntityId = 1
                    }
                };

            // If this relationship is already established then the entities must be added
            childEntities.First().ChildEntityReferencingChildEntities =
                childEntityReferencingChildEntities.Where(x => x.ChildEntityId == 1).ToList();

            // GraphDiff cannot handle this situation where a collection is added
            // Must have an existing Id or else EF fails.
            //childEntities.Last().ChildEntityReferencingChildEntities =
            //    childEntityReferencingChildEntities.Where(x => x.ChildEntityId == default(long)).ToList();

            var parentEntity = new ParentEntity
            {
                Id = 1,
                Name = "ParentEntity 1",
                ChildEntities = childEntities.Where(x => x.ParentEntityId == 1).ToList(),
                ChildReferencingChildEntities =
                    childEntityReferencingChildEntities.Where(x => x.ParentEntityId == 1).ToList()
            };

            // act
            var optionsBuilder = new DbContextOptionsBuilder<SimpleContext>();
            switch (databaseType)
            {
                case DatabaseType.InMemory:
                    optionsBuilder.UseInMemoryDatabase();
                    break;
                case DatabaseType.Sqlite:
                    var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "test.db" };
                    var connectionString = connectionStringBuilder.ToString();
                    var connection = new SqliteConnection(connectionString);
                    optionsBuilder.UseSqlite(connection);

                    // Need to load in the Sqlite assemblies - overload assembly resolver
                    // http://stackoverflow.com/questions/7264383/options-for-using-system-data-sqlite-in-a-32bit-and-64bit-c-sharp-world
                    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                    {
                        if (args.Name == "Sqlite3")
                        {
                            var path = Path.Combine(@"..\..\..\Lib\Sqlite3\", "Native");
                            path = Path.Combine(path, Environment.Is64BitProcess ? @"x64\" : @"x32\");
                            path = Path.Combine(path, "Sqlite3.dll");
                            return Assembly.LoadFrom(path);
                        }
                        return null;
                    };
                    break;

                case DatabaseType.SqlServer:
                    optionsBuilder.UseSqlServer(@"Server=(LocalDB)\MSSQLLocalDB;Database=Test;integrated security=True;");
                    break;
            }

            using (var context = new SimpleContext(optionsBuilder.Options))
            {
                // To ensure contents are empty delete contents for Sqlite
                context.Database.EnsureDeleted();

                // Ensure DB created
                context.Database.EnsureCreated();

                SimpleInitializer.Seed(context);
            } // must be seeded with a separate context or already added to graph

            using (var context = new SimpleContext(optionsBuilder.Options))
            {
                context.Update(parentEntity);

                context.SaveChanges();
            }

            // assert
            using (var context = new SimpleContext(optionsBuilder.Options))
            {
                var persistedParentEntity = 
                    context.ParentEntities
                        .Include(x => x.ChildEntities)
                        .Include(x => x.ChildReferencingChildEntities)
                        .Single(x => x.Id == parentEntity.Id);

                Assert.That(persistedParentEntity.Id, Is.EqualTo(parentEntity.Id));
                Assert.That(persistedParentEntity.Name, Is.EqualTo(parentEntity.Name));
                Assert.That(persistedParentEntity.ChildEntities.Count, Is.EqualTo(parentEntity.ChildEntities.Count));
                Assert.That(persistedParentEntity.ChildReferencingChildEntities.Count,
                    Is.EqualTo(parentEntity.ChildReferencingChildEntities.Count));

                // Require to order by as EF 7 orders in the opposite order
                var zippedChildren =
                    persistedParentEntity.ChildEntities.OrderBy(x => x.Id).Zip(parentEntity.ChildEntities,
                        (persisted, entity) => new {Persisted = persisted, Entity = entity});

                foreach (var persistedAndEntity in zippedChildren)
                {
                    Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    Assert.That(persistedAndEntity.Persisted.ParentEntityId, Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                }

                var zippedChildReferencingChildren =
                    persistedParentEntity.ChildReferencingChildEntities.OrderBy(x => x.Id).Zip(
                        parentEntity.ChildReferencingChildEntities,
                        (persisted, entity) => new {Persisted = persisted, Entity = entity});

                foreach (var persistedAndEntity in zippedChildReferencingChildren)
                {
                    Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    Assert.That(persistedAndEntity.Persisted.ParentEntityId, Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                    Assert.That(persistedAndEntity.Persisted.ChildEntityId, Is.EqualTo(
                            persistedAndEntity.Entity.ChildEntityId == default(long) ?
                                3 : persistedAndEntity.Entity.ChildEntityId)); // should be added following ef graph merge
                }
            }
        }
    }
}

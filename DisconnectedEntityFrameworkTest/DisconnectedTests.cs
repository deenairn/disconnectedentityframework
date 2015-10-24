using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DisconnectedEntityFramework;
using DisconnectedEntityFramework.Model;
using NUnit.Framework;

namespace DisconnectedEntityFrameworkTest
{
    [TestFixture]
    public class DisconnectedTests
    {
        [Test]
        public void FullySetupRelationshipsTest()
        {
            // Arrange
            // Create untracked entities equivaelent to our collection
            // and then attempt to save and check list
            var childEntities = new List<ChildEntity>
                {
                    new ChildEntity {Id = 1, Name = "ChildEntity 1", ParentEntityId = 1},
                    //new ChildEntity {Id = 2, Name = "ChildEntity 2", ParentEntityId = 2},
                    new ChildEntity {Name = "ChildEntity 3", ParentEntityId = 1}
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
                        Name = "ChildEntityReferencingChildEntity 3",
                        ChildEntityId = default(long),
                        ChildEntity = childEntities.Last(), // untracked and not yet added
                        ParentEntityId = 1
                    }
                };

            var parentEntity = new ParentEntity
            {
                Id = 1,
                Name = "ParentEntity 1",
                ChildEntities = childEntities.Where(x => x.Id == default(long) || x.Id == 1).ToList(),
                ChildReferencingChildEntities =
                    childEntityReferencingChildEntities.Where(x => x.Id == default(long) || x.Id == 1).ToList()
            };

            // act
            using (var context = new SimpleContext())
            {
                var entry = context.Entry(parentEntity);
                entry.State = EntityState.Modified;

                context.SaveChanges();
            }

            // assert
            using (var context = new SimpleContext())
            {
                context.Configuration.LazyLoadingEnabled = false;
                var persistedParentEntity = context.ParentEntities.Find(parentEntity.Id);

                context.Entry(persistedParentEntity)
                       .Collection(x => x.ChildEntities).Load();

                context.Entry(persistedParentEntity)
                    .Collection(x => x.ChildReferencingChildEntities).Load();


                Assert.That(persistedParentEntity, Is.EqualTo(parentEntity));
            }
        }
    }
}

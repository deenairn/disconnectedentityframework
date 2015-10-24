using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DisconnectedEntityFramework;
using DisconnectedEntityFramework.Model;
using NUnit.Framework;
using RefactorThis.GraphDiff;

namespace DisconnectedEntityFrameworkTest
{
    [TestFixture]
    public class DisconnectedTests
    {
        [Test, TestCase(true), TestCase(false)]
        public void FullySetupRelationshipsTest(bool useGraphDiff)
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
                ChildEntities = childEntities.Where(x => x.Id == default(long) || x.Id == 1).ToList(),
                ChildReferencingChildEntities =
                    childEntityReferencingChildEntities.Where(x => x.Id == default(long) || x.Id == 1).ToList()
            };

            // act
            using (var context = new SimpleContext())
            {
                // force re-seed database every time
                context.Database.Initialize(true);

                if (useGraphDiff)
                {
                    context.UpdateGraph(parentEntity);
                }

                context.SaveChanges();
            }

            // assert
            using (var context = new SimpleContext())
            {
                var persistedParentEntity = context.ParentEntities.Find(parentEntity.Id);

                context.Entry(persistedParentEntity)
                       .Collection(x => x.ChildEntities).Load();

                context.Entry(persistedParentEntity)
                    .Collection(x => x.ChildReferencingChildEntities).Load();

                Assert.That(persistedParentEntity.Id, Is.EqualTo(parentEntity.Id));
                Assert.That(persistedParentEntity.Name, Is.EqualTo(parentEntity.Name));
                Assert.That(persistedParentEntity.ChildEntities.Count, Is.EqualTo(parentEntity.ChildEntities.Count));
                Assert.That(persistedParentEntity.ChildReferencingChildEntities.Count,
                    Is.EqualTo(parentEntity.ChildReferencingChildEntities.Count));


                var zippedChildren = persistedParentEntity.ChildEntities.Zip(parentEntity.ChildEntities,
                    (persisted, entity) => new {Persisted = persisted, Entity = entity});

                foreach (var persistedAndEntity in zippedChildren)
                {
                    Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    Assert.That(persistedAndEntity.Persisted.ParentEntityId, Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                }

                var zippedChildReferencingChildren =
                    persistedParentEntity.ChildReferencingChildEntities.Zip(parentEntity.ChildReferencingChildEntities,
                        (persisted, entity) => new {Persisted = persisted, Entity = entity});

                foreach (var persistedAndEntity in zippedChildReferencingChildren)
                {
                    Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    Assert.That(persistedAndEntity.Persisted.ParentEntityId, Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                    Assert.That(persistedAndEntity.Persisted.ChildEntityId, Is.Not.Null); // should be added
                }
            }
        }
    }
}

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
            var parentEntity = Arrange();

            // act
            Act(parentEntity, useGraphDiff);

            // assert
            Assert(parentEntity, useGraphDiff);
        }

        private static ParentEntity Arrange()
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
            return parentEntity;
        }

        private static void Act(ParentEntity parentEntity, bool useGraphDiff)
        {
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
        }

        private static void Assert(ParentEntity parentEntity, bool useGraphDiff)
        {
            using (var context = new SimpleContext())
            {
                var persistedParentEntity = context.ParentEntities.Find(parentEntity.Id);

                context.Entry(persistedParentEntity)
                    .Collection(x => x.ChildEntities).Load();

                context.Entry(persistedParentEntity)
                    .Collection(x => x.ChildReferencingChildEntities).Load();

                NUnit.Framework.Assert.That(persistedParentEntity.Id, Is.EqualTo(parentEntity.Id));
                NUnit.Framework.Assert.That(persistedParentEntity.Name, Is.EqualTo(parentEntity.Name));
                NUnit.Framework.Assert.That(persistedParentEntity.ChildEntities.Count, 
                    (useGraphDiff) ? Is.EqualTo(parentEntity.ChildEntities.Count)
                        : Is.EqualTo(parentEntity.ChildEntities.Count(x => x.Id != default(long))));
                NUnit.Framework.Assert.That(persistedParentEntity.ChildReferencingChildEntities.Count,
                     (useGraphDiff) ? Is.EqualTo(parentEntity.ChildReferencingChildEntities.Count) 
                        : Is.EqualTo(parentEntity.ChildReferencingChildEntities.Count(x => x.Id != default(long))));


                // where not using graphdiff - compare that the existing entities are the same
                var zippedChildren = persistedParentEntity.ChildEntities.Zip(
                    (useGraphDiff) ? parentEntity.ChildEntities : parentEntity.ChildEntities.Where(x => x.Id != default(long)),
                        (persisted, entity) => new { Persisted = persisted, Entity = entity });

                foreach (var persistedAndEntity in zippedChildren)
                {
                    NUnit.Framework.Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    NUnit.Framework.Assert.That(persistedAndEntity.Persisted.ParentEntityId,
                        Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                }

                var zippedChildReferencingChildren =
                    persistedParentEntity.ChildReferencingChildEntities.Zip(
                        parentEntity.ChildReferencingChildEntities.Where(x => x.Id != default(long)),
                            (persisted, entity) => new { Persisted = persisted, Entity = entity });

                foreach (var persistedAndEntity in zippedChildReferencingChildren)
                {
                    NUnit.Framework.Assert.That(persistedAndEntity.Persisted.Name, Is.EqualTo(persistedAndEntity.Entity.Name));
                    NUnit.Framework.Assert.That(persistedAndEntity.Persisted.ParentEntityId,
                        Is.EqualTo(persistedAndEntity.Entity.ParentEntityId));
                    NUnit.Framework.Assert.That(persistedAndEntity.Persisted.ChildEntityId, 
                        Is.EqualTo(
                            persistedAndEntity.Entity.ChildEntityId == default(long) ?
                                3 : persistedAndEntity.Entity.ChildEntityId)); // should be added by graphdiff
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DisconnectedEntityFramework7beta.Model;

namespace DisconnectedEntityFramework7beta
{
    public static class SimpleInitializer
    {
        public static void Seed(SimpleContext context)
        {
            var parentEntities = new List<ParentEntity>
            {
                new ParentEntity {Name = "ParentEntity 1"},
                new ParentEntity {Name = "ParentEntity 2"}
            };

            context.ParentEntities.AddRange(parentEntities);

            var childEntities = new List<ChildEntity>
            {
                new ChildEntity { Name = "ChildEntity 1", ParentEntity = parentEntities.First() },
                new ChildEntity { Name = "ChildEntity 2", ParentEntity = parentEntities.Last() }
            };

            context.ChildEntities.AddRange(childEntities);

            var childEntityReferencingChildEntities = new List<ChildEntityReferencingChildEntity>
            {
                new ChildEntityReferencingChildEntity
                {
                    Name = "ChildEntityReferencingChildEntity 1",
                    ParentEntity = parentEntities.First(),
                    ChildEntity = childEntities.First()
                },
                new ChildEntityReferencingChildEntity
                {
                    Name = "ChildEntityReferencingChildEntity 2",
                    ParentEntity = parentEntities.Last(),
                    ChildEntity = childEntities.Last()
                }
            };

            context.ChildEntityReferencingChildEntities.AddRange(childEntityReferencingChildEntities);

            context.SaveChanges();
        }
    }
}

using System.Collections.Generic;
using RefactorThis.GraphDiff.Attributes;

namespace DisconnectedEntityFramework.Model
{
    public class ChildEntity : NamedEntity
    {
        public long ParentEntityId { get; set; }

        public ParentEntity ParentEntity { get; set; }

        [Associated]
        public ICollection<ChildEntityReferencingChildEntity> ChildEntityReferencingChildEntities { get; set; }
    }
}

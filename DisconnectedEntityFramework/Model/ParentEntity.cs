using System.Collections.Generic;
using RefactorThis.GraphDiff.Attributes;

namespace DisconnectedEntityFramework.Model
{
    public class ParentEntity : NamedEntity
    {
        [Owned]
        public ICollection<ChildEntityReferencingChildEntity> ChildReferencingChildEntities { get; set; }

        [Owned]
        public ICollection<ChildEntity> ChildEntities { get; set; }
    }
}

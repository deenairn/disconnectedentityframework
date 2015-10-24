using System.Collections.Generic;

namespace DisconnectedEntityFramework7beta.Model
{
    public class ChildEntity : NamedEntity
    {
        public long ParentEntityId { get; set; }

        public ParentEntity ParentEntity { get; set; }

        public ICollection<ChildEntityReferencingChildEntity> ChildEntityReferencingChildEntities { get; set; }
    }
}

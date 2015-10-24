using System.Collections.Generic;

namespace DisconnectedEntityFramework.Model
{
    public class ChildEntityReferencingChildEntity : NamedEntity
    {
        public long ParentEntityId { get; set; }

        public ParentEntity ParentEntity { get; set; }

        public long? ChildEntityId { get; set; }

        public ChildEntity ChildEntity { get; set; }
    }
}

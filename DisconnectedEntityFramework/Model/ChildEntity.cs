using System.Collections.Generic;

namespace DisconnectedEntityFramework.Model
{
    public class ChildEntity : NamedEntity
    {
        public long ParentEntityId { get; set; }

        public ParentEntity ParentEntity { get; set; }

        public ICollection<ChildEntityReferencingChildEntity> CustomerProfiles { get; set; }
    }
}

using System.Collections.Generic;

namespace DisconnectedEntityFramework7beta.Model
{
    public class ParentEntity : NamedEntity
    {
        public ICollection<ChildEntityReferencingChildEntity> ChildReferencingChildEntities { get; set; }

        public ICollection<ChildEntity> ChildEntities { get; set; }
    }
}

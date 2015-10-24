using System.Collections.Generic;

namespace DisconnectedEntityFramework.Model
{
    public class ParentEntity : NamedEntity
    {
        public ICollection<ChildEntityReferencingChildEntity> ChildReferencingChildEntities { get; set; }

        public ICollection<ChildEntity> ChildEntities { get; set; }
    }
}

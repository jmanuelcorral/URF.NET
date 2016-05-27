using System.ComponentModel.DataAnnotations.Schema;
using URF.Core.Infrastructure;

namespace URF.EntityFramework
{
    public abstract class Entity : IObjectState
    {
        [NotMapped]
        public ObjectState ObjectState { get; set; }
    }
}

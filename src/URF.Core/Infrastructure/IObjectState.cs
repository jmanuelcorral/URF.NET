using System.ComponentModel.DataAnnotations.Schema;

namespace URF.Core.Infrastructure
{
    public interface IObjectState
    {
        [NotMapped]
        ObjectState ObjectState { get; set; }
    }
}
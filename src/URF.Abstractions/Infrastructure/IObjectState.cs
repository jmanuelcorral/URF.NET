using System.ComponentModel.DataAnnotations.Schema;

namespace URF.Abstractions.Infrastructure
{
    public interface IObjectState
    {
        [NotMapped]
        ObjectState ObjectState { get; set; }
    }
}
using System;
using URF.Abstractions.Infrastructure;

namespace URF.Abstractions.DataContext
{
    public interface IDataContext : IDisposable
    {
        int SaveChanges();
#if COREFX
        int SaveChanges(bool acceptAllChangesOnSuccess);
#endif
        void SyncObjectState<TEntity>(TEntity entity) where TEntity : class, IObjectState;
        void SyncObjectsStatePostCommit();
    }
}
using System;
using System.Data;
using URF.Core.Infrastructure;
using URF.Core.Repositories;

namespace URF.Core.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        int SaveChanges();
#if COREFX
        int SaveChanges(bool acceptAllChangesOnSuccess);
#endif
        IRepository<TEntity> Repository<TEntity>() where TEntity : class, IObjectState;
        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        bool Commit();
        void Rollback();
    }
}
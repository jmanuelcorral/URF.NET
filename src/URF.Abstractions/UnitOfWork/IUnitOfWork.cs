using System;
using System.Data;
using URF.Abstractions.Infrastructure;
using URF.Abstractions.Repositories;

namespace URF.Abstractions.UnitOfWork
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if COREFX
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using Microsoft.Practices.ServiceLocation;
#endif
using URF.Core.DataContext;
using URF.Core.Repositories;
using URF.Core.UnitOfWork;
using URF.Core.Infrastructure;

namespace URF.EntityFramework
{
    public class UnitOfWork : IUnitOfWorkAsync
    {

#if COREFX
        private readonly IServiceProvider _serviceProvider;
        private IDbContextTransaction _transaction;
        private DbContext _dbContext;
#else
        private IDbTransaction _transaction;
        private ObjectContext _objectContext;
#endif
        private IDataContextAsync _dataContext;
        private Dictionary<string, object> _repositories;
        private bool _disposed;

#if COREFX
        public UnitOfWork(IDataContextAsync dataContext, IServiceProvider serviceProvider)
#else
        public UnitOfWork(IDataContextAsync dataContext)
#endif
        {
#if COREFX
            _serviceProvider = serviceProvider;
#endif
            _dataContext = dataContext;
            _repositories = new Dictionary<string, object>();
        }

        public int SaveChanges() => _dataContext.SaveChanges();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) => _dataContext.SaveChangesAsync(cancellationToken);

#if COREFX
        public int SaveChanges(bool acceptAllChangesOnSuccess) => _dataContext.SaveChanges(acceptAllChangesOnSuccess);
        public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) => _dataContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
#endif

        public IRepository<TEntity> Repository<TEntity>() where TEntity : class, IObjectState
        {
            IRepository<TEntity> repository= null;
#if COREFX
            repository = _serviceProvider?.GetService(typeof(IRepository<TEntity>)) as IRepository<TEntity>;
#else
            if (ServiceLocator.IsLocationProviderSet)
            {
                repository = ServiceLocator.Current.GetInstance<IRepository<TEntity>>();
            }
#endif
            return repository != null ? (IRepository<TEntity>)repository : RepositoryAsync<TEntity>();
        }

        public IRepositoryAsync<TEntity> RepositoryAsync<TEntity>() where TEntity : class, IObjectState
        {
            IRepositoryAsync<TEntity> repository = null;

#if COREFX
            repository = _serviceProvider?.GetService(typeof(IRepositoryAsync<TEntity>)) as IRepositoryAsync<TEntity>;
            if (repository != null)
            {
                return (IRepositoryAsync<TEntity>)repository;
            }
#else
            if (ServiceLocator.IsLocationProviderSet)
            {
                repository = ServiceLocator.Current.GetInstance<IRepositoryAsync<TEntity>>();
                if (repository != null)
                {
                    return (IRepositoryAsync<TEntity>)repository;
                }
            }
#endif
            if (_repositories == null)
            {
                _repositories = new Dictionary<string, object>();
            }

            var type = typeof(TEntity).Name;

            if (_repositories.ContainsKey(type))
            {
                return (IRepositoryAsync<TEntity>)_repositories[type];
            }

            var repositoryType = typeof(Repository<>);

            _repositories.Add(type, Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _dataContext, this));

            return (IRepositoryAsync<TEntity>)_repositories[type];
        }

        public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
#if COREFX
            _dbContext = (DbContext)_dataContext;
            _transaction = _dbContext.Database.BeginTransaction();
#else
            _objectContext = ((IObjectContextAdapter)_dataContext).ObjectContext;
            if (_objectContext.Connection.State != ConnectionState.Open)
            {
                _objectContext.Connection.Open();
            }

            _transaction = _objectContext.Connection.BeginTransaction(isolationLevel);
#endif
        }

        public bool Commit()
        {
            _transaction.Commit();
            return true;
        }

        public void Rollback()
        {
            _transaction.Rollback();
            _dataContext.SyncObjectsStatePostCommit();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
#if COREFX
                        if (_dbContext != null)
                        {
                            _dbContext.Dispose();
                            _dbContext = null;
                        }
#else
                        if (_objectContext != null)
                        {
                            if (_objectContext.Connection.State == ConnectionState.Open)
                                _objectContext.Connection.Close();

                            _objectContext.Dispose();
                            _objectContext = null;
                        }
#endif

                        if (_dataContext != null)
                        {
                            _dataContext.Dispose();
                            _dataContext = null;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // do nothing, the objectContext has already been disposed
                    }

                    if (_repositories != null)
                        _repositories = null;
                }

                _disposed = true;
            }
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}

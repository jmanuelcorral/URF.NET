using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
#if COREFX
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
using LinqKit;
#endif
using URF.Core.DataContext;
using URF.Core.Infrastructure;
using URF.Core.Repositories;
using URF.Core.UnitOfWork;

namespace URF.EntityFramework
{
    public class Repository<TEntity> : IRepositoryAsync<TEntity> where TEntity : class, IObjectState
    {
        private readonly IDataContextAsync _context;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(IDataContextAsync context, IUnitOfWorkAsync unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;

            var dbContext = context as DbContext;

            if (dbContext != null)
            {
                _dbSet = dbContext.Set<TEntity>();
            }
#if !COREFX
            else
            {
                var fakeContext = context as FakeDbContext;

                if (fakeContext != null)
                {
                    _dbSet = fakeContext.Set<TEntity>();
                }
            }
#endif
        }

        IRepository<T> IRepository<TEntity>.GetRepository<T>() => _unitOfWork.Repository<T>();
        public IQueryable<TEntity> Queryable() => _dbSet;


        public virtual TEntity Find(params object[] keyValues) => _dbSet.Find(keyValues);

        public virtual Task<TEntity> FindAsync(params object[] keyValues) => _dbSet.FindAsync(default(CancellationToken), keyValues);

        public virtual Task<TEntity> FindAsync(CancellationToken cancellationToken, params object[] keyValues) => _dbSet.FindAsync(cancellationToken, keyValues);

        public virtual void Insert(TEntity entity)
        {
            entity.ObjectState = ObjectState.Added;
            _dbSet.Attach(entity);
            _context.SyncObjectState(entity);
        }

        public virtual void InsertRange(IEnumerable<TEntity> entities)
        {
            foreach (var item in entities)
            {
                Insert(item);
            }
        }

        public virtual void InsertGraphRange(IEnumerable<TEntity> entities)
        {
            _dbSet.AddRange(entities);
        }

        public virtual void Delete(TEntity entity)
        {
            entity.ObjectState = ObjectState.Deleted;
            _dbSet.Attach(entity);
            _context.SyncObjectState(entity);
        }

        public virtual void Delete(object id)
        {
            var entity = _dbSet.Find(id);
            Delete(entity);
        }

        public virtual Task<bool> DeleteAsync(params object[] keyValues) => DeleteAsync(default(CancellationToken), keyValues);

        public virtual async Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keyValues)
        {
            var entity = await FindAsync(cancellationToken, keyValues);

            if (entity == null)
            {
                return false;
            }

            entity.ObjectState = ObjectState.Deleted;
            _dbSet.Attach(entity);

            return true;
        }

        public virtual void UpsertGraph(TEntity entity)
        {
            SyncObjectGraph(entity);
            _entitesChecked = null;
            _dbSet.Attach(entity);
        }

        public virtual void Update(TEntity entity)
        {
            entity.ObjectState = ObjectState.Modified;
            _dbSet.Attach(entity);
            _context.SyncObjectState(entity);
        }

#if !COREFX
        public IQueryFluent<TEntity> Query()
        {
            return new QueryFluent<TEntity>(this);
        }

        public IQueryFluent<TEntity> Query(Expression<Func<TEntity, bool>> query)
        {
            return new QueryFluent<TEntity>(this, query);
        }

        public IQueryFluent<TEntity> Query(IQueryObject<TEntity> queryObject)
        {
            return new QueryFluent<TEntity>(this, queryObject);
        }

        public IQueryable<TEntity> SelectQuery(string query, params object[] parameters)
        {
            return _dbSet.SqlQuery(query, parameters).AsQueryable();
        }

        internal IQueryable<TEntity> Select(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            List<Expression<Func<TEntity, object>>> includes = null,
            int? page = null,
            int? pageSize = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            if (filter != null)
            {
                query = query.AsExpandable().Where(filter);
            }
            if (page != null && pageSize != null)
            {
                query = query.Skip((page.Value - 1)*pageSize.Value).Take(pageSize.Value);
            }
            return query;
        }

        internal async Task<IEnumerable<TEntity>> SelectAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            List<Expression<Func<TEntity, object>>> includes = null,
            int? page = null,
            int? pageSize = null)
        {
            return await Select(filter, orderBy, includes, page, pageSize).ToListAsync();
        }
#endif

        // tracking of all processed entities in the object graph when calling SyncObjectGraph
        HashSet<object> _entitesChecked;

        private void SyncObjectGraph(object entity) // scan object graph for all 
        {
            // instantiating _entitesChecked so we can keep track of all entities we have scanned, avoid any cyclical issues
            if (_entitesChecked == null)
                _entitesChecked = new HashSet<object>();

            // if already processed skip
            if (_entitesChecked.Contains(entity))
                return;

            // add entity to alreadyChecked collection
            _entitesChecked.Add(entity);

            var objectState = entity as IObjectState;

            // discovered entity with ObjectState.Added, sync this with provider e.g. EF
            if (objectState != null && objectState.ObjectState == ObjectState.Added)
                _context.SyncObjectState((IObjectState)entity);

            // Set tracking state for child collections
            foreach (var prop in entity.GetType().GetProperties())
            {
                // Apply changes to 1-1 and M-1 properties
                var trackableRef = prop.GetValue(entity, null) as IObjectState;
                if (trackableRef != null)
                {
                    // discovered entity with ObjectState.Added, sync this with provider e.g. EF
                    if (trackableRef.ObjectState == ObjectState.Added)
                        _context.SyncObjectState((IObjectState)entity);

                    // recursively process the next property
                    SyncObjectGraph(prop.GetValue(entity, null));
                }

                // Apply changes to 1-M properties
                var items = prop.GetValue(entity, null) as IEnumerable<IObjectState>;

                // collection was empty, nothing to process, continue
                if (items == null) continue;

                // collection isn't empty, continue to recursively scan the elements of this collection
                foreach (var item in items)
                    SyncObjectGraph(item);
            }
        }
    }
}

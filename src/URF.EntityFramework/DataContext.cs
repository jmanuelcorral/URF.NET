using System;
using System.Threading;
using System.Threading.Tasks;
#if COREFX
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using URF.Core.DataContext;
using URF.Core.Infrastructure;

namespace URF.EntityFramework
{
    public class DataContext : DbContext, IDataContextAsync
    {
#if COREFX
        public DataContext() : this(new DbContextOptions<DataContext>())
        {
        }

        public DataContext(DbContextOptions options) : base(options)
        {
            InstanceId = Guid.NewGuid();
        }
#else
        public DataContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            InstanceId = Guid.NewGuid();
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }
#endif

        public Guid InstanceId { get; }

        public void SyncObjectState<TEntity>(TEntity entity) where TEntity : class, IObjectState
        {
            Entry(entity).State = entity.ObjectState.ToEntityState();
        }

        public override int SaveChanges()
        {
            SyncObjectsStatePreCommit();
            int changes = base.SaveChanges();
            SyncObjectsStatePostCommit();
            return changes;
        }

#if COREFX
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            SyncObjectsStatePreCommit();
            int changes = base.SaveChanges(acceptAllChangesOnSuccess);
            SyncObjectsStatePostCommit();
            return changes;
        }
#endif

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SyncObjectsStatePreCommit();
            int changes = await base.SaveChangesAsync(cancellationToken);
            SyncObjectsStatePostCommit();
            return changes;
        }

#if COREFX
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyncObjectsStatePreCommit();
            int changes = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            SyncObjectsStatePostCommit();
            return changes;
        }
#endif

        public void SyncObjectsStatePostCommit()
        {
            foreach (var dbEntityEntry in ChangeTracker.Entries())
            {
                ((IObjectState)dbEntityEntry.Entity).ObjectState = dbEntityEntry.State.ToObjectState();
            }
        }

        private void SyncObjectsStatePreCommit()
        {
            foreach (var dbEntityEntry in ChangeTracker.Entries())
            {
                dbEntityEntry.State = ((IObjectState)dbEntityEntry.Entity).ObjectState.ToEntityState();
            }
        }
    }
}
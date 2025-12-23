using System.Collections.Concurrent;

namespace NinthBall.Core
{
    internal sealed class ObjectPool<T>(Func<T> factory, int MaxItems = 100) where T : class
    {
        private readonly ConcurrentQueue<T> InstancePool = new();

        // Return an instance from the pool. Create new instance if none available in the pool.
        public Lease Rent() => new Lease(this, InstancePool.TryDequeue(out var item) ? item : factory());

        private void Return(T item)
        {
            // Throw away the instance if too many in the pool
            if (InstancePool.Count < MaxItems) InstancePool.Enqueue(item);
        }

        public readonly struct Lease(ObjectPool<T> pool, T instance) : IDisposable
        {
            public T Instance { get; } = instance;
            public void Dispose() => pool.Return(Instance);
        }
    }
}

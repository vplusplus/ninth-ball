using System.Collections.Concurrent;

namespace NinthBall
{
    public sealed class ObjectPool<T>(Func<T> factory) where T : class
    {
        private readonly ConcurrentQueue<T> _pool = new();

        public Lease Rent() => new Lease(this, _pool.TryDequeue(out var item) ? item : factory());

        private void Return(T item) => _pool.Enqueue(item);

        public readonly struct Lease(ObjectPool<T> pool, T instance) : IDisposable
        {
            public T Instance { get; } = instance;
            public void Dispose() => pool.Return(Instance);
        }
    }
}

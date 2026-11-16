using System;
using System.Collections.Generic;

namespace Bosque.World
{
    public sealed class RestorationProgress
    {
        private readonly HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal);
        public RestorationProgress(IEnumerable<string> obstacleIds)
        {
            if (obstacleIds == null) throw new ArgumentNullException(nameof(obstacleIds));
            foreach (var id in obstacleIds)
                if (string.IsNullOrWhiteSpace(id) || !expected.Add(id))
                    throw new ArgumentException("Los IDs deben ser únicos y no vacíos.");
            if (expected.Count == 0) throw new ArgumentException("Se necesita al menos un obstáculo.");
        }
        public int RemovedCount => removed.Count;
        public int TotalCount => expected.Count;
        public bool IsRestored => removed.Count == expected.Count;
        public bool IsCompleted { get; private set; }
        public event Action Changed;
        public event Action Restored;
        public bool IsRemoved(string id) => id != null && removed.Contains(id);
        public bool TryRemove(string id)
        {
            if (id == null || !expected.Contains(id) || !removed.Add(id)) return false;
            Changed?.Invoke();
            if (IsRestored) Restored?.Invoke();
            return true;
        }
        public bool TryComplete()
        {
            if (!IsRestored || IsCompleted) return false;
            IsCompleted = true;
            Changed?.Invoke();
            return true;
        }
        public void Reset()
        {
            removed.Clear(); IsCompleted = false;
            Changed?.Invoke();
        }
    }
}

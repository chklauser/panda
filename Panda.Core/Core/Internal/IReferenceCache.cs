using System;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    /// <summary>
    /// A cache that only serves to keep objects alive (prevent them from being garbage collected). This is useful when combined with a <see cref="WeakReference{T}"/> dictionary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReferenceCache<in T> where T : class
    {
        void RegisterAccess(T reference);
        void EvictEarly(T reference);
    }
}
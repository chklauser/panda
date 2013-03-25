namespace Panda.Core.Internal
{
    /// <summary>
    /// An object that designates a dedicated <see cref="CacheKey"/> to be used in object caches.
    /// </summary>
    /// <typeparam name="T">The type of key, must implement Equals and GetHashCode.</typeparam>
    /// <remarks>This interface is best implemented explicitly so to not pollute the public API with technical members like <see cref="CacheKey"/>.</remarks>
    public interface ICacheKeyed<out T>
    {
        /// <summary>
        /// The key to use for caching.
        /// </summary>
        T CacheKey { get; }
    }
}
using System;

namespace Panda.Core.IO
{
    /// <summary>
    /// Raw pointer-based persistence space.
    /// </summary>
    public interface IRawPersistenceSpace : IPersistenceSpace
    {
        /// <summary>
        /// A raw pointer to the beginning of the persistence space.
        /// </summary>
        /// <remarks>
        ///     <para>Be careful to not access anything at or beyond <code><see cref="Pointer"/>+<see cref="IPersistenceSpace.Capacity"/></code>.</para>
        ///     <para>Resizing may cause the pointer to change. Do not keep copies of the pointer across calls to <see cref="IPersistenceSpace.Resize"/> or 
        /// beyond the lifetime of this <see cref="IRawPersistenceSpace"/>.</para>
        /// </remarks>
        unsafe void* Pointer { get; }
    }
}
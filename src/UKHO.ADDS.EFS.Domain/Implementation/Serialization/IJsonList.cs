namespace UKHO.ADDS.EFS.Domain.Implementation.Serialization
{
    /// <summary>
    ///     Minimal contract for a "list wrapper" that wants to serialize as a bare JSON array.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    internal interface IJsonList<T>
    {
        /// <summary>
        ///     The read-only view of the elements.
        /// </summary>
        IReadOnlyList<T> Items { get; }

        /// <summary>
        ///     Add an item (used during deserialization).
        /// </summary>
        void Add(T item);

        /// <summary>
        ///     Clear all items (used for rehydration if necessary).
        /// </summary>
        void Clear();
    }
}

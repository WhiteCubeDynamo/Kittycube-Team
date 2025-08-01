namespace StealthHeist.Core.Interfaces
{
    /// <summary>
    /// Defines a contract for objects that can have their state saved and restored across game loops.
    /// </summary>
    public interface IPersistent
    {
        /// <summary>
        /// A unique ID for this object instance in the scene. Must be set manually in the Inspector.
        /// </summary>
        string PersistenceID { get; }

        /// <summary>
        /// Captures the current state of the object to be saved.
        /// </summary>
        /// <returns>An object representing the state (e.g., a bool, a struct).</returns>
        object CaptureState();

        /// <summary>
        /// Restores the object's state from a previously saved state.
        /// </summary>
        /// <param name="state">The state object to restore from.</param>
        void RestoreState(object state);
    }
}
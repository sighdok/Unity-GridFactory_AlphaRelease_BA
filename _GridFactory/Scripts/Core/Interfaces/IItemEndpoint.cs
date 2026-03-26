using GridFactory.Directions;

namespace GridFactory.Core
{
    /// <summary>
    /// Endpunkt für Items (Maschine oder Conveyor)
    /// Wird vom Crossing verwendet, um Items zu routen.
    /// </summary>
    public interface IItemEndpoint
    {
        Item CurrentItem { get; set; }

        /// <summary>
        /// Richtung, aus der aus Sicht dieses Endpunkts Items ankommen.
        /// </summary>
        Direction InputDirection { get; }

        /// <summary>
        /// Richtung, in die aus Sicht dieses Endpunkts Items weitergegeben werden.
        /// </summary>
        Direction OutputDirection { get; }
    }
}

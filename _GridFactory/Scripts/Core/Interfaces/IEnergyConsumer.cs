namespace GridFactory.Core
{
    public interface IEnergyConsumer
    {
        /// <summary>
        /// Energiebedarf pro Tick 
        /// </summary>
        float DemandPerTick { get; }
    }
}
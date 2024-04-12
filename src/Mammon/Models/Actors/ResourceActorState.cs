namespace MammonActors.Models.Actors
{
    public class ResourceActorState
    {
        public double Cost { get; set; }
        public Dictionary<string, double> CostItems { get; set; } = [];
    }
}

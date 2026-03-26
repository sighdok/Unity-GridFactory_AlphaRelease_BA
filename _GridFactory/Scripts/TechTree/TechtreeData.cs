using GridFactory.Core;

namespace GridFactory.Tech
{
    public class ResearchItemCounter
    {
        public Item item;
        public int current;
        public int target;
        public bool completed;

        public ResearchItemCounter(Item item, int targetAmount)
        {
            this.item = item;
            this.target = targetAmount;
            this.current = 0;
            this.completed = false;
        }
    }
}

using System;

namespace GridFactory.Core
{
    [Serializable]
    public class ResearchItem
    {
        public Item item;
        public int amount = 0;
    }

    [Serializable]
    public class RecipeItem
    {
        public Item item;
        public int amount = 0;
    }

    public class RecipeItemCounter
    {
        public Item item;
        public int current;
        public int target;
        public bool completed;

        public RecipeItemCounter(Item item, int targetAmount)
        {
            this.item = item;
            this.target = targetAmount;
            this.current = 0;
            this.completed = false;
        }
    }
}
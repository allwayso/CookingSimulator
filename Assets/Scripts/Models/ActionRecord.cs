using System;

namespace CookingSimulator.Models
{
    [Serializable]
    public class ActionRecord
    {
        public string action;
        public string target;
        public float elapsedSeconds;
        public string stage;
        public DishState stateBefore;
        public DishState stateAfter;
    }
}

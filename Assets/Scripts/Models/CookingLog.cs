using System;
using System.Collections.Generic;

namespace CookingSimulator.Models
{
    [Serializable]
    public class CookingLog
    {
        public string logId;
        public string userId;
        public string dishId;
        public string recipeId;
        public string startedAt;
        public string finishedAt;
        public DishState finalState;
        public List<ActionRecord> records = new List<ActionRecord>();
    }
}

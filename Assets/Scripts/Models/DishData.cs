using System;
using System.Collections.Generic;

namespace CookingSimulator.Models
{
    [Serializable]
    public class DishData
    {
        public string dishId;
        public string userId;
        public string name;
        public float price;
        public int score;
        public int objectiveScore;
        public DishState finalState;
        public string logPath;
        public string reviewId;
        public string reviewText;
        public List<string> reviewIds;
        public string createdAt;
    }
}

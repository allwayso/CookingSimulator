using System;

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
        public DishState finalState;
        public string logPath;
        public string reviewText;
        public string createdAt;
    }
}

using System;

namespace CookingSimulator.Models
{
    [Serializable]
    public class ReviewData
    {
        public string reviewId;
        public string dishId;
        public string reviewerName;
        public string reviewerId;
        public int score;
        public string summary;
        public string suggestion;
        public int reputationDelta;
        public string createdAt;
    }
}

using System;

namespace CookingSimulator.Models
{
    [Serializable]
    public class AIReviewConfig
    {
        public AIReviewProvider[] providers;
    }

    [Serializable]
    public class AIReviewProvider
    {
        public string name;
        public string baseUrl;
        public string apiKey;
        public string model;
    }
}

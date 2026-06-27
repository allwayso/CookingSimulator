using System;

namespace CookingSimulator.Models
{
    [Serializable]
    public class UserData
    {
        public string userId;
        public string username;
        public string passwordHash;
        public int reputation;
        public string createdAt;
        public string lastLoginAt;
        public int lifeValue = 100;
    }
}

using CookingSimulator.Services;
using UnityEditor;

namespace CookingSimulator.Editor
{
    public static class AIReviewTester
    {
        [MenuItem("Cooking Simulator/Test AI Review API")]
        public static void TestAIReviewApi()
        {
            if (!AIReviewService.TestConnectionNow(out var message))
            {
                throw new System.InvalidOperationException(message);
            }

            UnityEngine.Debug.Log("AI review API test succeeded.");
        }
    }
}

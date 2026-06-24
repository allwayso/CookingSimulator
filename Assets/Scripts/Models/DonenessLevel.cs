namespace CookingSimulator.Models
{
    /// <summary>
    /// 食材熟度等级：全生 → 半生 → 全熟 → 过头
    /// </summary>
    public enum DonenessLevel
    {
        Raw,         // 全生
        HalfCooked,  // 半生
        FullyCooked, // 全熟
        Overcooked   // 过头
    }
}

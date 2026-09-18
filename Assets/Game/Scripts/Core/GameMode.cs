/// <summary>
/// Defines the active gameplay mode (Endless vs Level-based Campaign).
/// </summary>
public enum GameMode
{
    Endless = 0, // Infinite mode: unlimited drops, ends on danger line overflow
    Level = 1    // Level mode: target score objective with move/drop limits
}

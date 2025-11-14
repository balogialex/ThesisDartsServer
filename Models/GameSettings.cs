using System;

namespace DartsAPI.Models;

public enum LegFormat { FirstTo, BestOf }
public enum MatchFormat { Leg }

public class GameSettings
{
    public MatchFormat MatchFormat { get; set; } = MatchFormat.Leg;
    public LegFormat LegFormat { get; set; } = LegFormat.FirstTo;
    public int LegCount { get; set; } = 2;
    public List<string> OrderOfPlayerNames { get; set; } = new List<string>();
    public int StartScore { get; set; } = 501;
}



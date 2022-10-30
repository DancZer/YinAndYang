using UnityEngine;

public readonly struct GameDate
{
    public readonly int Year;
    public readonly int Month;
    public readonly int TotalMonth;

    public GameDate(int totalMonth)
    {
        TotalMonth = totalMonth;
        Year = totalMonth / 12;
        Month = (totalMonth % 12) + 1;
    }

    public static GameDate operator ++(GameDate a)
        => new GameDate(a.TotalMonth+1);
    public static GameDate operator --(GameDate a)
     => new GameDate(a.TotalMonth-1);
    public static GameDate operator +(GameDate a, GameDate b)
        => new GameDate(a.TotalMonth + b.TotalMonth);
    public static GameDate operator -(GameDate a, GameDate b)
        => new GameDate(a.TotalMonth - b.TotalMonth);
}

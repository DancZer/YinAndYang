using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;


public class TimeManager : NetworkBehaviour
{
    [SerializeField] [Range(0, 600)] public float DayInGameInSecond = 24;
    [SerializeField] [Range(0, 600)] public float MonthInGameInSecond = 24;
    [SerializeField] [Range(0, 3)] public int DaySyncPrecision = 2;

    [SyncVar] public float TimeOfTheDay;
    [SyncVar] public GameDate DateOfTheGame;

    private float _timeCounter;
    private float _rounder;

    private int _monthCounter;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _rounder = Mathf.Pow(10, DaySyncPrecision);
        _timeCounter = DayInGameInSecond / 2;
    }

    private void Update()
    {
        if (!IsServer) return;
        
        _timeCounter += Time.deltaTime;

        UpdateTimeOfTheDay();
        UpdateYearOfTheGame();
    }

    private void UpdateTimeOfTheDay()
    {
        var newTimeOfTheDay = (_timeCounter / DayInGameInSecond) * 24f;
            
        newTimeOfTheDay %= 24;
        newTimeOfTheDay = Mathf.Round(newTimeOfTheDay*_rounder)/_rounder;

        if(newTimeOfTheDay != TimeOfTheDay)
        {
            TimeOfTheDay = newTimeOfTheDay;
        }
    }
    private void UpdateYearOfTheGame()
    {
        var newMonth = Mathf.FloorToInt(_timeCounter / MonthInGameInSecond);

        if (newMonth != _monthCounter)
        {
            _monthCounter = newMonth;
            DateOfTheGame = new GameDate(newMonth);
        }
    }
}

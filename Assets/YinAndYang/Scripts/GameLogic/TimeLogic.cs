using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;


public class TimeLogic : NetworkBehaviour
{
    [SerializeField] [Range(0, 600)] public float DayInGameInSecond = 24;
    [SerializeField] [Range(0, 600)] public float YearInGameInSecond = 24;
    [SerializeField] [Range(0, 3)] public int DaySyncPrecision = 2;

    [SyncVar] public float TimeOfTheDay;
    [SyncVar] public int YearOfTheGame;

    private float _timeCounter;
    private float _rounder;
    public override void OnStartServer()
    {
        base.OnStartServer();

        _rounder = Mathf.Pow(10, DaySyncPrecision);
        _timeCounter = DayInGameInSecond / 2;
    }

    private void Update()
    {
        if (IsServer)
        {
            _timeCounter += Time.deltaTime;

            UpdateTimeOfTheDay();
            UpdateYearOfTheGame();
        }
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
        var newYear = Mathf.FloorToInt(_timeCounter / YearInGameInSecond);

        if (newYear != YearOfTheGame)
        {
            YearOfTheGame = newYear;
        }
    }
}

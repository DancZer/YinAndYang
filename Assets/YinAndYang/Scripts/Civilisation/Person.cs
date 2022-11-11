using UnityEngine;

public class Person : MonoBehaviour
{
    public enum Gender
    {
        Male, Female
    }
    public enum Actions
    {
        Stop, Move, Sleep
    }

    public string PersonName;
    public GameDate BirthDate;
    public Gender Sex;

    public TownCenter TownCenter;

    public GameDate Age 
    { 
        get
        {
            return _timeManager.DateOfTheGame - BirthDate;
        } 
    }

    private GameTimeManager _timeManager;

    void Start()
    {
        _timeManager = StaticObjectAccessor.GetTimeManager();
    }
}

using FishNet.Object;

public class Person : NetworkBehaviour
{
    public enum Sexs
    {
        Male, Female
    }
    public enum Actions
    {
        Stop, Move, Sleep
    }

    public string PersonName;
    public GameDate BirthDate;
    public Sexs Sex;

    public GameDate Age 
    { 
        get
        {
            return _timeManager.DateOfTheGame - BirthDate;
        } 
    }

    private GameTimeManager _timeManager;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _timeManager = StaticObjectAccessor.GetTimeManager();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    void Update()
    {
    }
}

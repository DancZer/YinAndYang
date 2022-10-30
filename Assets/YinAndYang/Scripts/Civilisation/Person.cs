using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

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

    public GameDate Age { get
        {
            return _timeManager.DateOfTheGame - BirthDate;
        } }

    public float MoveSpeed = 0.1f;
    public float MaxRestTime = 10;

    public Vector3 TargetPos;
    public float NextTargetTime;

    private Rigidbody _rigidbody;
    private GameTimeManager _timeManager;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _timeManager = StaticObjectAccessor.GetTimeManager();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (IsServer)
        {
            if(TargetPos != null)
            {
                var distance = Vector3.Distance(transform.position, TargetPos);
                if (distance < MoveSpeed)
                {
                    TargetPos = transform.position;
                    NextTargetTime = Time.realtimeSinceStartup + Random.Range(MaxRestTime/10f, MaxRestTime);
                }
                else
                {
                    var dir = transform.position - TargetPos;
                    dir = dir.normalized;

                    MovePerson(dir);
                }
            }
            else
            {
                if (NextTargetTime < Time.realtimeSinceStartup)
                {
                    TargetPos = new Vector3(Random.Range(0f, 50f), 0f, Random.Range(0f, 50f));
                }
            }
        }   
    }

    [ObserversRpc]
    public void MovePerson(Vector3 dir)
    {
        _rigidbody.AddForce(dir * MoveSpeed);
    }
}

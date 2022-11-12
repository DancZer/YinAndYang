using UnityEngine;

public class PersonMovement : MonoBehaviour
{
    public float PatrolSearchRadius = 100;
    public float CompletionRadius = 2;
    public float RestBetweenPatrol = 10;
    public float WalkSpeed = 2;

    private MapManager _terrainManager;

    private Vector3 _patrolDestPos;
    private bool _isPatrolDestSet;
    private float _lastPatrolFinisedTime;

    private Person _person;
    private Rigidbody _rigidbody;

    void Start()
    {
        _person = GetComponent<Person>();
        _rigidbody = GetComponent<Rigidbody>();
        _terrainManager = StaticObjectAccessor.GetTerrainManager();
    }

    void Update()
    {
        Patroling();
    }

    private void Patroling()
    {
        if (!_isPatrolDestSet)
        {
            if (_lastPatrolFinisedTime + RestBetweenPatrol < Time.realtimeSinceStartup)
            {
                SearchForPatrolDest();
                _isPatrolDestSet = true;
            }

        }
        else
        {
            float dest = Vector3.Distance(_patrolDestPos, transform.position);

            if(dest < CompletionRadius)
            {
                _isPatrolDestSet = false;
                _lastPatrolFinisedTime = Time.realtimeSinceStartup;
            }

            var dir = (_patrolDestPos - transform.position).normalized;

            _rigidbody.velocity = dir * WalkSpeed;
        }
    }

    private void SearchForPatrolDest()
    {
        var nextPosOffset = new Vector3();

        nextPosOffset.x = Random.Range(-PatrolSearchRadius, PatrolSearchRadius);
        nextPosOffset.z = Random.Range(-PatrolSearchRadius, PatrolSearchRadius);

        nextPosOffset += transform.position;

        _patrolDestPos = _terrainManager.GetPosOnTerrain(nextPosOffset);
        Debug.Log($"SetDestination {_patrolDestPos}");
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_patrolDestPos, CompletionRadius);
    }
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pascolo casuale con supporto per Animator nei figli (Metodo Scatola)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CowWander : MonoBehaviour
{
    [Header("Area di pascolo")]
    public Transform penCenter;
    public float wanderRadius = 5f;

    [Header("Ritmo")]
    public float minWait = 3f;
    public float maxWait = 9f;

    [Header("Freeze durante il puzzle")]
    public GameObject level1Panel;

    NavMeshAgent agent;
    Animator anim;
    float nextMoveTime;
    Vector3 home;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // AGGIORNAMENTO: Cerca l'Animator anche negli oggetti figli!
        anim = GetComponentInChildren<Animator>();

        home = penCenter != null ? penCenter.position : transform.position;
        agent.speed *= Random.Range(0.8f, 1.2f);
        ScheduleNext();
    }

    void Update()
    {
        bool puzzleActive = level1Panel != null && level1Panel.activeInHierarchy;
        if (puzzleActive)
        {
            if (!agent.isStopped) agent.isStopped = true;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }
        if (agent.isStopped) agent.isStopped = false;

        if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);

        if (!agent.pathPending && agent.remainingDistance < 0.3f && Time.time >= nextMoveTime)
        {
            MoveToRandomPoint();
            ScheduleNext();
        }
    }

    void MoveToRandomPoint()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = home + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void ScheduleNext()
    {
        nextMoveTime = Time.time + Random.Range(minWait, maxWait);
    }
}
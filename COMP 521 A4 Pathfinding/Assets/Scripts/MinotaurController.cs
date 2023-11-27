using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class MinotaurController : MonoBehaviour
{
    public static float SPEED = 5f;
    public static int RECALCULATE_BLOCKED_PATH_DISTANCE = 1;
    public static float HUNT_TRIGGER_DISTANCE = 5;
    public static float ATTACK_RADIUS = 2;
    public static float ATTACK_COOLDOWN = 1;

    public GameObject treasure;
    public SpriteRenderer attackDisplay;

    private NavMesh navMesh;
    private Vector3 lastTargetPosition;
    private List<Partition> path;
    private MinotaurState state;
    private float secondsSinceLastAttack = 0;

    // Start is called before the first frame update
    void Start()
    {
        navMesh = FindObjectOfType<NavMesh>();
        var parts = navMesh.GetPartitions();
        path = null;
    
        attackDisplay.gameObject.SetActive(false);

        StartCoroutine(GuardTreasure());
    }

    // Update is called once per frame
    void Update()
    {
        secondsSinceLastAttack += Time.deltaTime;

        foreach (GameObject adventurer in AdventurerController.ADVENTURERS)
        {
            if (adventurer.activeSelf && state == MinotaurState.Idle && Vector3.Distance(adventurer.transform.position, treasure.transform.position) <= HUNT_TRIGGER_DISTANCE)
            {
                StopAllCoroutines();
                StartCoroutine(HuntAdventurer(adventurer));
            }
        }
    }

    // Make minotaur chase an adventurer
    IEnumerator HuntAdventurer(GameObject adventurer)
    {
        //Debug.Log("hunting");
        state = MinotaurState.Attack;

        while (adventurer.activeSelf && (Vector3.Distance(adventurer.gameObject.transform.position, treasure.transform.position) <= HUNT_TRIGGER_DISTANCE 
                || Vector3.Distance(adventurer.gameObject.transform.position, treasure.transform.position) <= HUNT_TRIGGER_DISTANCE/2 ))
        {
            yield return StartCoroutine(GoToPlayer(navMesh.GetPartition(transform.position), navMesh.GetPartition(adventurer.transform.position), adventurer));
        }

        StartCoroutine(GuardTreasure());
    }

    // Move minotaur from point start to end
    IEnumerator GoToPlayer(Partition start, Partition end, GameObject adventurer)
    {
        //Debug.Log("start follow path");
        List<Partition> path = AStar.FindPath(start, end, new List<GameObject>(){ gameObject, adventurer });
        
        if (path == null)
        {
            yield break;
        }

        path.Remove(end);

        yield return null;

        if(path.Count == 0) { yield break; }
        Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

        while (!(path.Count == 1 && Vector3.Distance(transform.position, nextPathPos) < 0.001))
        {
            if (adventurer.activeSelf == false)
            {
                yield break;
            }
            if (Vector3.Distance(end.GetPosition(), adventurer.transform.position) > 2f)
            {
                RecalculatePath(path, end);
            }
            if (Vector3.Distance(transform.position, adventurer.transform.position) <= ATTACK_RADIUS && secondsSinceLastAttack > ATTACK_COOLDOWN)
            {
                StartCoroutine(Attack());
            }
            HandleBlockedPath(path, end);
            MoveAlongPath(path);
            DrawDebugPath(path, Color.red);
            nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;
            //Debug.Log("following path");
            yield return null;
        }
        //Debug.Log("end follow path");
    }

    IEnumerator Attack()
    {
        //Debug.Log("attacking");
        attackDisplay.gameObject.SetActive(true);
        secondsSinceLastAttack = 0;

        Collider[] hitObjects = Physics.OverlapCapsule(attackDisplay.transform.position, attackDisplay.transform.position + Vector3.up, ATTACK_RADIUS);

        foreach (Collider collider in hitObjects)
        {
            AdventurerController adventurer = collider.GetComponent<AdventurerController>();
            if (adventurer != null)
            {
                adventurer.TakeDamage();
            }
        }

        yield return new WaitForSeconds(0.5f);
        attackDisplay.gameObject.SetActive(false);
    }

    // Make minotaur circle the treasure on a loop
    IEnumerator GuardTreasure()
    {
        //Debug.Log("guarding");
        state = MinotaurState.Idle;
        Vector3 lastTreasurePos = treasure.transform.position;
        List<Partition> points = GeneratePatrolPoints(2);

        while (true)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(lastTreasurePos, treasure.transform.position) > 0.01)
                {
                    StartCoroutine(GuardTreasure());
                    yield break;
                }
                yield return StartCoroutine(FollowPath(navMesh.GetPartition(transform.position), points[(i+1)%points.Count], new List<GameObject>(){gameObject}));
            }
        }
    }

    // Move minotaur from point start to end
    IEnumerator FollowPath(Partition start, Partition end, List<GameObject> ignoreObjects)
    {
        //Debug.Log("start follow path");
        List<Partition> path = AStar.FindPath(start, end, ignoreObjects);
        
        if (path == null)
        {
            yield break;
        }

        yield return null;

        Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

        while (!(path.Count == 1 && Vector3.Distance(transform.position, nextPathPos) < 0.001))
        {
            HandleBlockedPath(path, end);
            MoveAlongPath(path);
            DrawDebugPath(path, Color.red);
            nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;
            //Debug.Log("follow path 2");
            yield return null;
        }
        //Debug.Log("end follow path");
    }

    List<Partition> GeneratePatrolPoints(float distFromTreasure)
    {
        List<Partition> points = new List<Partition>();

        for (int i = -1; i < 2; i += 2)
        {
            for (int j = 1; j > -2; j -= 2)
            {
                Vector3 pos = treasure.transform.position + Vector3.forward * i * distFromTreasure + Vector3.left * j * i * distFromTreasure;
                Partition part = navMesh.GetPartition(pos);
                if (part != null)
                {
                    points.Add(part);
                }
            }
        }

        return points;
    }


    // Recalculates path when position of target changes or path is blocked
    void HandleBlockedPath(List<Partition> curPath, Partition target)
    {
        bool blocked = false;
        if (curPath != null)
        {
            for (int i = 0; i < Mathf.Min(curPath.Count, RECALCULATE_BLOCKED_PATH_DISTANCE + 1); i++)
        {
            if (curPath[i].GetOccupied() != null && curPath[i].GetOccupied() != gameObject)
            {
                blocked = true;
            }
        }
        }
        

        if (blocked || curPath == null)
        {
            RecalculatePath(curPath, target);
        }
    }

    void RecalculatePath(List<Partition> curPath, Partition target)
    {
        curPath = AStar.FindPath(navMesh.GetPartition(transform.position), target, gameObject);
    }

    // Handles movement towards target
    void MoveToTarget()
    {
        if (path != null && path.Count > 0)
        {
            Vector3 nextPathPos = path[0].GetPosition() - path[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

            // Check if the position of the human and the next point on the path are approximately equal.
            if (Vector3.Distance(transform.position, nextPathPos) < 0.001f)
            {
                transform.position = nextPathPos;
                path.RemoveAt(0);
            }

            if (path.Count > 0)
            {
                // Move position a step closer to the target.
                var step =  SPEED * Time.deltaTime; // calculate distance to move
                
                transform.position = Vector3.MoveTowards(transform.position, nextPathPos, step);
            }
        }
    }

    // Handles movement towards target
    void MoveAlongPath(List<Partition> curPath)
    {
        if (curPath != null && curPath.Count > 0)
        {
            Vector3 nextPathPos = curPath[0].GetPosition() - curPath[0].GetPosition().y * Vector3.up + transform.position.y * Vector3.up;

            // Check if the position of the human and the next point on the path are approximately equal.
            if (Vector3.Distance(transform.position, nextPathPos) < 0.001f)
            {
                transform.position = nextPathPos;
                curPath.RemoveAt(0);
            }

            if (curPath.Count > 0)
            {
                // Move position a step closer to the target.
                var step =  SPEED * Time.deltaTime; // calculate distance to move
                
                transform.position = Vector3.MoveTowards(transform.position, nextPathPos, step);
            }
        }
    }

    // Draws path in debug window
    void DrawDebugPath()
    {
        if (path != null) 
        {
            for(int i = 0; i < path.Count-1; i++)
            {
                Debug.DrawLine(path[i].GetPosition(), path[i+1].GetPosition(), Color.red);
            }
        }
    }

    // Draws path in scene view
    void DrawDebugPath(List<Partition> curPath, Color color)
    {
        if (curPath != null) 
        {
            for(int i = 0; i < curPath.Count-1; i++)
            {
                Debug.DrawLine(curPath[i].GetPosition(), curPath[i+1].GetPosition(), color);
            }
        }
    }

    public List<Partition> GetPath()
    {
        return path;
    }

    public enum MinotaurState 
    {
        Attack,
        Idle
    }
}

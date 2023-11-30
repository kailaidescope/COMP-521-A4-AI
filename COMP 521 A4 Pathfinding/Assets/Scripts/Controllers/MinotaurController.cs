using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class MinotaurController : MonoBehaviour
{
    public static float SPEED = 3.75f;
    public static int RECALCULATE_BLOCKED_PATH_DISTANCE = 1;
    public static float AGRO_DISTANCE = 5;
    public static float ATTACK_RADIUS = 2;
    public static float ATTACK_COOLDOWN = 1;
    public static float FORGET_LAST_ATTACKER_COOLDOWN = 5;
    public static int RECALCULATE_TARGET_DELAY = 1;
    public static MinotaurController MINOTAUR;

    public GameObject treasure;
    public SpriteRenderer attackDisplay;

    private NavMesh navMesh;
    private List<Partition> path;
    private float secondsSinceLastAttack = 1;
    private float secondsSinceTookDmg = 5;
    private AdventurerController lastAttackedBy = null;
    private AdventurerController lastTarget = null;
    private AdventurerController currentTarget = null;

    // Called before Start()
    void Awake()
    {
        MINOTAUR = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        navMesh = FindObjectOfType<NavMesh>();
        var parts = navMesh.GetPartitions();
        path = null;

        attackDisplay.gameObject.SetActive(false);

        StartCoroutine(GuardTreasure());
        StartCoroutine(GetAttackPriority());
    }

    // Update is called once per frame
    void Update()
    {
        secondsSinceLastAttack += Time.deltaTime;
        secondsSinceTookDmg += Time.deltaTime;

        if (currentTarget != null && currentTarget != lastTarget)
        {
            StopAllCoroutines();
            StartCoroutine(GetAttackPriority());
            StartCoroutine(HuntAdventurer(currentTarget));
        } else if (currentTarget == null && currentTarget != lastTarget)
        {
            StopAllCoroutines();
            StartCoroutine(GetAttackPriority());
            StartCoroutine(GuardTreasure());
        }
    }   

    // Calculates the highest priority target, if there is any
    IEnumerator GetAttackPriority()
    {
        Debug.Log("Getting target");
        if (lastAttackedBy != null && secondsSinceTookDmg > FORGET_LAST_ATTACKER_COOLDOWN)
        {
            lastAttackedBy = null;
        }

        lastTarget = currentTarget;

        if (treasure.GetComponent<TreasureController>().GetHolder() != null)
        {
            currentTarget = treasure.GetComponent<TreasureController>().GetHolder();
        } else if (lastAttackedBy != null)
        {
            currentTarget = lastAttackedBy;
        } else 
        {
            AdventurerController closestAdventurerToTreasure = null;
            float closestAdventurerToTreasureDist = -1;

            AdventurerController closestAdventurerToMinotaur = null;
            float closestAdventurerToMinotaurDist = -1;

            foreach (AdventurerController adventurer in AdventurerController.LIVING_ADVENTURERS)
            {
                float distanceToTreasure = Vector3.Distance(adventurer.transform.position, treasure.transform.position);
                if (adventurer.gameObject.activeSelf && distanceToTreasure <= AGRO_DISTANCE)
                {
                    if (closestAdventurerToTreasure == null)
                    {
                        closestAdventurerToTreasure = adventurer;
                        closestAdventurerToTreasureDist = distanceToTreasure;
                    } else if (closestAdventurerToTreasureDist > distanceToTreasure)
                    {
                        closestAdventurerToTreasure = adventurer;
                        closestAdventurerToTreasureDist = distanceToTreasure;
                    }
                }

                float distanceToMinotaur = Vector3.Distance(adventurer.transform.position, transform.position);
                if (adventurer.gameObject.activeSelf && distanceToMinotaur <= AGRO_DISTANCE)
                {
                    if (closestAdventurerToMinotaur == null)
                    {
                        closestAdventurerToMinotaur = adventurer;
                        closestAdventurerToMinotaurDist = distanceToMinotaur;
                    } else if (closestAdventurerToMinotaurDist > distanceToMinotaur)
                    {
                        closestAdventurerToMinotaur = adventurer;
                        closestAdventurerToMinotaurDist = distanceToMinotaur;
                    }
                }
            }

            if (closestAdventurerToTreasure != null)
            {
                currentTarget = closestAdventurerToTreasure;
            } else if (closestAdventurerToMinotaur != null)
            {
                currentTarget = closestAdventurerToMinotaur;
            } else
            {
                currentTarget = null;
            }
        }

        //Debug.Log("Current target = "+currentTarget + ", Last target = "+lastTarget);
        yield return new WaitForSeconds(RECALCULATE_TARGET_DELAY);
        StartCoroutine(GetAttackPriority());
    }

    // Make minotaur chase an adventurer
    IEnumerator HuntAdventurer(AdventurerController adventurer)
    {
        Debug.Log("start hunt");

        while(true)
        {
            yield return StartCoroutine(GoToPlayer(navMesh.GetPartition(transform.position), navMesh.GetPartition(adventurer.gameObject.transform.position), adventurer));
        }
    }

    // Move minotaur from point start to end
    IEnumerator GoToPlayer(Partition start, Partition end, AdventurerController adventurer)
    {
        //Debug.Log("start follow path");
        List<Partition> path = AStar.FindPath(start, end, new List<UnityEngine.GameObject>() { gameObject, adventurer.gameObject });

        if (path == null)
        {
            yield break;
        }

        path.Remove(end);

        yield return null;

        if(path.Count == 0) { yield break; }

        while (path.Count > 0)
        {
            if (adventurer.gameObject.activeSelf == false)
            {
                yield break;
            }
            if (Vector3.Distance(end.GetPosition(), adventurer.transform.position) > 2f)
            {
                RecalculatePath(path, end);
            }
            if (Vector3.Distance(transform.position, adventurer.transform.position) <= ATTACK_RADIUS && secondsSinceLastAttack > ATTACK_COOLDOWN)
            {
                if (secondsSinceLastAttack <= ATTACK_COOLDOWN)
                {
                    yield return new WaitForSeconds(1);
                } 
                
                StartCoroutine(Attack());
                yield break;
            }
            HandleBlockedPath(path, end);
            MoveAlongPath(path);
            DrawDebugPath(path, Color.red);
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
        Debug.Log("guarding");
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
                yield return StartCoroutine(FollowPath(navMesh.GetPartition(transform.position), points[(i + 1)% points.Count], new List<UnityEngine.GameObject>() { gameObject }));
            }
        }
    }

    // Move minotaur from point start to end
    IEnumerator FollowPath(Partition start, Partition end, List<UnityEngine.GameObject> ignoreObjects)
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

    public void Attack(AdventurerController adventurerController)
    {
        lastAttackedBy = adventurerController;
        secondsSinceTookDmg = 0;
    }

    public List<Partition> GetPath()
    {
        return path;
    }

    public AdventurerController GetCurrentTarget()
    {
        return currentTarget;
    }
}

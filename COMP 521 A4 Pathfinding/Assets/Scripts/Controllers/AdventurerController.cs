using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class AdventurerController : MonoBehaviour
{
    public static List<AdventurerController> ADVENTURERS = new List<AdventurerController>();
    public static Color MELEE_COLOR = Color.red;
    public static Color RANGED_COLOR = Color.white;
    public static float FLEE_DISTANCE = MinotaurController.AGRO_DISTANCE * 2;
    public static float FLEE_ATTEMPT_DISTANCE = 10;
    public static float PICKUP_TREASURE_DISTANCE = 1f;
    public static float HEIGHT_OFFSET_TO_GROUND = -1;
    public static int RECALCULATE_BLOCKED_PATH_DISTANCE = 2;
    public static float SPEED = MinotaurController.SPEED / 2;

    public AdventurerType adventurerType;
    public TextMeshProUGUI[] texts = new TextMeshProUGUI[10];

    private SpriteRenderer spriteRenderer;
    private CharacterController characterController;
    private Task headTask;
    private List<BasicTask> plan;
    private StateVector currentState;
    private NavMesh navMesh;

    void Awake()
    {
        ADVENTURERS.Add(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = (adventurerType == AdventurerType.MELEE)? MELEE_COLOR : RANGED_COLOR;
        characterController = GetComponent<CharacterController>();
        navMesh = FindObjectOfType<NavMesh>();

        if (adventurerType == AdventurerType.MELEE)
        {
            InitMeleeHTN();
        } else 
        {
            InitRangedHTN();
        }
    }

    void InitMeleeHTN()
    {
        // Flee Minotaur
        Func<StateVector, bool> pre = (StateVector state) => { return state.dist_minotaur < FLEE_DISTANCE; };
        Func<StateVector, StateVector> post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask fleeMinotaur = new BasicTask(StartFleeMinotaur, "Fleeing minotaur", pre, post);

        // Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask moveToTreasure = new BasicTask(StartMoveToTreasure, "Moving to treasure", pre, post);

        // Claim Treasure
        // Method 0: Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        CompositeTask claimTreasure = new CompositeTask(new List<List<Task>>{ new List<Task>(){ moveToTreasure } }, pre, post);
    }

    void InitRangedHTN()
    {
        // Flee Minotaur
        Func<StateVector, bool> pre = (StateVector state) => { return state.dist_minotaur < FLEE_DISTANCE; };
        Func<StateVector, StateVector> post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask fleeMinotaur = new BasicTask(StartFleeMinotaur, "Fleeing minotaur", pre, post);

        // Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask moveToTreasure = new BasicTask(StartMoveToTreasure, "Moving to treasure", pre, post);

        // Claim Treasure
        // Method 0: Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        CompositeTask claimTreasure = new CompositeTask(new List<List<Task>>{ new List<Task>(){ moveToTreasure } }, pre, post);
    }

    public void StartFleeMinotaur()
    {
        StopAllCoroutines();
        StartCoroutine(FleeMinotaur());
    }

    IEnumerator FleeMinotaur()
    {
        int rotations = 8;

        Vector3 fleePoint = Vector3.forward * FLEE_ATTEMPT_DISTANCE;
        Vector3 bestFleePoint = Vector3.zero;
        float bestDistDiff = 2;
        
        for (int i = 0; i < rotations; i++)
        {
            try
            {
                Partition goal = navMesh.GetPartition(transform.position + fleePoint);

                float adventurerDistance = AStar.FindPathDistance(navMesh.GetPartition(transform.position), goal, new List<GameObject>(){ gameObject });
                float minotaurDistance = AStar.FindPathDistance(navMesh.GetPartition(MinotaurController.MINOTAUR.transform.position), goal, new List<GameObject>(){ gameObject });

                // Check if path exists
                if (adventurerDistance != -1)
                {
                    float thisDistDiff = minotaurDistance - adventurerDistance;
                    if ( bestDistDiff < thisDistDiff)
                    {
                        bestFleePoint = fleePoint;
                        bestDistDiff = thisDistDiff;
                    }
                }
            } catch(Exception e)
            {
                Debug.Log("Exception caught: "+e);
            }

            fleePoint = Quaternion.AngleAxis(360/rotations, Vector3.up) * fleePoint;
        }

        // Check if valid point was found
        if(bestDistDiff == 2)
        {
            // Recalculate plan here
            yield break;
        }

        Partition end = navMesh.GetPartition(transform.position + bestFleePoint);
        List<Partition> path = AStar.FindPath(navMesh.GetPartition(transform.position), end, gameObject);

        yield return StartCoroutine(MoveOnPath(path, end));
    }

    public void StartMoveToTreasure()
    {
        StopAllCoroutines();
        StartCoroutine(MoveToTreasure());
    }

    IEnumerator MoveToTreasure()
    {
        List<Partition> adjToTreasure = navMesh.GetPartition(TreasureController.TREASURE.transform.position).GetConnectedPartitions();
        Partition end = adjToTreasure[UnityEngine.Random.Range(0, adjToTreasure.Count)];
        List<Partition> path = AStar.FindPath(navMesh.GetPartition(transform.position), end, gameObject);

        yield return StartCoroutine(MoveOnPath(path, end));
    }

    IEnumerator MoveOnPath(List<Partition> path, Partition end)
    {
        if (path == null)
        {
            // Should recalculate plan here
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


    // Update is called once per frame
    void Update()
    {
        UpdateCurrentState();
    }

    void UpdateCurrentState()
    {
        RaycastHit hit;
        Ray ray = new Ray(gameObject.transform.position + HEIGHT_OFFSET_TO_GROUND * Vector3.up, MinotaurController.MINOTAUR.gameObject.transform.position - gameObject.transform.position);
        Physics.Raycast(ray, out hit, 40, -1, QueryTriggerInteraction.Ignore);
        //Debug.DrawLine(ray.origin, ray.direction * hit.distance, Color.green);

        Partition closestCorner = null;
        float closestCornerDistance = -1;

        foreach (Partition corner in navMesh.GetCorners())
        {
            float distance = Vector3.Distance(transform.position + HEIGHT_OFFSET_TO_GROUND * Vector3.up, corner.GetPosition());

            if (closestCorner == null || closestCornerDistance > distance)
            {
                closestCorner = corner;
                closestCornerDistance = distance;
            }
        }

        StateVector newState = new StateVector((int) characterController.healthBar.value, Vector3.Distance(transform.position, MinotaurController.MINOTAUR.transform.position), 
                                Vector3.Distance(transform.position, TreasureController.TREASURE.transform.position), closestCornerDistance, currentState.seconds_attack + Time.deltaTime, 
                                currentState.seconds_damaged + Time.deltaTime, currentState.seconds_dropped_treasure + Time.deltaTime, hit.rigidbody != null && hit.rigidbody.gameObject == MinotaurController.MINOTAUR.gameObject,
                                TreasureController.TREASURE.GetHolder(), MinotaurController.MINOTAUR.GetCurrentTarget());

        texts[0].text = "Health: "+newState.health;
        texts[1].text = "Dist Mino: "+(int)newState.dist_minotaur;
        texts[2].text = "Dist Tre: "+(int)newState.dist_treasure;
        texts[3].text = "Dist Cor: "+(int)newState.dist_corner;
        texts[4].text = "Sec Atk: "+(int)newState.seconds_attack;
        texts[5].text = "Sec Dmg: "+(int)newState.seconds_damaged;
        texts[6].text = "Sec Drop: "+(int)newState.seconds_dropped_treasure;
        texts[7].text = "LoS: "+newState.line_of_sight;
        texts[8].text = "Tre Hold: "+newState.treasure_holder;
        texts[9].text = "Min Trg: "+newState.minotaur_target;

        currentState = newState;
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

    public void TakeDamage()
    {
        characterController.TakeDamage();
    }

    public enum AdventurerType
    {
        MELEE,
        RANGED
    }
}

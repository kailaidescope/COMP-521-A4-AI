using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AdventurerController : MonoBehaviour
{
    public static List<AdventurerController> LIVING_ADVENTURERS = new List<AdventurerController>();
    public static Color MELEE_COLOR = Color.red;
    public static Color RANGED_COLOR = Color.white;
    public static float FLEE_DISTANCE = 10;
    public static float FLEE_ATTEMPT_DISTANCE = 10;
    public static float INTERACT_WITH_OBJECT_DISTANCE = 1f;
    public static float HEIGHT_OFFSET_TO_GROUND = -1;
    public static int RECALCULATE_BLOCKED_PATH_DISTANCE = 2;
    public static float SPEED = 3;
    public static int MAX_HEALTH = 6;
    public static int PICKUP_TREASURE_DELAY = 3;
    public static int ATTACK_COOLDOWN = 1;
    public static float RANGED_IDEAL_ATTACK_DIST = 15;

    public AdventurerType adventurerType;
    public TextMeshProUGUI[] texts = new TextMeshProUGUI[12];
    public Slider healthBar;

    private SpriteRenderer spriteRenderer;
    private CharacterController characterController;
    private Task headTask;
    private List<BasicTask> plan;
    private BasicTask currentTask;
    private StateVector currentState;
    private NavMesh navMesh;

    void Awake()
    {
        LIVING_ADVENTURERS.Add(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = (adventurerType == AdventurerType.MELEE)? MELEE_COLOR : RANGED_COLOR;
        characterController = GetComponent<CharacterController>();
        navMesh = FindObjectOfType<NavMesh>();
        healthBar.value = MAX_HEALTH;

        if (adventurerType == AdventurerType.MELEE)
        {
            InitMeleeHTN();
        } else 
        {
            InitRangedHTN();
        }

        UpdateCurrentState();
        plan = HTN.GeneratePlan(currentState, headTask);
    }

    void InitMeleeHTN()
    {
        // Flee Minotaur
        Func<StateVector, bool> pre = (StateVector state) => { return state.dist_minotaur < FLEE_DISTANCE; };
        Func<StateVector, StateVector> post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask fleeMinotaur = new BasicTask(StartFleeMinotaur, "Flee minotaur", pre, post);

        // Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask moveToTreasure = new BasicTask(StartMoveToTreasure, "Move to treasure", pre, post);

        // Grab Treasure
        pre = (StateVector state) => { return state.seconds_dropped_treasure > PICKUP_TREASURE_DELAY && state.dist_treasure <= INTERACT_WITH_OBJECT_DISTANCE; };
        post = (StateVector state) => { state.treasure_holder = this; return state; };
        BasicTask grabTreasure = new BasicTask(StartGrabTreasure, "Grab treasure", pre, post);

        // Move to Corner
        pre = (StateVector state) => { return state.dist_corner > INTERACT_WITH_OBJECT_DISTANCE && state.treasure_holder == this; };
        post = (StateVector state) => { state.dist_corner = INTERACT_WITH_OBJECT_DISTANCE; return state; };
        BasicTask moveToCorner = new BasicTask(StartMoveToCorner, "Move to corner", pre, post);

        // Claim Treasure
        // Method 0: Move to Treasure
        // Method 1: Grab treasure
        // Method 2: Move to corner (to claim victory)
        List<List<Task>> methods = new List<List<Task>>{ new List<Task>(){ moveToTreasure }, new List<Task>(){ grabTreasure }, new List<Task>(){ moveToCorner } };
        Func<StateVector, List<List<Task>>> methodSelector = (StateVector state) => 
        { 
            if(state.treasure_holder == null){
                return new List<List<Task>>(){ new List<Task>(){ moveToTreasure } };
            } else
            {
                return new List<List<Task>>();
            }
        };
        CompositeTask claimTreasure = new CompositeTask(methods, methodSelector);

        // Move to Attack
        pre = (StateVector state) => { return state.dist_minotaur > INTERACT_WITH_OBJECT_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = INTERACT_WITH_OBJECT_DISTANCE; return state; };
        BasicTask moveToAttack = new BasicTask(StartMoveToAttack_Melee, "Move to attack", pre, post);

        // Attack
        pre = (StateVector state) => { return state.dist_minotaur <= INTERACT_WITH_OBJECT_DISTANCE && state.seconds_attack > ATTACK_COOLDOWN && state.seconds_damaged > ATTACK_COOLDOWN; };
        post = (StateVector state) => { state.seconds_attack = 0; return state; };
        BasicTask attack = new BasicTask(StartAttack_Melee, "Attack", pre, post);

        // Idle to attack
        pre = (StateVector state) => { return state.seconds_attack <= ATTACK_COOLDOWN || state.seconds_damaged <= ATTACK_COOLDOWN; };
        post = (StateVector state) => { state.seconds_attack = 2; state.seconds_damaged = 2; return state; };
        BasicTask idleToAttack = new BasicTask(StartIdleToAttack, "Idle to attack", pre, post);

        // Provoke Minotaur
        // Method 0: Move to Attack
        // Method 1: Attack
        // Method 2: Idle to attack
        methods = new List<List<Task>>{ new List<Task>(){ moveToAttack }, new List<Task>(){ attack }, new List<Task>(){ idleToAttack } };
        methodSelector = (StateVector state) => 
        { 
            List<List<Task>> meths = new List<List<Task>>();

            if(state.dist_minotaur > INTERACT_WITH_OBJECT_DISTANCE){
                return new List<List<Task>>(){ new List<Task>(){ moveToAttack } };
            } else if (attack.preconditionChecker(state))
            {
                return new List<List<Task>>(){ new List<Task>(){ attack } };
            } else 
            {
                return new List<List<Task>>(){ new List<Task>(){ idleToAttack } };
            }
        };
        CompositeTask provokeMinotaur = new CompositeTask(methods, methodSelector);

        // Be Melee
        // Method 0: Flee Minotaur
        // Method 1: Claim Treasure
        methods = new List<List<Task>>{ new List<Task>(){ fleeMinotaur }, new List<Task>(){ claimTreasure }, new List<Task>(){ provokeMinotaur } };
        methodSelector = (StateVector state) => 
        { 
            List<List<Task>> meths = new List<List<Task>>();

            if(state.dist_minotaur <= FLEE_DISTANCE)
            {
                meths.Add(new List<Task>(){ fleeMinotaur });
            } 
            if(state.treasure_holder == null || state.treasure_holder == this)
            {
                meths.Add(new List<Task>(){ claimTreasure });
            }
            if(state.treasure_holder != null && state.treasure_holder != this)
            {
                meths.Add(new List<Task>(){ provokeMinotaur });
            }

            return meths;
        };
        CompositeTask beMelee = new CompositeTask(methods, methodSelector);

        headTask = beMelee;
    }

    void InitRangedHTN()
    {
        // Flee Minotaur
        Func<StateVector, bool> pre = (StateVector state) => { return state.dist_minotaur < FLEE_DISTANCE; };
        Func<StateVector, StateVector> post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask fleeMinotaur = new BasicTask(StartFleeMinotaur, "Flee minotaur", pre, post);

        // Move to Treasure
        pre = (StateVector state) => { return state.dist_treasure < FLEE_DISTANCE; };
        post = (StateVector state) => { state.dist_minotaur = FLEE_DISTANCE; return state; };
        BasicTask moveToTreasure = new BasicTask(StartMoveToTreasure, "Move to treasure", pre, post);

        // Grab Treasure
        pre = (StateVector state) => { return state.seconds_dropped_treasure > PICKUP_TREASURE_DELAY && state.dist_treasure <= INTERACT_WITH_OBJECT_DISTANCE; };
        post = (StateVector state) => { state.treasure_holder = this; return state; };
        BasicTask grabTreasure = new BasicTask(StartGrabTreasure, "Grab treasure", pre, post);

        // Move to Corner
        pre = (StateVector state) => { return state.dist_corner > INTERACT_WITH_OBJECT_DISTANCE && state.treasure_holder == this; };
        post = (StateVector state) => { state.dist_corner = INTERACT_WITH_OBJECT_DISTANCE; return state; };
        BasicTask moveToCorner = new BasicTask(StartMoveToCorner, "Move to corner", pre, post);

        // Claim Treasure
        // Method 0: Move to Treasure
        // Method 1: Grab treasure
        // Method 2: Move to corner (to claim victory)
        List<List<Task>> methods = new List<List<Task>>{ new List<Task>(){ moveToTreasure }, new List<Task>(){ grabTreasure }, new List<Task>(){ moveToCorner } };
        Func<StateVector, List<List<Task>>> methodSelector = (StateVector state) => 
        { 
            if(state.treasure_holder == null){
                return new List<List<Task>>(){ new List<Task>(){ moveToTreasure } };
            } else
            {
                return new List<List<Task>>();
            }
        };
        CompositeTask claimTreasure = new CompositeTask(methods, methodSelector);

        // Move to Attack
        pre = (StateVector state) => { return !state.line_of_sight; };
        post = (StateVector state) => { state.line_of_sight = true; return state; };
        BasicTask moveToAttack = new BasicTask(StartMoveToAttack_Ranged, "Move to attack", pre, post);

        // Attack
        pre = (StateVector state) => { return state.line_of_sight && state.seconds_attack > ATTACK_COOLDOWN && state.seconds_damaged > ATTACK_COOLDOWN; };
        post = (StateVector state) => { state.seconds_attack = 0; return state; };
        BasicTask attack = new BasicTask(StartAttack_Ranged, "Attack", pre, post);

        // Idle to attack
        pre = (StateVector state) => { return state.seconds_attack <= ATTACK_COOLDOWN || state.seconds_damaged <= ATTACK_COOLDOWN; };
        post = (StateVector state) => { state.seconds_attack = 2; state.seconds_damaged = 2; return state; };
        BasicTask idleToAttack = new BasicTask(StartIdleToAttack, "Idle to attack", pre, post);

        // Provoke Minotaur
        // Method 0: Move to Attack
        // Method 1: Attack
        // Method 2: Idle to attack
        methods = new List<List<Task>>{ new List<Task>(){ moveToAttack }, new List<Task>(){ attack }, new List<Task>(){ idleToAttack } };
        methodSelector = (StateVector state) => 
        { 
            List<List<Task>> meths = new List<List<Task>>();

            if(state.dist_minotaur > INTERACT_WITH_OBJECT_DISTANCE){
                return new List<List<Task>>(){ new List<Task>(){ moveToAttack } };
            } else if (attack.preconditionChecker(state))
            {
                return new List<List<Task>>(){ new List<Task>(){ attack } };
            } else 
            {
                return new List<List<Task>>(){ new List<Task>(){ idleToAttack } };
            }
        };
        CompositeTask provokeMinotaur = new CompositeTask(methods, methodSelector);

        // Be Melee
        // Method 0: Flee Minotaur
        // Method 1: Claim Treasure
        methods = new List<List<Task>>{ new List<Task>(){ fleeMinotaur }, new List<Task>(){ claimTreasure }, new List<Task>(){ provokeMinotaur } };
        methodSelector = (StateVector state) => 
        { 
            List<List<Task>> meths = new List<List<Task>>();

            if(state.dist_minotaur <= FLEE_DISTANCE)
            {
                meths.Add(new List<Task>(){ fleeMinotaur });
            } 
            if(state.dist_minotaur > FLEE_DISTANCE)
            {
                meths.Add(new List<Task>(){ provokeMinotaur });
            }
            if(state.treasure_holder == null || state.treasure_holder == this)
            {
                meths.Add(new List<Task>(){ claimTreasure });
            }

            return meths;
        };
        CompositeTask beMelee = new CompositeTask(methods, methodSelector);

        headTask = beMelee;
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
                //Debug.Log("Exception caught: "+e);
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

        yield return StartCoroutine(FollowPath(path, end));

        currentTask = null;
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

        yield return StartCoroutine(FollowPath(path, end));

        currentTask = null;
    }

    public void StartGrabTreasure()
    {
        StopAllCoroutines();
        StartCoroutine(GrabTreasure());
    }
    
    IEnumerator GrabTreasure()
    {
        yield return new WaitForSeconds(PICKUP_TREASURE_DELAY);
        TreasureController.TREASURE.PickupTreasure(this);

        currentTask = null;
    }

    public void StartMoveToCorner()
    {
        StopAllCoroutines();
        StartCoroutine(MoveToCorner());
    }

    IEnumerator MoveToCorner()
    {
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

        if (closestCorner == null)
        {
            // Reset plan
            currentTask = null;
            plan = new List<BasicTask>();
        }

        List<Partition> path = AStar.FindPath(navMesh.GetPartition(transform.position), closestCorner, gameObject);

        yield return StartCoroutine(FollowPath(path, closestCorner));

        currentTask = null;
    }

    public void StartMoveToAttack_Melee()
    {
        StopAllCoroutines();
        StartCoroutine(MoveToAttack_Melee());
    }

    IEnumerator MoveToAttack_Melee()
    {
        List<Partition> adjToMinotaur = navMesh.GetPartition(MinotaurController.MINOTAUR.transform.position).GetConnectedPartitions();
        Partition end = adjToMinotaur[UnityEngine.Random.Range(0, adjToMinotaur.Count)];
        List<Partition> path = AStar.FindPath(navMesh.GetPartition(transform.position), end, gameObject);

        yield return StartCoroutine(FollowPath(path, end));

        currentTask = null;
    }

    public void StartMoveToAttack_Ranged()
    {
        StopAllCoroutines();
        StartCoroutine(MoveToAttack_Ranged());
    }

    IEnumerator MoveToAttack_Ranged()
    {


        int rotations = 16;

        Vector3 attackPoint = Vector3.forward * RANGED_IDEAL_ATTACK_DIST;
        Partition bestAttackPoint = null;
        float bestAttackPointDist = -1;
        
        for (int j = 0; j < rotations; j++)
        {
            Partition goal = null;
            try
            {
                goal = navMesh.GetPartition(MinotaurController.MINOTAUR.transform.position + attackPoint);
            } catch (Exception e)
            {
            }

            if (goal != null)
            {
                float pathDist = AStar.FindPathDistance(navMesh.GetPartition(transform.position), goal, new List<GameObject>(){gameObject});

                if (pathDist > 0)
                {
                    RaycastHit hit;
                    Ray ray = new Ray(gameObject.transform.position + HEIGHT_OFFSET_TO_GROUND * Vector3.up, MinotaurController.MINOTAUR.gameObject.transform.position - gameObject.transform.position);
                    Physics.Raycast(ray, out hit, 40, -1, QueryTriggerInteraction.Ignore);

                    if (hit.collider.gameObject == MinotaurController.MINOTAUR && (bestAttackPointDist == -1 || bestAttackPointDist > pathDist))
                    {
                        bestAttackPoint = goal;
                        bestAttackPointDist = pathDist;
                    }
                }
            }

            attackPoint = Quaternion.AngleAxis(360/rotations, Vector3.up) * attackPoint;
        }
        
        if (bestAttackPoint == null)
        {
            // Try again in a second if a position to attack from is not found
            yield return new WaitForSeconds(1);
            StartCoroutine(MoveToAttack_Ranged());
            yield break;
        }

        List<Partition> path = AStar.FindPath(navMesh.GetPartition(transform.position), bestAttackPoint, gameObject);

        yield return StartCoroutine(FollowPath(path, bestAttackPoint));
    }

    public void StartAttack_Melee()
    {
        StopAllCoroutines();
        MinotaurController.MINOTAUR.Attack(this);
        currentState.seconds_attack = 0;
        currentTask = null;
    }

    public void StartAttack_Ranged()
    {
        StopAllCoroutines();
        MinotaurController.MINOTAUR.Attack(this);
        currentState.seconds_attack = 0;
        currentTask = null;
    }

    public void StartIdleToAttack()
    {
        StopAllCoroutines();
    }

    IEnumerator IdleToAttack()
    {
        yield return new WaitForSeconds(ATTACK_COOLDOWN * 1.5f);
        currentTask = null;
    }

    IEnumerator FollowPath(List<Partition> path, Partition end)
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

        // End game condition
        if (currentState.dist_corner <= INTERACT_WITH_OBJECT_DISTANCE && currentState.treasure_holder == this)
        {
            PlayerInputController.VICTORY_TEXT.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        if (plan != null)
        {
            // Check if plan still works
            if (!HTN.DoesPlanStillWork(currentState, currentTask, plan))
            {
                Debug.Log("regen plan");
                plan = HTN.GeneratePlan(currentState, headTask);
                StopAllCoroutines();
                currentTask = null;
            }

            // Check if there is still a plan left
            if (plan.Count < 1 && currentTask == null)
            {
                plan = HTN.GeneratePlan(currentState, headTask);

                if (plan.Count < 1)
                {
                    StartCoroutine(IdleAndReplan());
                }
            } else if (currentTask == null)
            {
                currentTask = plan[0];
                plan.RemoveAt(0);

                currentTask._operator();
            }
        }
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
                                TreasureController.TREASURE.GetHolder(), MinotaurController.MINOTAUR.GetCurrentTarget(), LIVING_ADVENTURERS);

        // Display world state
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

        // Display current plan
        String currentTaskDescription = "";
        if (currentTask != null)
        {
            currentTaskDescription = currentTask.description;
        }
        texts[10].text = "Current task: "+ currentTaskDescription;
        
        String taskList = "";
        if (plan != null)
        {
            foreach (BasicTask basicTask in plan)
            {
                taskList = taskList + basicTask.description + ", ";
            }
        }
        texts[11].text = "Plan: "+taskList;

        currentState = newState;
    }

    // Waits to calculate a new plan
    IEnumerator IdleAndReplan()
    {
        Debug.Log("idling",this);
        plan = null;

        yield return new WaitForSeconds(1);

        plan = new List<BasicTask>();
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
                Debug.Log("Path blocked by: "+curPath[i].GetOccupied());
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
        healthBar.value -= 1;
        currentState.seconds_damaged = 0;
        if (healthBar.value == 0)
        {
            gameObject.SetActive(false);
        }
    }

    public enum AdventurerType
    {
        MELEE,
        RANGED
    }
}

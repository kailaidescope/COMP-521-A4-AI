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
    public static float PICKUP_TREASURE_DISTANCE = 1f;

    public AdventurerType adventurerType;
    public TextMeshProUGUI[] texts = new TextMeshProUGUI[10];

    private SpriteRenderer spriteRenderer;
    private CharacterController characterController;
    private Task headTask;
    private List<BasicTask> plan;
    private StateVector currentState;

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

        if (adventurerType == AdventurerType.MELEE)
        {

        } else 
        {

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
    }

    void InitRangedHTN()
    {

    }

    void StartFleeMinotaur()
    {

    }

    void StartMoveToTreasure()
    {

    }


    // Update is called once per frame
    void Update()
    {
        UpdateCurrentState();
    }

    void UpdateCurrentState()
    {
        StateVector newState = new StateVector((int) characterController.healthBar.value, Vector3.Distance(transform.position, MinotaurController.MINOTAUR.transform.position), 
                                Vector3.Distance(transform.position, TreasureController.TREASURE.transform.position), 0, currentState.seconds_attack + Time.deltaTime, 
                                currentState.seconds_damaged + Time.deltaTime, currentState.seconds_dropped_treasure + Time.deltaTime, true, TreasureController.TREASURE.GetHolder(), 
                                MinotaurController.MINOTAUR.GetCurrentTarget());

        texts[0].text = "Health: "+newState.health;
        texts[1].text = "Dist Mino: "+newState.dist_minotaur;
        texts[2].text = "Dist Tre: "+newState.dist_treasure;
        texts[3].text = "Dist Cor: "+newState.dist_corner;
        texts[4].text = "Sec Atk: "+newState.seconds_attack;
        texts[5].text = "Sec Dmg: "+newState.seconds_damaged;
        texts[6].text = "Sec Drop: "+newState.seconds_dropped_treasure;
        texts[7].text = "LoS: "+newState.line_of_sight;
        texts[8].text = "Tre Hold: "+newState.treasure_holder;
        texts[9].text = "Min Trg: "+newState.minotaur_target;

        currentState = newState;
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

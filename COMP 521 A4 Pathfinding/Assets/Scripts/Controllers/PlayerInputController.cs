using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public static TextMeshProUGUI VICTORY_TEXT;

    public CharacterController character1;
    public CharacterController character2;
    public TextMeshProUGUI victoryText;

    private new Camera camera;
    private NavMesh navMesh;
    private AdventurerController adventurer1;
    private AdventurerController adventurer2;

    // Start is called before the first frame update
    void Start()
    {
        camera = FindObjectOfType<Camera>();
        navMesh = FindObjectOfType<NavMesh>();
        adventurer1 = character1.gameObject.GetComponent<AdventurerController>();
        adventurer2 = character2.gameObject.GetComponent<AdventurerController>();

        VICTORY_TEXT = victoryText;
        victoryText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                adventurer1.StartMoveToTreasure();
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                adventurer2.StartMoveToTreasure();
            }
        } else if (Input.GetKey(KeyCode.Alpha2))
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                adventurer1.StartFleeMinotaur();
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                adventurer2.StartFleeMinotaur();
            }
        } else
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                RaycastHit hit;
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out hit, 100, -1, QueryTriggerInteraction.Ignore)) 
                {
                    character1.SetTarget(hit.point);
                }
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                RaycastHit hit;
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out hit, 100, -1, QueryTriggerInteraction.Ignore)) 
                {
                    character2.SetTarget(hit.point);
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public AdventurerController adventurer1;
    public AdventurerController adventurer2;

    private new Camera camera;
    private NavMesh navMesh;

    // Start is called before the first frame update
    void Start()
    {
        camera = FindObjectOfType<Camera>();
        navMesh = FindObjectOfType<NavMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out hit)) 
            {
                adventurer1.SetTarget(hit.point);
            }
        }
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out hit)) 
            {
                adventurer2.SetTarget(hit.point);
            }
        }
    }
}

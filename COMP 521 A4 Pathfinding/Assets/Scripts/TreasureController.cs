using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureController : MonoBehaviour
{
    private AdventurerController holder = null;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool PickupTreasure(AdventurerController adventurer)
    {
        if (holder == null)
        {
            holder = adventurer;
            return true;
        } else
        {
            return false;
        }
    }

    public void DropTreasure()
    {
        holder = null;
    }

    public AdventurerController GetHolder()
    {
        return holder;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureController : MonoBehaviour
{
    public static TreasureController TREASURE;

    private AdventurerController holder = null;

    void Awake()
    {
        TREASURE = this;
    }

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

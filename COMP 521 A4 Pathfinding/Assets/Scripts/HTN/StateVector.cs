using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StateVector
{
    // Integers
    public int health;

    // Floats
    public float dist_minotaur;
    public float dist_treasure;
    public float dist_corner;
    public float seconds_attack;
    public float seconds_damaged;
    public float seconds_dropped_treasure;

    // Booleans
    public bool line_of_sight;

    // GameObjects    
    public GameObject treasure_holder;
    public GameObject minotaur_target;
}

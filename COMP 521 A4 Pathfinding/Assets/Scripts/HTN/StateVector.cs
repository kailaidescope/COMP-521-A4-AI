using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateVector
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

    public StateVector()
    {
        health = 6;
        
        dist_minotaur = 0;
        dist_treasure = 0;
        dist_corner = 0;
        seconds_attack = 1;
        seconds_damaged = 1;
        seconds_dropped_treasure = 3;
        
        line_of_sight = false;

        treasure_holder = null;
        minotaur_target = null;
    }
}

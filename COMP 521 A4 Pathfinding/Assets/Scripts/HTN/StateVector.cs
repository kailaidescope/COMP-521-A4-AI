using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StateVector
{
    public StateVector(int health, float dist_minotaur, float dist_treasure, float dist_corner, 
                        float seconds_attack, float seconds_damaged, float seconds_dropped_treasure, 
                        bool line_of_sight, AdventurerController treasure_holder, AdventurerController minotaur_target,
                        List<AdventurerController> living_adventurers)
    {
        this.health = health;
        this.dist_minotaur = dist_minotaur;
        this.dist_treasure = dist_treasure;
        this.dist_corner = dist_corner;
        this.seconds_attack = seconds_attack;
        this.seconds_damaged = seconds_damaged;
        this.seconds_dropped_treasure = seconds_dropped_treasure;
        this.line_of_sight = line_of_sight;
        this.treasure_holder = treasure_holder;
        this.minotaur_target = minotaur_target;
        this.living_adventurers = living_adventurers;
    }

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
    public AdventurerController treasure_holder;
    public AdventurerController minotaur_target;

    // Lists of GameObjects
    public List<AdventurerController> living_adventurers;
}

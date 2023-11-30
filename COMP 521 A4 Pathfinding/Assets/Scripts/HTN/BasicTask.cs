using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BasicTask : Task
{
    public Action _operator;
    public String description;
    // Engine for CheckPreconditions
    public Func<StateVector, bool> preconditionChecker;
    // Engine for ApplyPostconditions
    public Func<StateVector, StateVector> postconditionApplier;

    public BasicTask(Action op, String description, Func<StateVector, bool> pre, Func<StateVector, StateVector> post)
    {
        _operator = op;
        this.description = description;
        preconditionChecker = pre;
        postconditionApplier = post;
    }

    public bool CheckPreconditions(StateVector state)
    {
        return preconditionChecker(state);
    }
    
    public StateVector ApplyPostconditions(StateVector state)
    {
        return postconditionApplier(state);
    }
}

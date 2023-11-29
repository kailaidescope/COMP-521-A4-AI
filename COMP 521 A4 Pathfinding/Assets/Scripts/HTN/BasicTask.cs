using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BasicTask : Task
{
    public Action _operator;
    public String description;

    public BasicTask(Action op, String description, Func<StateVector, bool> pre, Func<StateVector, StateVector> post)
    {
        _operator = op;
        this.description = description;
        preconditionChecker = pre;
        postconditionApplier = post;
    }

    public override bool CheckPreconditions(StateVector state)
    {
        return preconditionChecker(state);
    }
    
    public override StateVector ApplyPostconditions(StateVector state)
    {
        return postconditionApplier(state);
    }
}

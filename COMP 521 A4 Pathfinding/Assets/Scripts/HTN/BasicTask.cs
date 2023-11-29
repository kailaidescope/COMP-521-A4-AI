using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BasicTask : Task
{
    public Action _operator;

    public BasicTask(Action op, Func<StateVector, bool> pre, Func<StateVector, StateVector> post)
    {
        _operator = op;
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

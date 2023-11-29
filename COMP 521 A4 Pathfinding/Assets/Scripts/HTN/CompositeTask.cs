using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompositeTask : Task
{
    public List<List<Task>> methods;

    public CompositeTask(List<List<Task>> methods, Func<StateVector, bool> pre, Func<StateVector, StateVector> post)
    {
        this.methods = methods;
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

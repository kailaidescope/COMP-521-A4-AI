using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Task
{
    // Engine for CheckPreconditions
    public Func<StateVector, bool> preconditionChecker;
    // Engine for ApplyPostconditions
    public Func<StateVector, StateVector> postconditionApplier;

    // Checks whether state fulfills the preconditions of this task
    public abstract bool CheckPreconditions(StateVector state);

    // Applies the postconditions of this task to a StateVector
    public abstract StateVector ApplyPostconditions(StateVector state);
}

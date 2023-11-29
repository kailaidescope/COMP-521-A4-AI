using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Task
{
    public Action _operator;
    public List<List<Task>> methods;
    public Func<StateVector, bool> preconditionChecker;
    public Func<StateVector, StateVector> postconditionApplier;

    public abstract bool CheckPreconditions(StateVector state);
    public abstract StateVector ApplyPostconditions(StateVector state);
}

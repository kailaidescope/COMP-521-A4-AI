using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompositeTask : Task
{
    public List<List<Task>> methods;
    public Func<StateVector,List<List<Task>>> methodSelector;

    public CompositeTask(List<List<Task>> methods, Func<StateVector, List<List<Task>>> methodSelector)
    {
        this.methods = methods;
        this.methodSelector = methodSelector;
    }
}

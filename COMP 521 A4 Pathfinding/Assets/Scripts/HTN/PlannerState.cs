using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlannerRecord
{
    
    public PlannerRecord(Stack<Task> tasksToProcess, List<BasicTask> finalPlan, CompositeTask decomposedTask, int chosenMethod, StateVector workingState)
    {
        this.tasksToProcess = tasksToProcess;
        this.finalPlan = finalPlan;
        this.decomposedTask = decomposedTask;
        this.selectedMethod = chosenMethod;
        this.workingState = workingState;
    }

    public Stack<Task> tasksToProcess;
    public List<BasicTask> finalPlan;
    public CompositeTask decomposedTask;
    public int selectedMethod;
    public StateVector workingState;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HTN
{

    public static List<BasicTask> GeneratePlan(StateVector currentState, Task headTask)
    {
        // Planner history
        Stack<PlannerRecord> decompositionHistory = new Stack<PlannerRecord>(); 

        // Planner attributes
        StateVector workingState = currentState;
        PlannerRecord lastDecomposition = new PlannerRecord(null, null, null, 0, new StateVector());
        Stack<Task> tasksToProcess = new Stack<Task>();
        List<BasicTask> finalPlan = new List<BasicTask>();

        tasksToProcess.Push(headTask);

        while (tasksToProcess.Count > 0)
        {
            Task currentTask = tasksToProcess.Pop();

            //Debug.Log("Task in planning: "+currentTask);

            // If task is composite
            if(currentTask.GetType() == typeof(CompositeTask))
            {
                CompositeTask compositeTask = (CompositeTask) currentTask;
                List<List<Task>> satisfiedMethods = compositeTask.methodSelector(workingState);
                int selectedMethod = 0;

                if (lastDecomposition.decomposedTask == compositeTask)
                {
                    selectedMethod = lastDecomposition.selectedMethod + 1;
                }

                // If there exists a method that works
                if (selectedMethod < satisfiedMethods.Count)
                {
                    // Record planner state
                    decompositionHistory.Push(new PlannerRecord(tasksToProcess, finalPlan, compositeTask, selectedMethod, workingState));
                    
                    // Push tasks from selected method
                    foreach (Task t in satisfiedMethods[selectedMethod])
                    {
                        tasksToProcess.Push(t);
                    }
                } else // Otherwise
                {
                    // Restore to last decomposed state
                    lastDecomposition = decompositionHistory.Pop();
                    workingState = lastDecomposition.workingState;
                    tasksToProcess = lastDecomposition.tasksToProcess;
                    finalPlan = lastDecomposition.finalPlan;
                }
            } else // If it is basic
            {
                BasicTask basicTask = (BasicTask) currentTask;

                // If preconditions hold
                if (basicTask.preconditionChecker(workingState))
                {
                    // Apply task to working state
                    workingState = basicTask.ApplyPostconditions(workingState);
                    finalPlan.Add(basicTask);
                } else // Otherwise
                {
                    // Restore to last decomposed state
                    lastDecomposition = decompositionHistory.Pop();
                    workingState = lastDecomposition.workingState;
                    tasksToProcess = lastDecomposition.tasksToProcess;
                    finalPlan = lastDecomposition.finalPlan;
                }
            }
        }

        return finalPlan;
    }

    public static bool DoesPlanStillWork(StateVector currentState, BasicTask currentTask, List<BasicTask> tasks)
    {
        if (currentTask != null)
        {
            if (currentTask.CheckPreconditions(currentState))
            {
                currentState = currentTask.ApplyPostconditions(currentState);
            } else
            {
                return false;
            }
        }

        foreach (BasicTask task in tasks)
        {
            if (task.CheckPreconditions(currentState))
            {
                currentState = task.ApplyPostconditions(currentState);
            } else
            {
                return false;
            }
        }

        return true;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityHFSM;

public static class StateMachineExtensions
{
    public static string GetFullStatePath(this StateMachine stateMachine)
    {
        if (stateMachine == null)
            return string.Empty;

        var path = new List<string>();
        BuildStatePath(stateMachine, path);
        return string.Join("/", path);
    }

    private static void BuildStatePath(StateMachine stateMachine, List<string> path)
    {
        if (stateMachine == null)
            return;

        path.Add(stateMachine.name);

        var activeState = stateMachine.ActiveState;
        if (activeState is StateMachine childStateMachine)
        {
            BuildStatePath(childStateMachine, path);
        }
        else if (activeState != null)
        {
            path.Add(activeState.name);
        }
    }
}
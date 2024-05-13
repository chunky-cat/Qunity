using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class QunityEventEntry
{
    public string name;
    public UnityEvent unityEvent = new UnityEvent();

    public QunityEventEntry(string targetName)
    {
        name = targetName;
    }
}

public class QunityEventBus : MonoBehaviour
{
    public string test;
    public List<QunityEventEntry> eventList = new List<QunityEventEntry>();


    public void FireEvent(string targetName)
    {
        var ev = FindEvent(targetName);
        if (ev != null)
        {
            ev.Invoke();
        }
    }

    public UnityEvent FindEvent(string targetName)
    {
        foreach (var ev in eventList)
        {
            if (ev.name == targetName) return ev.unityEvent;
        }
        return null;
    }
}

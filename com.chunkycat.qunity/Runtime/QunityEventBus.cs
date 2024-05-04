using System.Collections.Generic;
using UnityEditor.Events;
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
    public void AddEvent(string targetName, UnityAction cb)
    {
        var ev = findEvent(targetName);
        if (ev == null)
        {
            var uev = new QunityEventEntry(targetName);
            eventList.Add(uev);
            UnityEventTools.AddVoidPersistentListener(uev.unityEvent, cb);
            return;
        }
        UnityEventTools.AddVoidPersistentListener(ev, cb);
    }

    public void FireEvent(string targetName)
    {
        var ev = findEvent(targetName);
        if (ev != null)
        {
            ev.Invoke();
        }
    }

    private UnityEvent findEvent(string targetName)
    {
        foreach (var ev in eventList)
        {
            if (ev.name == targetName) return ev.unityEvent;
        }
        return null;
    }
}

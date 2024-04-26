using UnityEngine;


public class EntityEventEmitter : MonoBehaviour
{
    public string target;
    public bool once;
    protected bool triggered = false;

    public void SetTarget(string targetName)
    {
        target = targetName;
    }

    public virtual void TriggerEntered(Collider col)
    {
        if (target != "" && (!once || !triggered))
        {
            QunityEventBus.GetInstance().FireEvent(target);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEntered(other);
    }
}

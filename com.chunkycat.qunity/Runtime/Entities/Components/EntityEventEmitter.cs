using UnityEngine;


public class EntityEventEmitter : MonoBehaviour
{
    public string target;
    public bool once;
    protected bool triggered = false;

    [SerializeField] protected QunityEventBus localEventBus;

    public void SetTarget(string targetName)
    {
        target = targetName;
    }

    public void SetLocalEventBus(QunityEventBus eb)
    {
        localEventBus = eb;
    }

    public virtual void TriggerEntered(Collider col)
    {
        if (target != "" && (!once || !triggered))
        {
            localEventBus.FireEvent(target);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEntered(other);
    }
}

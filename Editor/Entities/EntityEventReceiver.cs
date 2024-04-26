using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityEventReceiver : MonoBehaviour
{
    public string targetName;
    public void OnTrigger()
    {
        Debug.Log(targetName + " got triggered");
    }
}

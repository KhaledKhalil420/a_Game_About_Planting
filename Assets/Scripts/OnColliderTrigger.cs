using UnityEngine;
using UnityEngine.Events;

public class OnColliderTrigger : MonoBehaviour
{
    public UnityEvent Event;

    void OnTriggerEnter(Collider other)
    {
        Event?.Invoke();
    }
}

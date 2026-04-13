using UnityEngine;

public class WwiseEventCaller : MonoBehaviour
{
    [Header("Assign a Wwise Event here")]
    public AK.Wwise.Event wwiseEvent;

    [Header("Optional: post on this GameObject instead of this component's object")]
    public GameObject targetObject;

    public void Play()
    {
        GameObject postTarget = targetObject != null ? targetObject : gameObject;

        if (wwiseEvent == null)
        {
            Debug.LogWarning($"No Wwise event assigned on {name}", this);
            return;
        }

        wwiseEvent.Post(postTarget);
    }

    public void Stop()
    {
        GameObject postTarget = targetObject != null ? targetObject : gameObject;

        if (wwiseEvent == null)
        {
            Debug.LogWarning($"No Wwise event assigned on {name}", this);
            return;
        }

        wwiseEvent.Stop(postTarget);
    }
}
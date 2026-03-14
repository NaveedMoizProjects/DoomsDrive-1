using UnityEngine;
public class EffectStopper : MonoBehaviour
{
    public void StopAfter(float delay)
    {
        CancelInvoke();
        Invoke("Disable", delay);
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }
}
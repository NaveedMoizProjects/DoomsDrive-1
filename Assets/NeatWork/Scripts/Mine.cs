using UnityEngine;
using System.Collections;

public class Mine : MonoBehaviour
{
    public GameObject explosionEffect;
    public float delay = 2f; // seconds delay

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(ExplodeAfterDelay(other.gameObject));
        }
    }

    IEnumerator ExplodeAfterDelay(GameObject car)
    {

        Instantiate(explosionEffect, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(delay);
        Destroy(car);

        GameplayManagerMine.instance.LevelFail();
    }
}
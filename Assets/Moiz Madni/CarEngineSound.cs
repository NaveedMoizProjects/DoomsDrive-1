using UnityEngine;

public class CarEngineSound : MonoBehaviour
{
    public float minSpeed = 0f;
    public float maxSpeed = 200f;

    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;

    private AudioSource carAudio;
    private Rigidbody carRb;

    private float currentSpeed;
    private float pitchFromCar;

    void Start()
    {
        carAudio = GetComponent<AudioSource>();
        carRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        EngineSound();
    }

    void EngineSound()
    {
        // Normalizes speed between 0 and 1
        float speedPercentage = carRb.velocity.magnitude / maxSpeed;

        // Smoothly moves pitch between min and max based on speed
        carAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercentage);
    }
}
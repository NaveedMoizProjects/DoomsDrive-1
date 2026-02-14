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
        currentSpeed = carRb.velocity.magnitude;
        pitchFromCar = carRb.velocity.magnitude / 50f;

        if (currentSpeed < minSpeed)
        {
            carAudio.pitch = minPitch;
        }

        if (currentSpeed >= minSpeed && currentSpeed < maxSpeed)
        {
            carAudio.pitch = minPitch + pitchFromCar;
        }

        if (currentSpeed >= maxSpeed)
        {
            carAudio.pitch = maxPitch;
        }
    }
}
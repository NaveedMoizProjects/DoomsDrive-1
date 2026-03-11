using UnityEngine;

public class WheelEffectsHandler : MonoBehaviour
{
    private DynamicCarController controller;
    private Rigidbody carRb;

    [Header("Central Audio")]
    public AudioSource globalSkidAudio; // Drag your single AudioSource here

    [Header("Settings")]
    public float minSpeedForVFX = 2.0f;
    public float slipThreshold = 0.2f; // How much sliding before effects start

    void Start()
    {
        controller = GetComponent<DynamicCarController>();
        carRb = GetComponent<Rigidbody>();

        // Setup the audio source
        if (globalSkidAudio)
        {
            globalSkidAudio.loop = true;
            globalSkidAudio.playOnAwake = false;
        }
    }

    void Update()
    {
        float maxSlipFound = 0f;
        bool anyWheelSkidding = false;
        bool isBraking = Input.GetKey(KeyCode.Space);

        foreach (var wheel in controller.wheels)
        {
            // If wheel is detached (Collider is null), stop its particles and skip
            if (wheel.wheelCollider == null)
            {
                if (wheel.sandParticles && wheel.sandParticles.isPlaying)
                    wheel.sandParticles.Stop();
                continue;
            }

            WheelHit hit;
            if (wheel.wheelCollider.GetGroundHit(out hit))
            {
                // Calculate slip intensity
                float combinedSlip = Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip);

                // Logic: Is the car actually doing something that makes noise/dust?
                bool isSkidding = (isBraking && carRb.velocity.magnitude > minSpeedForVFX) || (combinedSlip > slipThreshold);

                if (isSkidding)
                {
                    anyWheelSkidding = true;
                    if (combinedSlip > maxSlipFound) maxSlipFound = combinedSlip;

                    // Start Particles
                    if (wheel.sandParticles && !wheel.sandParticles.isPlaying)
                        wheel.sandParticles.Play();
                }
                else
                {
                    // Stop Particles if this specific wheel isn't skidding
                    if (wheel.sandParticles && wheel.sandParticles.isPlaying)
                        wheel.sandParticles.Stop();
                }
            }
            else
            {
                // Stop particles if the wheel is in the air
                if (wheel.sandParticles && wheel.sandParticles.isPlaying)
                    wheel.sandParticles.Stop();
            }
        }

        // --- Handle the Single Audio Source ---
        if (anyWheelSkidding && globalSkidAudio)
        {
            if (!globalSkidAudio.isPlaying) globalSkidAudio.Play();

            // Volume is based on the most aggressive slip happening right now
            globalSkidAudio.volume = Mathf.Lerp(globalSkidAudio.volume, Mathf.Clamp01(maxSlipFound), Time.deltaTime * 10f);
            // Pitch shift slightly for realism (faster slip = higher pitch)
            globalSkidAudio.pitch = Mathf.Lerp(0.8f, 1.2f, maxSlipFound);
        }
        else if (globalSkidAudio && globalSkidAudio.isPlaying)
        {
            // Fade out the sound instead of cutting it instantly
            globalSkidAudio.volume = Mathf.Lerp(globalSkidAudio.volume, 0, Time.deltaTime * 10f);
            if (globalSkidAudio.volume < 0.01f) globalSkidAudio.Stop();
        }
    }
}
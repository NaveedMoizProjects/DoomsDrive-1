using UnityEngine;

public class CarMovement : MonoBehaviour
{
    [Header("Nitro Boost Settings")]
    public ParticleSystem Nitro;
    public float boostMultiplier = 2f;
    public bool isNitroActive = false;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    private float currentSteerAngle = 0f;
    public float steerSmoothSpeed = 5f;

    [Header("Engine")]
    public AudioSource EngineAudio;

    [Header("Car Settings")]
    public float motorTorque = 4000f;
    public float steeringAngle = 25f;
    public float brakeTorque = 3000f;
    public float accelerationSmooth = 5f; // optional for smoother acceleration
    private float currentMotorInput = 0f;
    public enum DriveType { FrontWheelDrive, RearWheelDrive, AllWheelDrive }
    public DriveType driveType = DriveType.RearWheelDrive;

    public enum PlayerID { Player1, Player2 }
    public PlayerID playerID;

    private Rigidbody rb;
    private void Awake()
    {
        
    }

    void Start()
    {
        if(EngineAudio!= null)
        {
            EngineAudio.volume = 0.2f;
            EngineAudio.loop = true;
        }
        // Setup Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.mass = 5000f;
            //rb.drag = 0.2f;
            //rb.angularDrag = 0.2f;
            //rb.centerOfMass = new Vector3(0, -0.5f, 0);  // Lower center of gravity
        }
        rb.centerOfMass = new Vector3(0, -0.5f, 0);  // Stabilize turning

        // Setup friction
        ApplyWheelFriction(frontLeftWheel);
        ApplyWheelFriction(frontRightWheel);
        ApplyWheelFriction(rearLeftWheel);
        ApplyWheelFriction(rearRightWheel);
    }

    private void FixedUpdate()
    {
        //rb.AddForce(-transform.up * 0.1f); // Simulate aerodynamic downforce
        float motorInput = 0f;
        float steerInput = 0f;
        float brakeInput = 0f;

        
        // Get Input based on player
        if (playerID == PlayerID.Player1)
        {
            if (Input.GetKey(ControlSettingsManager.Instance.p1_Forward))
                motorInput = 1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p1_Brake))
                brakeInput = brakeTorque;
            if (Input.GetKey(ControlSettingsManager.Instance.p1_Left))
                steerInput = -1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p1_Right))
                steerInput = 1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p1_Boost))
                isNitroActive = true;
            else
                isNitroActive = false;
        }
        else if (playerID == PlayerID.Player2)
        {
            if (Input.GetKey(ControlSettingsManager.Instance.p2_Forward))
                motorInput = 1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p2_Brake))
                brakeInput = brakeTorque;
            if (Input.GetKey(ControlSettingsManager.Instance.p2_Left))
                steerInput = -1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p2_Right))
                steerInput = 1f;
            if (Input.GetKey(ControlSettingsManager.Instance.p2_Boost))
                isNitroActive = true;
            else
                isNitroActive = false;
        }

        // Smooth acceleration (optional)
        currentMotorInput = Mathf.Lerp(currentMotorInput, motorInput, accelerationSmooth * Time.fixedDeltaTime);

        ApplyDrive(currentMotorInput, steerInput, brakeInput);
        UpdateWheelMeshes();
    }

    private void ApplyDrive(float motorInput, float steerInput, float brakeInput)
    {
        float appliedMotor = motorTorque * motorInput;
        if (isNitroActive)
            appliedMotor *= boostMultiplier;

        float targetSteerAngle = steeringAngle * steerInput;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSmoothSpeed * Time.fixedDeltaTime);

        // Motor torque
        if (driveType == DriveType.FrontWheelDrive || driveType == DriveType.AllWheelDrive)
        {
            frontLeftWheel.motorTorque = appliedMotor;
            frontRightWheel.motorTorque = appliedMotor;
        }

        if (driveType == DriveType.RearWheelDrive || driveType == DriveType.AllWheelDrive)
        {
            rearLeftWheel.motorTorque = appliedMotor;
            rearRightWheel.motorTorque = appliedMotor;
        }

        // Steering
        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;

        // Braking
        frontLeftWheel.brakeTorque = brakeInput;
        frontRightWheel.brakeTorque = brakeInput;
        rearLeftWheel.brakeTorque = brakeInput;
        rearRightWheel.brakeTorque = brakeInput;

        // Audio feedback based on average wheel RPM
        float avgRPM = (
            Mathf.Abs(frontLeftWheel.rpm) +
            Mathf.Abs(frontRightWheel.rpm) +
            Mathf.Abs(rearLeftWheel.rpm) +
            Mathf.Abs(rearRightWheel.rpm)
        ) / 4f;

        float rpmNormalized = Mathf.InverseLerp(0f, 1000f, avgRPM);

        if (EngineAudio != null)
        {
            EngineAudio.volume = Mathf.Lerp(0.2f, 1f, rpmNormalized);
            EngineAudio.pitch = Mathf.Lerp(0.8f, 2.2f, rpmNormalized);
        }

        // Nitro particle toggle
        if (Nitro != null)
        {
            if (isNitroActive && !Nitro.isPlaying)
                Nitro.Play();
            else if (!isNitroActive && Nitro.isPlaying)
                Nitro.Stop();
        }
    }

    private void UpdateWheelMeshes()
    {
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    private void ApplyWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;

        forwardFriction.stiffness = 1.5f;
        sidewaysFriction.stiffness = 2f;

        wheel.forwardFriction = forwardFriction;
        wheel.sidewaysFriction = sidewaysFriction;
    }
}

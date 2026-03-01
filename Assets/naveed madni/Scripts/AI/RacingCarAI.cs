using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RacingCarAI : MonoBehaviour
{
    public Transform[] waypoints;
    public float waypointRadius = 8f;

    public float maxSpeed = 70f;
    public float acceleration = 25f;
    public float steeringPower = 4f;
    public float turnSlowFactor = 0.6f;

    private Rigidbody rb;
    private int currentWaypoint;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // prevent tilt problems

        float distance = direction.magnitude;

        if (distance < waypointRadius)
        {
            currentWaypoint++;
            if (currentWaypoint >= waypoints.Length)
                currentWaypoint = 0;
        }

        direction.Normalize();

        // Smooth rotation (NO SPIN)
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            steeringPower * Time.fixedDeltaTime
        );

        // Angle based slow down
        float angle = Vector3.Angle(transform.forward, direction);
        float speedFactor = Mathf.Clamp01(1 - (angle / 90f));
        float targetSpeed = maxSpeed * Mathf.Lerp(turnSlowFactor, 1f, speedFactor);

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        rb.velocity = transform.forward * currentSpeed;
    }
}
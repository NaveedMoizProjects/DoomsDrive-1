using Cinemachine; // For handling Cinemachine virtual cameras
using StarterAssets; // Starter Assets package (ThirdPersonController, Inputs, etc.)
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera; // Camera used when aiming
    [SerializeField] private float normalSensitivity; // Sensitivity when not aiming
    [SerializeField] private float aimSensitivity; // Sensitivity when aiming
    [SerializeField] private Transform debugTransform; // Debug transform to visualize raycast hit
    [SerializeField] private Transform bulletProjectilepf; // Prefab for bullet projectile
    [SerializeField] private Transform spawnProjectilepf; // Spawn point for projectile
    [SerializeField] private LayerMask aimColliderlayerMask = new LayerMask(); // Layer mask for aiming raycast
    private Animator animator; // Animator reference
    private ThirdPersonController thirdPersonController; // Reference to movement controller
    private StarterAssetsInputs starterAssetsInputs; // Reference to input system

    private void Awake()
    {
        // Cache references on Awake
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Default mouse world position
        Vector3 mouseWorldPosition = Vector3.zero;

        // Get screen center point
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Create ray from camera center
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

        // Perform raycast to detect aim target
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderlayerMask))
        {
            debugTransform.position = raycastHit.point; // Move debug transform to hit point
            mouseWorldPosition = raycastHit.point; // Store hit point as aim target
        }

        // If player is aiming
        if (starterAssetsInputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true); // Enable aim camera
            thirdPersonController.SetSensitivity(aimSensitivity); // Reduce sensitivity for aiming
            thirdPersonController.SetOnrotate(false); // Disable auto-rotation

            // Calculate aim direction
            Vector3 worldaimTarget = mouseWorldPosition;
            worldaimTarget.y = transform.position.y; // Keep aim target on same Y level
            Vector3 aimDirection = (worldaimTarget - transform.position).normalized;

            // Smoothly rotate player towards aim direction
            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);

            // Smoothly blend in upper-body aiming animation layer
            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);

            thirdPersonController.SetOnrotate(true);   // 👈 YE LINE ADD KARO

            animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));
        }

        // Shooting logic
        if (starterAssetsInputs.shoot)
        {
            // Calculate direction from spawn point to aim target
            Vector3 aimDir = (mouseWorldPosition - spawnProjectilepf.position).normalized;

            // Instantiate bullet projectile facing aim direction
            Instantiate(bulletProjectilepf, spawnProjectilepf.position, Quaternion.LookRotation(aimDir, Vector3.up));

            // Reset shoot input to prevent continuous firing
            starterAssetsInputs.shoot = false;
        }
    }
}
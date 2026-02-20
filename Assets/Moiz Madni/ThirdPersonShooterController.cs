using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private Transform debugTransform;
    [SerializeField] private Transform bulletProjectilepf;
    [SerializeField] private Transform spawnProjectilepf;
    [SerializeField] private LayerMask aimColliderlayerMask=new LayerMask();

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        Vector3 mouseWorldPosition = Vector3.zero;
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderlayerMask))
        {
            debugTransform.position = raycastHit.point;
            mouseWorldPosition = raycastHit.point;
        }

        if (starterAssetsInputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);
            thirdPersonController.SetOnrotate(false);

            Vector3 worldaimTarget=mouseWorldPosition;
            worldaimTarget.y = transform.position.y;
            Vector3 aimDirection=(worldaimTarget-transform.position).normalized;
            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
        }

      
        if (starterAssetsInputs.shoot)
        {
            Vector3 aimDir = (mouseWorldPosition - spawnProjectilepf.position).normalized;
            Instantiate(bulletProjectilepf, spawnProjectilepf.position, Quaternion.LookRotation(aimDir, Vector3.up));
            starterAssetsInputs.shoot = false;
        }
    }
    
}

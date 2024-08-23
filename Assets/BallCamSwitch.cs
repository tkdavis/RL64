using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BallCamSwitch : MonoBehaviour
{
    public Transform playerTransform;
    public Transform ballTransform;

    private CinemachineVirtualCamera virtualCamera;

    private bool focusedOnBall = false;
    private Transform newTarget;
    private float distanceFromFollow = 2.0f;
    private CinemachineTransposer transposer;

    private void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire3"))
        {
            if (focusedOnBall)
            {
                newTarget = playerTransform;
                focusedOnBall = false;
                transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
                transposer.m_FollowOffset = new Vector3(0.0f, 0.75f, -2.0f);
                SetLookAtTarget();
            } else {
                newTarget = ballTransform;
                focusedOnBall = true;
                transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                SetLookAtTarget();
            }
        }

        if (virtualCamera != null && playerTransform != null && ballTransform != null && focusedOnBall)
        {
            // Calculate the direction from the follow target (player) to the look at target (ball)
            Vector3 directionToLookAt = (ballTransform.position - playerTransform.position).normalized;

            // Set the follow offset relative to the player, maintaining the desired distance
            Vector3 desiredOffset = directionToLookAt * -distanceFromFollow;  // Offset behind the player
            
            // Apply the follow offset
            transposer.m_FollowOffset = new Vector3(desiredOffset.x, Mathf.Max(0.75f, desiredOffset.y), desiredOffset.z);
        }
    }

    // Call this method to change the Look At target
    public void SetLookAtTarget()
    {
        if (virtualCamera != null)
        {
            virtualCamera.m_LookAt = newTarget;  // Set the new Look At target
        }
        else
        {
            Debug.LogWarning("Cinemachine Virtual Camera not assigned.");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPointController : MonoBehaviour
{
    public Transform playerTransform;
    public float aimOffset = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = playerTransform.forward * aimOffset;
    }
}

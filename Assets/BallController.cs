using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class BallController : MonoBehaviour
{
    public AudioSource ballBounceSfx;

    void Start()
    {
        ballBounceSfx = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision other)
    {
        ballBounceSfx.pitch = Random.Range(0.1f, 0.4f);
        ballBounceSfx.Stop();
        ballBounceSfx.Play();
    }
}

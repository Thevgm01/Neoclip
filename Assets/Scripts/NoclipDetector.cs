using System;
using UnityEngine;

public class NoclipDetector : MonoBehaviour
{
    private Collider collider;
    
    private int numEnters = 0;
    public bool IsInsideAnything => enabled && numEnters > 0;
    public bool IsOutsideEverything => !enabled || numEnters == 0;

    private void Awake()
    {
        collider = GetComponents<Collider>()[1]; // We're just hard-coding it to be the second one
    }

    private void OnEnable()
    {
        numEnters = 0;
        collider.enabled = true;
    }

    private void OnDisable()
    {
        numEnters = 0;
        collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        numEnters++;
    }
    
    private void OnTriggerExit(Collider other)
    {
        numEnters--;
    }
}

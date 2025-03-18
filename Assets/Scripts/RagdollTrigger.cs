using System;
using UnityEngine;

public class RagdollTrigger : MonoBehaviour
{
    private Collider collider;
    
    private int numEnters = 0;
    public bool InsideAnything => numEnters > 0;

    private void Awake()
    {
        collider = GetComponents<Collider>()[1]; // We're just hard-coding it to be the second one
    }

    private void OnEnable()
    {
        collider.enabled = true;
    }

    private void OnDisable()
    {
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

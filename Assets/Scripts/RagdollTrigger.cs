using System;
using UnityEngine;

public class RagdollTrigger : MonoBehaviour
{
    private int numEnters = 0;
    public bool InsideAnything => numEnters > 0;
    
    private void OnTriggerEnter(Collider other)
    {
        numEnters++;
    }
    
    private void OnTriggerExit(Collider other)
    {
        numEnters--;
    }
}

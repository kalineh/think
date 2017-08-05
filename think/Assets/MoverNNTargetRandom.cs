using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNNTargetRandom
    : MonoBehaviour
{
    public void OnEnable()
    {
        transform.localPosition =
            Vector3.Scale(Random.onUnitSphere, new Vector3(1.0f, 0.0f, 1.0f)).normalized * 15.0f;
        transform.localPosition += Vector3.up * 1.0f;
    }
}

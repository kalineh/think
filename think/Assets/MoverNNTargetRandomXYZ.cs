using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNNTargetRandomXYZ
    : MonoBehaviour
{
    public void OnEnable()
    {
        transform.localPosition = Random.onUnitSphere * Random.Range(10.0f, 15.0f);
        transform.localPosition += Vector3.up * 1.0f;
    }
}

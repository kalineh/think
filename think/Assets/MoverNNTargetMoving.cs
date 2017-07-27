using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNNTargetMoving
    : MonoBehaviour
{
    public bool flattenY;

    public void OnEnable()
    {
        transform.localPosition = Random.onUnitSphere * Random.Range(10.0f, 15.0f);
        transform.localPosition += Vector3.up * 1.0f;
    }

    public void FixedUpdate()
    {
        transform.position += new Vector3(
            Mathf.Sin(Time.time * 0.1f) * 0.02f,
            Mathf.Sin(Time.time * 0.1f) * 0.02f,
            Mathf.Sin(Time.time * 0.3f) * 0.03f
        );

        if (flattenY)
            transform.position = new Vector3(transform.position.x, 1.0f, transform.position.z);
    }
}

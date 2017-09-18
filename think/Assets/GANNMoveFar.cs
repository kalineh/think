using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GANNMoveFar
    : GANNBehaviour
{
    private Vector3 start;

    public void OnEnable()
    {
        start = transform.position;
    }

    public void FixedUpdate()
    {
        var inputs = new float[] {
            transform.position.x,
            transform.position.y,
            transform.position.z,
            start.x,
            start.y,
            start.z,
        };

        gann.SafeSetInputs(inputs);

        var outputs = new float[] {
            gann.Pull(0),
            gann.Pull(1),
            gann.Pull(2),
        };

        var body = GetComponent<Rigidbody>();
        var force = new Vector3(outputs[0], outputs[1], outputs[2]);

        body.AddForce(force / Time.fixedDeltaTime, ForceMode.Acceleration);
    }
}

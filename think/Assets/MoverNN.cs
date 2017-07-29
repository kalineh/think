using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNN
    : MonoBehaviour
{
    private Rigidbody body;
    private float energy;
    private float time;

    private Transform target;
    private Vector3 prevPosition;
    private int stationaryFrames;

    // input count N
    // neuron count M
    // output count O

    // input<->neuron weights
    // neuron<->neuron weights
    // neuron<->output weights

    // o  o  o  o
    //  \/ \/ \/
    //  NN NN NN
    //  /\ /\ /\
    //  NN NN NN
    //  /\ /\ /\
    //  NN NN NN
    //  /\ /\ /\
    // o  o  o  o

    public NeuralNetwork nn;

    public bool useTorque;

    public bool debugDrawNN;

    public void OnEnable()
    {
        Init();
    }

    public void Init()
    {
        if (body != null)
            return;

        body = GetComponent<Rigidbody>();

        var layerCount = 4;
        var neuronCount = 6;
        var inputCount = 5;
        var outputCount = 3;

        nn = new NeuralNetwork(layerCount, neuronCount, inputCount, outputCount);
        energy = 0.0f;
        time = 0.0f;
    }

    public void InitFrom(MoverNN rhs)
    {

    }

    public void Reset(Transform _target, Vector3 startPos, Quaternion startRot)
    {
        Init();

        target = _target;

        body.MovePosition(startPos);
        body.MoveRotation(startRot);
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        prevPosition = startPos;
        stationaryFrames = 0;
        energy = 0.0f;
    }

    string[] lastInputLabels;
    float[] lastInputValues;

    void OnDrawGizmosSelected()
    {
        nn.DebugDraw(transform.position, lastInputLabels, lastInputValues);
    }

    public void FixedUpdate()
    {
        if (!body)
            Init();

        if (!target)
            return;

        time += Time.fixedDeltaTime;

        var currPosition = body.position;
        var moved = currPosition - prevPosition;
        if (moved.sqrMagnitude < 0.01f * 0.01f)
            stationaryFrames++;
        else
            stationaryFrames = 0;
        prevPosition = currPosition;

        var rot = body.rotation.eulerAngles;
        var rotVel = body.angularVelocity;
        var vel = body.velocity;

        var mask = LayerMask.GetMask("floor");
        var info = new RaycastHit();
        var hit = Physics.Raycast(transform.position, Vector3.down, out info, 2.0f, mask);
        var dist = info.distance;

        if (hit == false)
            dist = 2.0f;

        var targetOfs = target.transform.position - transform.position;
        var targetDist = targetOfs.sqrMagnitude;

        if (targetDist > 0.001f)
            targetDist = Mathf.Sqrt(targetDist);

        //var input = new float[] { vel.x, vel.y, vel.z, rot.x, rot.y, rot.z, rot.w, rotVel.x, rotVel.y, rotVel.z, dist, energy, time, targetOfs.x, targetOfs.y, targetOfs.z, targetDist };
        //var output = new float[] { 0.0f, 0.0f, 0.0f };
        //lastInputLabels = new string[] { "vel.x", "vel.y", "vel.z", "rot.x", "rot.y", "rot.z", "rot.w", "rotVel.x", "rotVel.y", "rotVel.z", "dist", "energy", "time", "targetOfs.x", "targetOfs.y", "targetOfs.z", "targetDist" };
        //lastInputValues = input;

        //var input = new float[] { rot.x, rot.y, rot.z, rotVel.x, rotVel.y, rotVel.z, energy, targetOfs.x, targetOfs.y, targetOfs.z };
        var input = new float[] { targetOfs.z, targetOfs.y, targetOfs.x, 10000.0f, Random.Range(-1000.0f, 1000.0f) };
        //var input = new float[] { targetOfs.x, targetOfs.y, targetOfs.z, 100.0f };
        var output = new float[] { 0.0f, 0.0f, 0.0f, };

        for (int i = 0; i < input.Length; ++i)
            input[i] = NeuralNetwork.Sigmoid11(input[i]);

        nn.Forward(input, output);

        Debug.DrawLine(transform.position, transform.position + new Vector3(output[0], output[1], output[2]) * 10.0f, Color.red);

        //Debug.DrawLine(transform.position, info.point, Color.red, 3.0f);
        //Debug.LogFormat("energy: {0}, out: {1},{2},{3}", energy, output[0], output[1], output[2]);

        energy += 20.0f * Time.deltaTime;

        var spinX = Mathf.Min(energy, Mathf.Abs(output[0])) * Mathf.Sign(output[0]);
        var spinY = Mathf.Min(energy, Mathf.Abs(output[1])) * Mathf.Sign(output[1]);
        var spinZ = Mathf.Min(energy, Mathf.Abs(output[2])) * Mathf.Sign(output[2]);

        var desiredTotal = Mathf.Abs(spinX) + Mathf.Abs(spinY) + Mathf.Abs(spinZ);
        var desiredTotalRcp = 0.0f;
        if (desiredTotal > 0.0001f)
            desiredTotalRcp = 1.0f / desiredTotal;

        var ratioX = Mathf.Abs(spinX) * desiredTotalRcp;
        var ratioY = Mathf.Abs(spinY) * desiredTotalRcp;
        var ratioZ = Mathf.Abs(spinZ) * desiredTotalRcp;

        energy -= Mathf.Abs(spinX) * ratioX;
        energy -= Mathf.Abs(spinY) * ratioY;
        energy -= Mathf.Abs(spinZ) * ratioZ;

        energy = Mathf.Max(0.0f, energy);

        var dt = Time.fixedDeltaTime;

        var torque = new Vector3(
            (spinX * ratioX) / dt,
            (spinY * ratioY) / dt,
            (spinZ * ratioZ) / dt);

        if (useTorque)
            body.AddTorque(torque * 5.0f, ForceMode.Acceleration);
        else
            body.AddForce(torque * 0.25f, ForceMode.Acceleration);
    }

    public bool IsStopped()
    {
        return stationaryFrames > 120;
    }
}

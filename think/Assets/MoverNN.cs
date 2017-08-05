using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNN
    : MonoBehaviour
{
    private Rigidbody body;
    private float energy;
    private float time;
    private float swim;

    private Transform target;
    private Vector3 startPosition;
    private Vector3 prevPosition;
    private int stationaryFrames;

    public float lastOutputTest;

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
    public bool useForce;
    public bool useSwim;

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

        var layerCount = 7;
        var neuronCount = 6;
        var inputCount = 1;
        var outputCount = 1;

        nn = new NeuralNetwork(layerCount, neuronCount, inputCount, outputCount);
        energy = 0.0f;
        time = 0.0f;
        swim = 1.0f;

        startPosition = transform.position;
        prevPosition = transform.position;
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
        startPosition = startPos;

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
        //var input = new float[] { targetOfs.z, targetOfs.y, targetOfs.x, 10000.0f, Random.Range(-1000.0f, 1000.0f) };
        //var input = new float[] { targetOfs.x, targetOfs.y, targetOfs.z, swim, };
        //var output = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
        var input = new float[] { Random.Range(1.0f, 20.0f), };
        var output = new float[] { 0.0f, };

        for (int i = 0; i < input.Length; ++i)
            input[i] = NeuralNetwork.Sigmoid11(input[i]);

        nn.Forward(input, output);

        lastOutputTest = output[0];

        //Debug.DrawLine(transform.position, transform.position + new Vector3(output[0], output[1], output[2]) * 3.0f, Color.red);

        //Debug.DrawLine(transform.position, info.point, Color.red, 3.0f);
        //Debug.LogFormat("energy: {0}, out: {1},{2},{3}", energy, output[0], output[1], output[2]);

        if (useSwim)
        {
            // requires learning to suppress output3 by swim value (higher swim => output)
            // does it need a bias?

            // output: rotate xyz
            // output: swim trigger
            var swimRotX = output[0];
            var swimRotY = output[1];
            var swimRotZ = output[2];
            var swimPush = output[3] > 0.5f;

            if (swimPush)
            {
                if (swim <= 0.5f)
                {
                    Debug.DrawLine(transform.position, transform.position + new Vector3(swimRotX, swimRotY, swimRotZ) * 5.0f, Color.green);
                    body.AddForce(new Vector3(swimRotX, swimRotY, swimRotZ) * 30.0f, ForceMode.Acceleration);
                    swim = 2.0f;
                }
                else
                {
                    // penalty
                    Debug.DrawLine(transform.position, transform.position + new Vector3(swimRotX, swimRotY, swimRotZ) * 5.0f, Color.black);
                    swim = 5.0f;
                }
            }

            swim -= Time.deltaTime;
            swim = Mathf.Max(swim, 0.0f);

            return;
        }

        energy += 20.0f * Time.deltaTime;

        /*
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
        if (useForce)
            body.AddForce(torque * 0.25f, ForceMode.Acceleration);
            */
    }

    public bool IsStopped()
    {
        return stationaryFrames > 120;
    }

    public bool IsAtStart()
    {
        return (startPosition - transform.position).sqrMagnitude < 0.05f * 0.05f;
    }
}

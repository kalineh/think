using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neuron
{
    public float sum;
    public float[] weights;
}

public class NeuronLayer
{
    public Neuron[] neurons;
}

public class NeuralNetwork
{
    private NeuronLayer[] graph;

    public NeuralNetwork(int layerCount, int neuronCount, int inputCount, int outputCount)
    {
        graph = new NeuronLayer[layerCount];

        for (int i = 0; i < layerCount; ++i)
        {
            graph[i] = new NeuronLayer();
            graph[i].neurons = new Neuron[neuronCount];

            for (int j = 0; j < neuronCount; ++j)
            {
                var weightCount = neuronCount;

                if (i == 0)
                    weightCount = inputCount;

                graph[i].neurons[j] = new Neuron();
                graph[i].neurons[j].weights = new float[weightCount];
                for (int k = 0; k < weightCount; ++k)
                    graph[i].neurons[j].weights[k] = Random.Range(-0.5f, 0.5f);
            }
        }
    }

    public void CopyWeights(NeuralNetwork other)
    {
        for (int i = 0; i < graph.Length; ++i)
        {
            var layer = graph[i];
            for (int j = 0; j < layer.neurons.Length; ++j)
            {
                var neuron = layer.neurons[j];
                for (int k = 0; k < neuron.weights.Length; ++k)
                    neuron.weights[k] = other.graph[i].neurons[j].weights[k];
            }
        }
    }

    public void MutateWeights(float mutation)
    {
        for (int i = 0; i < graph.Length; ++i)
        {
            var layer = graph[i];
            for (int j = 0; j < layer.neurons.Length; ++j)
            {
                var neuron = layer.neurons[j];
                for (int k = 0; k < neuron.weights.Length; ++k)
                {
                    if (Random.Range(0.0f, 1.0f) < mutation)
                        neuron.weights[k] = Random.Range(-0.5f, 0.5f);
                }
            }
        }
    }

    public void Forward(float[] inputs, float[] output)
    {
        var layerFirst = graph[0];
        var layerLast = graph[graph.Length - 1];

        // first layer weights are the left lines to input

        for (int i = 0; i < layerFirst.neurons.Length; ++i)
        {
            var neuron = layerFirst.neurons[i];
            neuron.sum = 0.0f;
            for (int j = 0; j < inputs.Length; ++j)
                neuron.sum += neuron.weights[j] * inputs[j];
        }

        for (int i = 1; i < graph.Length; ++i)
        {
            var layerCurr = graph[i];
            var layerPrev = graph[i - 1];

            for (int j = 0; j < layerCurr.neurons.Length; ++j)
            {
                var neuronCurr = layerCurr.neurons[j];
                neuronCurr.sum = 0.0f;
                for (int k = 0; k < layerPrev.neurons.Length; ++k)
                {
                    var neuronPrev = layerPrev.neurons[k];
                    neuronCurr.sum += neuronCurr.weights[k] * neuronPrev.sum;
                }
            }
        }

        for (int i = 0; i < output.Length; ++i)
            output[i] = 1.0f / (1.0f + Mathf.Exp(layerLast.neurons[i].sum));

    } 
}

public class MoverNN
    : MonoBehaviour
{
    private Rigidbody body;
    private float energy;

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
        var neuronCount = 8;
        var inputCount = 9;
        var outputCount = 3;

        nn = new NeuralNetwork(layerCount, neuronCount, inputCount, outputCount);
        energy = 1.0f;
    }

    public void InitFrom(MoverNN rhs)
    {

    }

    public void Reset(Vector3 startPos, Quaternion startRot)
    {
        Init();

        body.MovePosition(startPos);
        body.MoveRotation(startRot);
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        prevPosition = startPos;
        stationaryFrames = 0;
        energy = 1.0f;
    }

    public void FixedUpdate()
    {
        if (!body)
            Init();

        var currPosition = body.position;
        var moved = currPosition - prevPosition;
        if (moved.sqrMagnitude < 0.01f * 0.01f)
            stationaryFrames++;
        else
            stationaryFrames = 0;
        prevPosition = currPosition;

        var rot = body.rotation;
        var rotVel = body.angularVelocity;
        var mask = LayerMask.GetMask("floor");
        var info = new RaycastHit();
        var hit = Physics.Raycast(transform.position, Vector3.down, out info, 2.0f, mask);
        var dist = info.distance;

        if (hit == false)
            dist = 2.0f;

        var input = new float[] { rot.x, rot.y, rot.z, rot.w, rotVel.x, rotVel.y, rotVel.z, dist, energy };
        var output = new float[] { 0.0f, 0.0f, 0.0f };

        nn.Forward(input, output);
        
        //Debug.DrawLine(transform.position, info.point, Color.red, 3.0f);
        //Debug.LogFormat("energy: {0}, out: {1}", energy, output[0] * 1000.0f);

        energy += 1.0f * Time.deltaTime;

        var available = energy;

        var spinX = Mathf.Min(energy, output[0]);
        energy -= spinX;

        var spinY = Mathf.Min(energy, output[1]);
        energy -= spinY;

        var spinZ = Mathf.Min(energy, output[2]);
        energy -= spinZ;

        energy = Mathf.Clamp01(energy);

        var dt = Time.fixedDeltaTime;

        body.AddForce(spinX / dt, spinY / dt, spinZ / dt, ForceMode.Acceleration);
    }

    public bool IsStopped()
    {
        return stationaryFrames > 60;
    }
}

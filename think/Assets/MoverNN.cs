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
            var layerPrev = graph[i];

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
            output[i] = layerLast.neurons[i].sum;

    } 
}

public class MoverNN
    : MonoBehaviour
{
    private Rigidbody body;

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

    private NeuralNetwork nn;

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
        var inputCount = 2;
        var outputCount = 1;

        nn = new NeuralNetwork(layerCount, neuronCount, inputCount, outputCount);
    }

    public void Reset(Vector3 startPos, Quaternion startRot)
    {
        Init();

        body.MovePosition(startPos);
        body.MoveRotation(startRot);
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    public void Update()
    {
        if (!body)
            Init();

        var rot = body.rotation;
        var rotVel = body.angularVelocity;
        var info = new RaycastHit();

        Physics.Raycast(transform.position, Vector3.down, out info);

        var dist = info.distance;

        var input = new float[] { rotVel.x, dist, };
        var output = new float[] { 0.0f };

        nn.Forward(input, output);
        
        Debug.DrawLine(transform.position, info.point, Color.red, 3.0f);

        body.AddForce(output[0], 0.0f, 0.0f, ForceMode.Acceleration);
    }
}

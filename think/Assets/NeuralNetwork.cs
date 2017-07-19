using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Neuron
{
    public float sum;
    public float[] weights;
}

[System.Serializable]
public class NeuronLayer
{
    public Neuron[] neurons;
}

[System.Serializable]
public class NeuralNetwork
{
    public static float Sigmoid(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }

    public NeuronLayer[] graph;

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
            output[i] = Sigmoid(layerLast.neurons[i].sum);
    } 

    public void DebugDraw(Vector3 pos, string[] lastInputLabels, float[] lastInputValues)
    {
        pos += Vector3.up * 2.0f;
        var layerStep = Vector3.right * 2.0f;
        var neuronStep = Vector3.up * 0.5f;

        if (lastInputLabels != null)
        {
            for (int j = 0; j < graph[0].neurons[0].weights.Length; ++j)
            {
                UnityEditor.Handles.Label(pos + Vector3.right * -5.0f + neuronStep * j + Vector3.up * 0.1f, string.Format("{0}: {1}", j, lastInputLabels[j]));
                Gizmos.color = Color.Lerp(Color.red, Color.green, lastInputValues[j]);
                Gizmos.DrawSphere(pos + layerStep * -1.0f + neuronStep * j, 0.1f);
            }
        }

        for (int i = 0; i < graph.Length; ++i)
        {
            var layer = graph[i];
            var neurons = layer.neurons;

            for (int j = 0; j < neurons.Length; ++j)
            {
                var neuron = neurons[j];
                var weights = neuron.weights;

                var layerOfs = layerStep * i;
                var neuronOfs = neuronStep * j;

                Gizmos.color = Color.Lerp(Color.red, Color.green, neuron.sum);
                Gizmos.DrawSphere(pos + layerOfs + neuronOfs, 0.2f);

                for (int k = 0; k < weights.Length; ++k)
                {
                    var color = Color.Lerp(Color.red, Color.green, weights[k] + 0.5f);

                    var a = pos + layerOfs + neuronOfs;
                    var b = pos + layerOfs - layerStep + neuronStep * k;

                    Gizmos.color = color;
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}


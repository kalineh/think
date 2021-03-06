﻿using System.Collections;
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
    public static float Sigmoid01(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }

    public static float Sigmoid11(float x)
    {
        return (2.0f * 1.0f / (1.0f + Mathf.Exp(-x))) - 1.0f;
    }

    public static float Relu(float x)
    {
        return Mathf.Max(x, 0.0f);
    }

    public static float ReluLeaky(float x)
    {
        return Mathf.Max(x, 0.25f);
    }

    public static float RandomGaussian(float mean, float stddev)
    {
        float u1 = 1.0f - Random.Range(0.0f, 1.0f);
        float u2 = 1.0f - Random.Range(0.0f, 1.0f);

        float stdnorm = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        float result = mean + stddev * stdnorm;

        return result;
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
                    graph[i].neurons[j].weights[k] = InitializeWeight();
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

    public void MutateWeightsAdverse(NeuralNetwork failure, float mutation)
    {
        for (int i = 0; i < graph.Length; ++i)
        {
            var layer = graph[i];
            for (int j = 0; j < layer.neurons.Length; ++j)
            {
                var neuron = layer.neurons[j];
                for (int k = 0; k < neuron.weights.Length; ++k)
                {
                    var failureWeight = failure.graph[i].neurons[j].weights[k];
                    var delta = failureWeight - neuron.weights[k];

                    neuron.weights[k] += -delta * mutation;
                    neuron.weights[k] = Mathf.Clamp(neuron.weights[k], -1.0f, 1.0f);
                }
            }
        }
    }

    public float InitializeWeight()
    {
        //return RandomGaussian(0.0f, 1.0f);
        return Random.Range(-1.0f, 1.0f);
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
                        neuron.weights[k] = MutateWeight(neuron.weights[k]);
                }
            }
        }
    }

    public float MutateWeight(float current)
    {
        var type = Random.Range(0, 4);

        switch (type)
        {
            case 0: return InitializeWeight();
            case 1: return current * -1.0f;
            case 2: return Mathf.Clamp(current * Random.Range(0.0f, 1.0f), -1.0f, 1.0f);
            case 3: return current * Random.Range(0.0f, 1.0f);
        }

        return current;
    }

    public void BlendWeights(List<NeuralNetwork> others, float factor, float amplify)
    {
        for (int i = 0; i < graph.Length; ++i)
        {
            var layer = graph[i];
            for (int j = 0; j < layer.neurons.Length; ++j)
            {
                var neuron = layer.neurons[j];
                for (int k = 0; k < neuron.weights.Length; ++k)
                {
                    if (Random.Range(0.0f, 1.0f) < factor)
                    {
                        var weight = neuron.weights[k];
                        var sum = weight;
                        foreach (var other in others)
                            sum += other.graph[i].neurons[j].weights[k];
                        var avg = sum / ((float)(1 + others.Count));
                        avg *= amplify;
                        avg = Mathf.Clamp(avg, -1.0f, 1.0f);
                        neuron.weights[k] = avg;
                    }
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
            //neuron.sum = Sigmoid11(neuron.sum);
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
                //neuronCurr.sum = Sigmoid11(neuronCurr.sum);
            }
        }

        //for (int i = 0; i < output.Length; ++i)
            //output[i] = layerLast.neurons[i].sum;
        for (int i = 0; i < output.Length; ++i)
            output[i] = Sigmoid11(layerLast.neurons[i].sum);
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
                    var color = Color.Lerp(Color.red, Color.green, weights[k] * 0.5f + 0.5f);

                    var a = pos + layerOfs + neuronOfs;
                    var b = pos + layerOfs - layerStep + neuronStep * k;

                    UnityEditor.Handles.Label((a + b) * 0.5f, string.Format("{0}", weights[k]));

                    Gizmos.color = color;
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}


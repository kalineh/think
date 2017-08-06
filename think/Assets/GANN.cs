using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GANN
{
    public static float Sigmoid01(float x)
    {
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }

    public static float Sigmoid11(float x)
    {
        return (2.0f * 1.0f / (1.0f + Mathf.Exp(-x))) - 1.0f;
    }

    public class Edge
    {
        public Node src;
        public Node dst;
        public float a;
        public float b;
        public float c;

        public float Pull()
        {
            var input = src.Pull();
            var result = Mathf.Pow(Mathf.Abs(input), a) * Mathf.Sign(input) + input * b + c;

            if (float.IsNaN(result))
                Debug.Log("nan");

            return result;
        }
    }

    public class Node
    {
        public float sum;
        public List<Edge> edges;

        public virtual float Pull()
        {
            sum = 0.0f;
            foreach (var edge in edges)
                sum += edge.Pull();
            return sum;
        }
    }

    public class InputNode
        : Node
    {
        public string label;

        public override float Pull()
        {
            return sum;
        }
    }

    public class OutputNode
        : Node
    {
        public string label;
    }

    public List<InputNode> inputs;
    public List<OutputNode> outputs;
    public List<Node> nodes;
    public List<Edge> edges;

    public float Pull(int outputIndex)
    {
        if (outputs.Count < outputIndex)
            return 0.0f;

        var output = outputs[outputIndex];
        var result = output.Pull();

        return result;
    }

    public static GANN BuildTestNetwork()
    {
        var network = new GANN();

        network.nodes = new List<Node>();
        network.edges = new List<Edge>();

        network.inputs = new List<InputNode>();
        for (int i = 0; i < 8; ++i)
        {
            var input = new InputNode();

            network.inputs.Add(input);
            network.nodes.Add(input);
        }

        network.outputs = new List<OutputNode>();
        for (int i = 0; i < 8; ++i)
        {
            var output = new OutputNode();

            network.outputs.Add(output);
            network.nodes.Add(output);
        }

        foreach (var output in network.outputs)
        {
            output.edges = new List<Edge>();

            foreach (var input in network.inputs)
            {
                var edge = new Edge();

                edge.src = input;
                edge.dst = output;
                edge.a = Random.Range(0.0f, 1.0f);
                edge.b = Random.Range(-1.0f, 1.0f);
                edge.c = Random.Range(-1.0f, 1.0f);

                network.edges.Add(edge);

                output.edges.Add(edge);
            }
        }

        network.nodes = new List<Node>();

        for (int i = 0; i < 10; ++i)
        {
            var edge = network.edges[Random.Range(0, network.edges.Count)];

            var newNode = new Node();
            var newEdge = new Edge();

            newEdge.src = edge.src;
            newEdge.dst = newNode;
            newEdge.a = Random.Range(0.0f, 1.0f);
            newEdge.b = Random.Range(-1.0f, 1.0f);
            newEdge.c = Random.Range(-1.0f, 1.0f);

            newNode.edges = new List<Edge>();
            newNode.edges.Add(newEdge);

            edge.src = newNode;

            network.edges.Add(newEdge);
            network.nodes.Add(newNode);
        }
        
        return network;
    }
}


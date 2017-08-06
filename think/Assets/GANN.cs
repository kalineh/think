using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [System.Serializable]
    public class Edge
    {
#if UNITY_EDITOR
        public string label;
#endif
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

    [System.Serializable]
    public class Node
    {
#if UNITY_EDITOR
        public string label;
#endif

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

    [System.Serializable]
    public class InputNode
        : Node
    {
        public override float Pull()
        {
            return sum;
        }
    }

    [System.Serializable]
    public class OutputNode
        : Node
    {
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

    //public float InsertNode()
    //{
    //}

    //public float RemoveNode()
    //{
    //}

#if UNITY_EDITOR

    public class DebugDrawDataNode
    {
        public Vector3 pos;
        public float sum;
    }

    public class DebugDrawDataEdge
    {
        public DebugDrawDataNode a;
        public DebugDrawDataNode b;
    }

    public Dictionary<Node, DebugDrawDataNode> debugDrawDataNodes;
    public Dictionary<Edge, DebugDrawDataEdge> debugDrawDataEdges;

    // we can build a set of nodes
    // and a set of edges
    // and spring connect the edges

    public void DebugDraw(Vector3 at)
    {
        if (debugDrawDataNodes == null)
            debugDrawDataNodes = new Dictionary<Node, DebugDrawDataNode>();
        if (debugDrawDataEdges == null)
            debugDrawDataEdges = new Dictionary<Edge, DebugDrawDataEdge>();

        foreach (var node in nodes)
        {
            if (!debugDrawDataNodes.ContainsKey(node))
                debugDrawDataNodes.Add(node, new DebugDrawDataNode() { pos = Vector3.zero, sum = 0.0f });
            debugDrawDataNodes[node].sum = node.sum;
        }

        foreach (var edge in edges)
        {
            if (!debugDrawDataEdges.ContainsKey(edge))
            {
                var an = (Node)edge.src;
                var bn = (Node)edge.dst;
                var da = debugDrawDataNodes[an];
                var db = debugDrawDataNodes[bn];

                debugDrawDataEdges.Add(edge, new DebugDrawDataEdge() { a = da, b = db, });
            }
        }

        foreach (var kv in debugDrawDataEdges)
        {
            var pa = kv.Value.a;
            var pb = kv.Value.b;

            var ofs = pb.pos - pa.pos;

            if (ofs.sqrMagnitude < 0.00001f)
            {
                pa.pos += Random.onUnitSphere * 0.0001f;
                pb.pos += Random.onUnitSphere * 0.0001f;
                continue;
            }

            var k = 0.01f;
            var d = 3.0f;
            var push = -k * (d - ofs.magnitude);

            var dir = ofs.normalized;

            pa.pos += dir * push;
            pb.pos -= dir * push;
        }

        foreach (var kv in debugDrawDataNodes)
        {
            var p = kv.Value.pos;
            var v = kv.Value.sum;

            var isInput = (kv.Key as InputNode) != null;
            var isOutput = (kv.Key as OutputNode) != null;

            Handles.color = Color.white;

            var color = Color.white;

            if (isInput)
                color = Color.blue;
            if (isOutput)
                color = Color.red;

            var brightness = Sigmoid01(v);

            Handles.color = Color.Lerp(Color.black, color, brightness);
            Handles.SphereHandleCap(0, p, Quaternion.identity, 0.25f, EventType.Repaint);

            var po = debugDrawDataNodes[nodes[Random.Range(0, nodes.Count)]];
            var poofs = p - po.pos;

            if (poofs.sqrMagnitude > 0.0001f)
                kv.Value.pos += poofs / poofs.sqrMagnitude * 0.001f;
        }

        foreach (var kv in debugDrawDataEdges)
        {
            var pa = kv.Value.a.pos;
            var pb = kv.Value.b.pos;

            var brightness = Sigmoid01(kv.Value.a.sum);

            Handles.color = Color.Lerp(Color.black, Color.white, brightness);
            Handles.DrawAAPolyLine(2.0f, pa, pb);
        }

        return;

    }
#endif

    public static GANN BuildTestNetwork()
    {
        var network = new GANN();

        network.nodes = new List<Node>();
        network.edges = new List<Edge>();

        network.inputs = new List<InputNode>();
        for (int i = 0; i < 8; ++i)
        {
            var input = new InputNode();

#if UNITY_EDITOR
            input.label = string.Format("in:{0}", i);
#endif
            network.inputs.Add(input);
            network.nodes.Add(input);
        }

        network.outputs = new List<OutputNode>();
        for (int i = 0; i < 8; ++i)
        {
            var output = new OutputNode();

#if UNITY_EDITOR
            output.label = string.Format("out:{0}", i);
#endif

            network.outputs.Add(output);
            network.nodes.Add(output);
        }

        foreach (var output in network.outputs)
        {
            output.edges = new List<Edge>();

            foreach (var input in network.inputs)
            {
                var edge = new Edge();

#if UNITY_EDITOR
                edge.label = string.Format("edge:{0}-{1}", input.label, output.label);
#endif

                edge.src = input;
                edge.dst = output;
                edge.a = Random.Range(0.0f, 1.0f);
                edge.b = Random.Range(-1.0f, 1.0f);
                edge.c = Random.Range(-1.0f, 1.0f);

                network.edges.Add(edge);

                output.edges.Add(edge);
            }
        }

        for (int i = 0; i < 10; ++i)
        {
            var edge = network.edges[Random.Range(0, network.edges.Count)];

            var newNode = new Node();
            var newEdge = new Edge();

#if UNITY_EDITOR
            newNode.label = string.Format("node:{0}", i);
#endif

            newEdge.src = edge.src;
            newEdge.dst = newNode;
            newEdge.a = Random.Range(0.0f, 1.0f);
            newEdge.b = Random.Range(-1.0f, 1.0f);
            newEdge.c = Random.Range(-1.0f, 1.0f);

#if UNITY_EDITOR
            newEdge.label = string.Format("edge:{0}-{1}", edge.src.label, newNode.label);
#endif

            newNode.edges = new List<Edge>();
            newNode.edges.Add(newEdge);

            edge.src = newNode;

#if UNITY_EDITOR
            edge.label = string.Format("edge:{0}-{1}", edge.src.label, edge.dst.label);
#endif

            network.edges.Add(newEdge);
            network.nodes.Add(newNode);
        }
        
        return network;
    }
}


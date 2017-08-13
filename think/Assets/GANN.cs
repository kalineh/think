using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO:
// node insert
// node remove
// edge mutate

[System.Serializable]
public class GANN
    : ISerializationCallbackReceiver
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

    public class InputNode
        : Node
    {
        public override float Pull()
        {
            return sum;
        }
    }

    public class OutputNode
        : Node
    {
    }

    public List<InputNode> inputs;
    public List<OutputNode> outputs;
    public List<Node> nodes;
    public List<Edge> edges;

    [System.Serializable]
    private class SerializeNode
    {
#if UNITY_EDITOR
        public string label;
#endif
        public int type;
        public float sum;
        public List<int> edgeIndices;
    }

    [System.Serializable]
    private class SerializeEdge
    {
#if UNITY_EDITOR
        public string label;
#endif
        public int srcIndex;
        public int dstIndex;
        public float a;
        public float b;
        public float c;
    }

    [SerializeField]
    private List<SerializeNode> serializeNodes;

    [SerializeField]
    private List<SerializeEdge> serializeEdges;

    public void OnBeforeSerialize()
    {
        // not created
        if (nodes == null)
            return;

        var nodeIndexLookup = new Dictionary<Node, int>();
        var edgeIndexLookup = new Dictionary<Edge, int>();

        for (int i = 0; i < nodes.Count; ++i)
            nodeIndexLookup.Add(nodes[i], i);
        for (int i = 0; i < edges.Count; ++i)
            edgeIndexLookup.Add(edges[i], i);

        serializeNodes = new List<SerializeNode>();

        foreach (var node in nodes)
        {
            var serializeNode = new SerializeNode();

#if UNITY_EDITOR
            serializeNode.label = node.label;
#endif

            serializeNode.type = 0;
            if ((node as InputNode) != null) serializeNode.type = 1;
            if ((node as OutputNode) != null) serializeNode.type = 2;

            serializeNode.sum = node.sum;

            serializeNode.edgeIndices = new List<int>();
            foreach (var edge in node.edges)
            {
                var index = edgeIndexLookup[edge];
                serializeNode.edgeIndices.Add(index);
            }

            serializeNodes.Add(serializeNode);
        }

        serializeEdges = new List<SerializeEdge>();

        foreach (var edge in edges)
        {
            var serializeEdge = new SerializeEdge();

#if UNITY_EDITOR
            serializeEdge.label = edge.label;
#endif
            serializeEdge.srcIndex = nodeIndexLookup[edge.src];
            serializeEdge.dstIndex = nodeIndexLookup[edge.dst];
            serializeEdge.a = edge.a;
            serializeEdge.b = edge.b;
            serializeEdge.c = edge.c;

            serializeEdges.Add(serializeEdge);
        }
    }

    public void OnAfterDeserialize()
    {
        // not created
        if (serializeNodes == null)
            return;

        nodes = new List<Node>();

        foreach (var serializeNode in serializeNodes)
        {
            var node = (Node)null;

            if (serializeNode.type == 0) node = new Node();
            if (serializeNode.type == 1) node = new InputNode();
            if (serializeNode.type == 2) node = new OutputNode();

#if UNITY_EDITOR
            node.label = serializeNode.label;
#endif

            node.sum = serializeNode.sum;

            // write edge references after edge list created

            nodes.Add(node);
        }

        edges = new List<Edge>();

        foreach (var serializeEdge in serializeEdges)
        {
            var edge = new Edge();

#if UNITY_EDITOR
            edge.label = serializeEdge.label;
#endif

            edge.src = nodes[serializeEdge.srcIndex];
            edge.dst = nodes[serializeEdge.dstIndex];
            edge.a = serializeEdge.a;
            edge.b = serializeEdge.b;
            edge.c = serializeEdge.c;

            edges.Add(edge);
        }

        for (int i = 0; i < serializeNodes.Count; ++i)
        {
            var serializeNode = serializeNodes[i];
            var node = nodes[i];

            node.edges = new List<Edge>();

            foreach (var edgeIndex in serializeNode.edgeIndices)
            {
                var edge = edges[edgeIndex];

                node.edges.Add(edge);
            }
        }
    }


    public float Pull(int outputIndex)
    {
        if (outputs.Count < outputIndex)
            return 0.0f;

        var output = outputs[outputIndex];
        var result = output.Pull();

        return result;
    }

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

    public void DebugDrawInvalidate()
    {
        debugDrawDataNodes = null;
        debugDrawDataEdges = null;
    }

    public void DebugDraw(Vector3 at)
    {
        if (nodes == null)
            return;

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

            Handles.color = Color.white;
            Handles.Label(p, kv.Key.label);

            var physicsMoveNode = nodes[Random.Range(0, nodes.Count)];
            var physicsMoveNodeData = debugDrawDataNodes[physicsMoveNode];
            var physicsMoveNodeOfs = p - physicsMoveNodeData.pos;
            if (physicsMoveNodeOfs.sqrMagnitude > 0.0001f)
            {
                kv.Value.pos += physicsMoveNodeOfs / physicsMoveNodeOfs.sqrMagnitude * 0.001f;
                if ((kv.Key as InputNode) != null && (physicsMoveNode as InputNode) != null)
                    kv.Value.pos += physicsMoveNodeOfs / physicsMoveNodeOfs.sqrMagnitude * -0.0005f;
                if ((kv.Key as OutputNode) != null && (physicsMoveNode as OutputNode) != null)
                    kv.Value.pos += physicsMoveNodeOfs / physicsMoveNodeOfs.sqrMagnitude * -0.0005f;
            }
        }

        foreach (var kv in debugDrawDataEdges)
        {
            var pa = kv.Value.a.pos;
            var pb = kv.Value.b.pos;

            var brightness = Sigmoid01(kv.Value.a.sum);

            Handles.color = Color.Lerp(Color.black, Color.white, brightness);
            Handles.DrawAAPolyLine(2.0f, pa, pb);

            Handles.color = Color.white;
            Handles.Label((pa + pb) * 0.5f, kv.Key.label);
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
        for (int i = 0; i < 1; ++i)
        {
            var input = new InputNode();

#if UNITY_EDITOR
            input.label = string.Format("in:{0}", i);
#endif

            input.edges = new List<Edge>();            

            network.inputs.Add(input);
            network.nodes.Add(input);
        }

        network.outputs = new List<OutputNode>();
        for (int i = 0; i < 1; ++i)
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
                edge.label = string.Format("{0}-{1}", input.label, output.label);
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

        for (int i = 0; i < 2; ++i)
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
            newEdge.label = string.Format("{0}-{1}", edge.src.label, newNode.label);
#endif

            newNode.edges = new List<Edge>();
            newNode.edges.Add(newEdge);

            edge.src = newNode;

#if UNITY_EDITOR
            edge.label = string.Format("{0}-{1}", edge.src.label, edge.dst.label);
#endif

            network.edges.Add(newEdge);
            network.nodes.Add(newNode);
        }
        
        return network;
    }

    public static void InsertNode(GANN gann)
    {
        var edge = gann.edges[Random.Range(0, gann.edges.Count)];

        // A <-B <-C
        // A <-N <-B <-C

        var newNode = new Node();
        var newEdge = new Edge();

#if UNITY_EDITOR
        newNode.label = string.Format("inserted");
#endif

        newEdge.src = edge.src;
        newEdge.dst = newNode;
        newEdge.a = Random.Range(0.0f, 1.0f);
        newEdge.b = Random.Range(-1.0f, 1.0f);
        newEdge.c = Random.Range(-1.0f, 1.0f);

#if UNITY_EDITOR
        newEdge.label = string.Format("{0}-{1}", edge.src.label, newNode.label);
#endif

        newNode.edges = new List<Edge>();
        newNode.edges.Add(newEdge);

        edge.dst.edges.Remove(edge);

        edge.src = newNode;

#if UNITY_EDITOR
        edge.label = string.Format("{0}-{1}", edge.src.label, edge.dst.label);
#endif

        gann.edges.Add(newEdge);
        gann.nodes.Add(newNode);

        gann.DebugDrawInvalidate();
    }
}


﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GANNBehaviour))]
public class GANNBehaviourEditor
    : Editor
{
    private int inputs = 1;
    private int outputs = 1;
    private int nodes = 1;

    public override void OnInspectorGUI()
    {
        var self = target as GANNBehaviour;

        inputs = EditorGUILayout.IntSlider("Inputs", inputs, 1, 8);
        outputs = EditorGUILayout.IntSlider("Outputs", outputs, 1, 8);
        nodes = EditorGUILayout.IntSlider("Nodes", nodes, 0, 8);

        if (GUILayout.Button("Clear"))
            self.Clear();
        if (GUILayout.Button("Rebuild"))
            self.Rebuild(inputs, outputs, nodes);
        if (GUILayout.Button("Pull"))
            self.Pull();
        if (GUILayout.Button("Insert Node"))
            self.InsertNode();
        if (GUILayout.Button("Remove Node"))
            self.RemoveNode();

        EditorUtility.SetDirty(target);

        base.OnInspectorGUI();
    }
}
#endif

public class GANNBehaviour
    : MonoBehaviour
{
    public GANN gann;

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (gann != null && Selection.Contains(gameObject))
            gann.DebugDraw(transform.position);
    }
#endif

    public virtual bool IsActive()
    {
        return true;
    }

    public virtual void Tick()
    {
    }

    public void Clear()
    {
        gann = null;
    }

    public void Rebuild(int inputs, int outputs, int nodes)
    {
        gann = GANN.BuildNetwork(inputs, outputs, nodes);
    }

    public void Duplicate(GANNBehaviour src)
    {
        gann = GANN.DuplicateNetwork(src.gann);
    }

    public void Mutate()
    {
        GANN.InsertNode(gann);
        GANN.RemoveNode(gann);
    }

    public void Pull()
    {
        for (int i = 0; i < gann.outputs.Count; ++i)
        {
            var raw = gann.Pull(i);
            var sig = GANN.Sigmoid11(raw);

            Debug.LogFormat("out: {0}: {1} (raw: {2})", i, sig, raw);
        }
    }

    public void InsertNode()
    {
        GANN.InsertNode(gann);
    }

    public void RemoveNode()
    {
        GANN.RemoveNode(gann);
    }
}

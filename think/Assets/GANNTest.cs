﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GANNTest))]
public class GANNTestEditor
    : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var self = target as GANNTest;

        if (GUILayout.Button("Rebuild"))
            self.Rebuild();
        if (GUILayout.Button("Pull"))
            self.Pull();
    }
}
#endif

public class GANNTest
    : MonoBehaviour
{
    public GANN gann;

    public void Rebuild()
    {
        gann = GANN.BuildTestNetwork();
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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GANNBehaviour))]
public class GANNBehaviourEditor
    : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var self = target as GANNBehaviour;

        if (GUILayout.Button("Clear"))
            self.Clear();
        if (GUILayout.Button("Rebuild"))
            self.Rebuild();
        if (GUILayout.Button("Pull"))
            self.Pull();
        if (GUILayout.Button("Insert Node"))
            self.InsertNode();
        if (GUILayout.Button("Remove Node"))
            self.RemoveNode();

        EditorUtility.SetDirty(target);
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

    public void Clear()
    {
        gann = null;
    }

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

    public void InsertNode()
    {
        GANN.InsertNode(gann);
    }

    public void RemoveNode()
    {
    }
}

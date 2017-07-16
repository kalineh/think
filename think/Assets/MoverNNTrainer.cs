using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNNTrainer
    : MonoBehaviour
{
    public MoverNN instance;

    public void OnEnable()
    {
        StartCoroutine(DoCycles());
    }

    public IEnumerator DoCycles()
    {
        Debug.LogFormat("MoverNNTrainer: training starting for '{0}'", instance.name);

        var startPos = Vector3.zero;
        var startRot = Quaternion.identity;

        while (true)
        {
            instance.Reset(startPos, startRot);

            yield return new WaitForSeconds(5.0f);

            var ofs = instance.transform.position - startPos;
            var lsq = ofs.sqrMagnitude;
            var len = lsq > 0.001f ? ofs.magnitude : 0.0f;
        }
    }
}

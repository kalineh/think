using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverNNTrainer
    : MonoBehaviour
{
    public GameObject pfb;
    
    private List<MoverNN> instances;

    public bool trainingMode;
    public bool trainingPause;
    public bool trainingStep;

    public bool trainingTimeScale;
    public float trainingTimeScaleValue = 1.0f;

    public Transform target;

    public void OnEnable()
    {
        StartCoroutine(DoCycles());
    }

    public IEnumerator DoCycles()
    {
        Debug.LogFormat("MoverNNTrainer: training starting...");

        var startPos = Vector3.zero;
        var startRot = Quaternion.identity;

        var generation = 0;
        var count = 64;

        instances = new List<MoverNN>();
        for (int i = 0; i < count; ++i)
        {
            instances.Add(GameObject.Instantiate(pfb).GetComponent<MoverNN>());
            instances[i].name = string.Format("Mover{0}", i);
        }

        while (true)
        {
            foreach (var instance in instances)
                instance.Reset(target, startPos, startRot);

            var timer = 0.0f;
            var dt = Time.fixedDeltaTime;
            var skip = false;

            if (trainingTimeScale)
                Time.timeScale = trainingTimeScaleValue;
            else
                Time.timeScale = 1.0f;

            if (trainingMode)
            {
                Physics.autoSimulation = false;
                int previewStep = 100;
                int previewIndex = 0;

                while (timer < 60.0f)
                {
                    var pause = trainingPause;

                    if (trainingStep)
                    {
                        pause = false;
                        trainingPause = true;
                        trainingStep = false;
                    }

                    if (pause == false)
                    {
                        Physics.Simulate(dt);
                        timer = Tick(timer, dt, out skip);
                        if (skip)
                            break;
                    }

                    previewIndex += 1;
                    if (previewIndex % previewStep == 0 || pause)
                        yield return null;
                }
                yield return null;
            }
            else
            {
                Physics.autoSimulation = true;
                while (timer < 60.0f)
                {
                    timer = Tick(timer, Time.fixedDeltaTime, out skip);
                    if (skip)
                        break;
                    yield return new WaitForFixedUpdate();
                }
            }

            var best = (MoverNN)null;
            var bestScore = 9999.0f;

            foreach (var instance in instances)
            {
                var ofs = instance.transform.position - target.transform.position;
                var lsq = ofs.sqrMagnitude;
                var len = lsq > 0.001f ? ofs.magnitude : 0.0f;

                if (instance.transform.position.y < -1.0f)
                    len = 999999.0f;

                if (len < bestScore)
                {
                    best = instance;
                    bestScore = len;
                }
            }

            if (best != null)
            {
                foreach (var instance in instances)
                    instance.nn.CopyWeights(best.nn);

                foreach (var instance in instances)
                {
                    if (instance != best)
                    {
                        var ofs = instance.transform.position - target.transform.position;
                        var lsq = ofs.sqrMagnitude;
                        var len = lsq > 0.001f ? ofs.magnitude : 0.0f;
    
                        instance.nn.MutateWeights(0.2f);
                    }
                }

                Debug.LogFormat("MoverNNTrainer: generation {0} winner '{1}': score {2} (time: {3})", generation, best.name, bestScore, timer);
            }

            generation++;
        }
    }

    public float Tick(float timer, float dt, out bool earlyOut)
    {
        timer += dt;

        earlyOut = false;

        var moving = false;
        foreach (var instance in instances)
        {
            if (instance.IsStopped() == false)
            {
                moving = true;
                break;
            }
        }

        if (!moving)
            earlyOut = true;

        return timer;
    }
}

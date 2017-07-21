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
    public bool trainingSkip;

    public bool trainingTimeScale;
    public float trainingTimeScaleValue = 1.0f;
    public float trainingTimeLimit = 30.0f;

    public int trainingCount = 64;

    public GameObject pfbTarget;
    public GameObject target;

    public void OnEnable()
    {
        StartCoroutine(DoCycles());
    }

    public IEnumerator DoCycles()
    {
        Debug.LogFormat("MoverNNTrainer: training starting...");

        var generation = 0;
        var count = trainingCount;

        var previousBestScore = 9999.0f;

        instances = new List<MoverNN>();
        for (int i = 0; i < count; ++i)
        {
            instances.Add(GameObject.Instantiate(pfb).GetComponent<MoverNN>());
            instances[i].name = string.Format("Mover{0}", i);
        }

        while (true)
        {
            if (target != null)
                Destroy(target);

            target = GameObject.Instantiate(pfbTarget);

            foreach (var instance in instances)
                instance.Reset(target.transform, transform.position, transform.rotation);

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

                while (timer < trainingTimeLimit)
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
                while (timer < trainingTimeLimit)
                {
                    timer = Tick(timer, Time.fixedDeltaTime, out skip);
                    if (skip)
                        break;
                    if (trainingSkip)
                    {
                        trainingSkip = false;
                        break;
                    }
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
                var score = len * 1.0f;

                if (instance.transform.position.y < -1.0f)
                    score = 999999.0f;

                if (score < bestScore)
                {
                    best = instance;
                    bestScore = len;
                }
            }

            previousBestScore += 2.0f;
            if (bestScore > previousBestScore * 1.25f)
            {
                Debug.LogFormat("MoverNNTrainer: generation {0} score {1} didn't beat score: {2}", generation, bestScore, previousBestScore);
                best = null;
            }
            else
                previousBestScore = bestScore;

            if (best != null)
            {
                foreach (var instance in instances)
                    instance.nn.CopyWeights(best.nn);

                foreach (var instance in instances)
                {
                    var mutation = instance == best ? 0.0f : 0.01f;

                    instance.nn.MutateWeights(mutation);
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

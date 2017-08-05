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
        var previousWorstScore = 9999.0f;

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

            var scoreTestingFixed = true;
            var mutateSingle = false;
            var mutateMultiple = false;
            var mutateAdverse = false;

            if (scoreTestingFixed)
            {
                var best = (MoverNN)null;
                var bestScore = 9999.0f;

                foreach (var instance in instances)
                {
                    var result = instance.lastOutputTest * 50.0f;
                    var delta = Mathf.Abs(-5.0f - result);
                    if (delta < bestScore)
                    {
                        best = instance;
                        bestScore = delta;
                    }
                }

                if (best == null)
                    previousBestScore += 1.0f;

                if (bestScore > previousBestScore * 1.25f)
                {
                    Debug.LogFormat("MoverNNTrainer: generation {0} score {1} didn't beat score: {2}", generation, bestScore, previousBestScore);
                    best = null;
                }
                else
                    previousBestScore = bestScore;

                if (best != null)
                {
                    Debug.LogFormat("MoverNNTrainer: generation {0} winner '{1}': score {2} (time: {3}, output: {4})", generation, best ? best.name : "no best", bestScore, timer, best.lastOutputTest * 50.0f);

                    foreach (var instance in instances)
                        instance.nn.CopyWeights(best.nn);
                }

                foreach (var instance in instances)
                {
                    var mutation = instance == best ? 0.0f : 0.05f;

                    instance.nn.MutateWeights(mutation);
                }
            }

            if (mutateSingle)
            {
                var best = (MoverNN)null;
                var bestScore = 9999.0f;

                foreach (var instance in instances)
                {
                    // invalid if didnt move
                    if (instance.IsAtStart())
                        continue;

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

                if (best == null)
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
                }

                foreach (var instance in instances)
                {
                    var mutation = instance == best ? 0.0f : 0.25f;

                    instance.nn.MutateWeights(mutation);
                }

                Debug.LogFormat("MoverNNTrainer: generation {0} winner '{1}': score {2} (time: {3})", generation, best ? best.name : "no best", bestScore, timer);
            }

            if (mutateAdverse)
            {
                var worst = (MoverNN)null;
                var worstScore = -9999.0f;

                foreach (var instance in instances)
                {
                    var ofs = instance.transform.position - target.transform.position;
                    var lsq = ofs.sqrMagnitude;
                    var len = lsq > 0.001f ? ofs.magnitude : 0.0f;
                    var score = len * 1.0f;

                    //if (instance.transform.position.y < -1.0f)
                        //score = 999999.0f;

                    if (score > worstScore)
                    {
                        worst = instance;
                        worstScore = len;
                    }
                }

                previousWorstScore += 2.0f;
                if (worstScore > previousWorstScore * 1.25f)
                {
                    Debug.LogFormat("MoverNNTrainer: adverse generation {0} score {1} didn't beat score: {2}", generation, worstScore, previousWorstScore);
                    worst = null;
                }
                else
                    previousWorstScore = worstScore;

                if (worst != null)
                {
                    foreach (var instance in instances)
                        instance.nn.MutateWeightsAdverse(worst.nn, 0.1f);

                    Debug.LogFormat("MoverNNTrainer: adverse generation {0} winner '{1}': score {2} (time: {3})", generation, worst.name, worstScore, timer);
                }
            }

            if (mutateMultiple)
            {
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

                previousBestScore += 1.0f;
                if (bestScore > previousBestScore * 1.25f)
                {
                    Debug.LogFormat("MoverNNTrainer: generation {0} score {1} didn't beat score: {2}", generation, bestScore, previousBestScore);
                    best = null;
                }
                else
                    previousBestScore = bestScore;

                if (best != null)
                {
                    var bestN = new List<MoverNN>(instances);
                    
                    bestN.Sort((a, b) => {
                        var ofsA = a.transform.position - target.transform.position;
                        var lsqA = ofsA.sqrMagnitude;
                        var lenA = lsqA > 0.001f ? ofsA.magnitude : 0.0f;
                        var scoreA = lenA * 1.0f;

                        var ofsB = b.transform.position - target.transform.position;
                        var lsqB = ofsB.sqrMagnitude;
                        var lenB = lsqB > 0.001f ? ofsB.magnitude : 0.0f;
                        var scoreB = lenB * 1.0f;

                        if (scoreA < scoreB)
                            return -1;
                        if (scoreA > scoreB)
                            return 1;

                        return 0;
                    });

                    var blend = new List<NeuralNetwork>();

                    blend.Add(bestN[1].nn);
                    blend.Add(bestN[2].nn);
                    blend.Add(bestN[3].nn);

                    //bestN[0].nn.BlendWeights(blend, 0.5f, 1.5f);

                    foreach (var instance in instances)
                        instance.nn.CopyWeights(bestN[0].nn);

                    for (int i = 1; i < bestN.Count; ++i)
                        bestN[i].nn.MutateWeights(0.01f);

                    //if (generation % 10 == 0)
                        //for (int i = 1; i < bestN.Count; ++i)
                            //bestN[i].nn.MutateWeights(0.25f);

                    Debug.LogFormat("MoverNNTrainer: generation {0} winner '{1}': (time: {2})", generation, bestN[0].name, timer);
                    Debug.LogFormat("MoverNNTrainer: > blend with '{0}', '{1}', '{2}'", bestN[1].name, bestN[2].name, bestN[3].name);
                }
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

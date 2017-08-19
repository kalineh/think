using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// mutation strategies
// train in different tasks
// run in parallel

public class GANNTrainer
    : MonoBehaviour
{
    public GameObject prefab;

    public int parallelCount = 16;

    public bool trainingPause = false;
    public bool trainingStep = false;
    public bool trainingTimeScale = true;
    public float trainingTimeScaleValue = 1.0f;
    public float trainingRunTime = 30.0f;

    public class TrainingRun
    {
        public GameObject holder;
        public List<GameObject> objects;
        public List<float> scores;
    }

    public List<TrainingRun> runs;

    public void OnEnable()
    {
        StartCoroutine(DoTraining());
    }

    public IEnumerator DoTraining()
    {
        Debug.LogFormat("GANNTrainer: training starting...");

        var count = parallelCount;

        runs = new List<TrainingRun>();

        while (true)
        {
            var run = new TrainingRun();

            run.holder = new GameObject(string.Format("Run{0}", runs.Count));
            run.objects = new List<GameObject>();
            run.scores = new List<float>();

            for (int i = 0; i < parallelCount; ++i)
            {
                var obj = GameObject.Instantiate(prefab);
                var gann = obj.GetComponent<GANNBehaviour>(); 

                obj.transform.SetParent(run.holder.transform);
                obj.name = string.Format("GANN{0}", i);
            }

            var remaining = trainingRunTime;
            while (remaining > 0.0f)
            {
                var timer = 0.0f;
                var dt = Time.fixedDeltaTime;
                var skip = false;

                if (trainingTimeScale)
                    Time.timeScale = trainingTimeScaleValue;
                else
                    Time.timeScale = 1.0f;

                if (trainingStep)
                {
                    trainingPause = true;
                    trainingStep = false;
                }

                Physics.autoSimulation = false;
                Physics.Simulate(dt);
                timer = Tick(run, timer, dt, out skip);
                if (skip)
                    break;

                remaining -= dt * Time.timeScale;
                yield return null;
            }

            run.holder.SetActive(false);

            runs.Add(run);

            yield return null;
        }
    }


    public float Tick(TrainingRun run, float timer, float dt, out bool earlyOut)
    {
        timer += dt;

        earlyOut = false;

        return timer;

        var moving = false;
        foreach (var obj in run.objects)
        {
            var gann = obj.GetComponent<GANNBehaviour>();
            //if (gann.IsStopped() == false)
            //{
                //moving = true;
                //break;
            //}
        }

        if (!moving)
            earlyOut = true;

        return timer;
    }
}

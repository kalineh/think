using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nntest
    : MonoBehaviour
{
    public float fwdMul(float a, float b) { return a * b; }
    public float fwdAdd(float a, float b) { return a + b; }
    public float fwdCircuit(float x, float y, float z) { return fwdMul(fwdAdd(x, y), z); }
    public float sigmoid(float x) { return 1.0f / (1.0f + Mathf.Exp(-x)); }

    public class Unit
    {
        public float value;
        public float gradient;

        public Unit()
        {
        }

        public Unit(float v, float g)
        {
            value = v;
            gradient = g;
        }

    }
    
    public struct GateMul
    {
        public Unit u0;
        public Unit u1;
        public Unit utop;

        public Unit forward(Unit _u0, Unit _u1)
        {
            u0 = _u0;
            u1 = _u1;
            utop = new Unit();
            utop.value = u0.value * u1.value;
            utop.gradient = 0.0f;
            return utop;
        }

        public void backward()
        {
            u0.gradient += u1.value * utop.gradient;
            u1.gradient += u0.value * utop.gradient;
        }
    }

    public struct GateAdd
    {
        public Unit u0;
        public Unit u1;
        public Unit utop;

        public Unit forward(Unit _u0, Unit _u1)
        {
            u0 = _u0;
            u1 = _u1;
            utop = new Unit();
            utop.value = u0.value + u1.value;
            utop.gradient = 0.0f;
            return utop;
        }

        public void backward()
        {
            u0.gradient += 1.0f * utop.gradient;
            u1.gradient += 1.0f * utop.gradient;
        }
    }

    public struct GateSigmoid
    {
        public Unit u0;
        public Unit utop;

        private float sigmoid(float x) { return 1.0f / (1.0f + Mathf.Exp(-x)); }

        public Unit forward(Unit _u0)
        {
            u0 = _u0;
            utop = new Unit();
            utop.value = sigmoid(u0.value);
            utop.gradient = 0.0f;
            return utop;
        }

        public void backward()
        {
            var s = sigmoid(u0.value);
            u0.gradient += (s * (1.0f - s)) * utop.gradient;
        }
    }

    public void OnEnable()
    {
        Test5();
    }

    public void Test0()
    {
        var x = -2.0f;
        var y = 5.0f;
        var z = -4.0f;

        //var f = fwdCircuit(x, y, z);

        var q = fwdAdd(x, y);
        var f = fwdMul(q, z);

        var dfz = q;
        var dfq = z;
        var dqx = 1.0f;
        var dqy = 1.0f;

        var dfx = dqx * dfq;
        var dfy = dqy * dfq;

        var step = 0.01f;

        var xx = x + step * dfx;
        var yy = y + step * dfy;
        var zz = z + step * dfz;

        var qq = fwdAdd(xx, yy);
        var ff = fwdMul(qq, zz);

        Debug.LogFormat("Output: {0} -> {1}", f, ff);
    }

    public void Test1()
    {
        var a = new Unit(1.0f, 0.0f);
        var b = new Unit(2.0f, 0.0f);
        var c = new Unit(-3.0f, 0.0f);
        var x = new Unit(-1.0f, 0.0f);
        var y = new Unit(3.0f, 0.0f);

        var mulg0 = new GateMul();
        var mulg1 = new GateMul();
        var addg0 = new GateAdd();
        var addg1 = new GateAdd();
        var sg0 = new GateSigmoid();

        var ax = mulg0.forward(a, x);
        var by = mulg1.forward(b, y);
        var axpby = addg0.forward(ax, by);
        var axpbypc = addg1.forward(axpby, c);
        var s = sg0.forward(axpbypc);

        Debug.LogFormat("forward: pass 1: {0}", s.value);

        s.gradient = 1.0f;
        sg0.backward();
        addg1.backward();
        addg0.backward();
        mulg1.backward();
        mulg0.backward();

        var step = 0.01f;

        a.value += step * a.gradient;
        b.value += step * b.gradient;
        c.value += step * c.gradient;
        x.value += step * x.gradient;
        y.value += step * y.gradient;

        ax = mulg0.forward(a, x);
        by = mulg1.forward(b, y);
        axpby = addg0.forward(ax, by);
        axpbypc = addg1.forward(axpby, c);
        s = sg0.forward(axpbypc);

        Debug.LogFormat("forward: pass 2: {0}", s.value);
    }

    public void Test2()
    {
        // mul
        //var x = a * b;
        //var da = b * dx;
        //var db = a * dx;

        // add
        //var x = a + b;
        //var da = 1.0f * dx;
        //var db = 1.0f * dx;

        // x = a + b + c
        //var q = a + b; // gate 1
        //var x = q + c; // gate 2
        //var dc = 1.0f * dx; // backprop gate 2
        //var dq = 1.0f * dx;
        //var da = 1.0f * dq; // backprop gate 1
        //var db = 1.0f * dq; 

        // x = a * b + c
        //var q = a * b; // gate 1
        //var x = q + c; // gate 2
        //var da = b * dx;
        //var db = a * dx;
        //var dc = 1.0f * dx;

        var a = 1.0f;
        var b = 2.0f;
        var c = -3.0f;
        var x = -1.0f;
        var y = 3.0f;

        var q = a*x + b*y + c;
        var f = sigmoid(q);

        var df = 1.0f;
        var dq = (f * (1.0f - f)) * df;

        var da = x * dq;
        var dx = a * dq;
        var dy = b * dq;
        var db = y * dq;
        var dc = 1.0f * dq;
    }

    public class Circuit
    {
        GateMul mg0;
        GateMul mg1;
        GateAdd ag0;
        GateAdd ag1;

        Unit ax;
        Unit by;
        Unit axpby;
        Unit axpbypc;

        public Circuit()
        {
            mg0 = new GateMul();
            mg1 = new GateMul();
            ag0 = new GateAdd();
            ag1 = new GateAdd();
        }

        public Unit Forward(Unit x, Unit y, Unit a, Unit b, Unit c)
        {
            // a*x + b*y + c
            ax = mg0.forward(a, x);
            by = mg1.forward(b, y);
            axpby = ag0.forward(ax, by);
            axpbypc = ag1.forward(axpby, c);
            return axpbypc;
        }

        public void Backward(float gradientTop)
        {
            axpbypc.gradient = gradientTop;
            ag1.backward();
            ag0.backward();
            mg1.backward();
            mg0.backward();
        }
    }

    public class SVM
    {
        Unit a = new Unit(1.0f, 0.0f);
        Unit b = new Unit(-2.0f, 0.0f);
        Unit c = new Unit(-1.0f, 0.0f);

        Circuit circuit = new Circuit();

        Unit unit_out;

        public Unit Forward(Unit x, Unit y)
        {
            unit_out = circuit.Forward(x, y, a, b, c);
            return unit_out;
        }

        public void Backward(float label)
        {
            a.gradient = 0.0f;
            b.gradient = 0.0f;
            c.gradient = 0.0f;

            var pull = 0.0f;
            if (label == 1.0f && unit_out.value < 1.0f)
                pull = 1.0f;
            if (label == -1.0f && unit_out.value > -1.0f)
                pull = -1.0f;

            circuit.Backward(pull);

            a.gradient += -a.value;
            b.gradient += -b.value;
        }

        public void LearnFrom(Unit x, Unit y, float label)
        {
            Forward(x, y);
            Backward(label);
            ParameterUpdate();
        }

        public void ParameterUpdate()
        {
            float step = 0.0f;
            a.value += step * a.gradient;
            b.value += step * b.gradient;
            c.value += step * c.gradient;
        }
    }

    public float Evaluate(List<float[]> data, SVM svm)
    {
        var correct = 0;
        for (int i = 0; i < data.Count; ++i)
        {
            var x = new Unit(data[i][0], 0.0f);
            var y = new Unit(data[i][1], 0.0f);
            var label = data[i][2];

            var predictedValue = svm.Forward(x, y).value;
            var predictedLabel = predictedValue > 0.0f ? 1.0f : -1.0f;

            if (predictedLabel == label)
                correct++;
        }

        return correct / (float)data.Count;
    }

    public void Test3()
    {
        var data = new List<float[]>();

        data.Add(new float[] { 1.0f, 0.7f, 1.0f, });
        data.Add(new float[] { -0.3f, -0.5f, -1.0f });
        data.Add(new float[] { 3.0f, 0.1f, 1.0f });
        data.Add(new float[] { -0.1f, -1.0f, -1.0f });
        data.Add(new float[] { -1.0f, 1.1f, -1.0f });
        data.Add(new float[] { 2.0f, -3.0f, 1.0f });

        // ref probs, not working
        var svm = new SVM();

        for (int i = 0; i < 400; ++i)
        {
            var index = Random.Range(0, data.Count);
            var x = new Unit(data[index][0], 0.0f);
            var y = new Unit(data[index][1], 0.0f);
            var label = data[index][2];

            svm.LearnFrom(x, y, label);

            if (i % 25 == 0)
                Debug.LogFormat("training: {0}: accuracy: {1}", i, Evaluate(data, svm));
        }
    }

    public void Test4()
    {
        var data = new List<float[]>();

        data.Add(new float[] { 1.2f, 0.7f, 1.0f, });
        data.Add(new float[] { -0.3f, -0.5f, -1.0f });
        data.Add(new float[] { 3.0f, 0.1f, 1.0f });
        data.Add(new float[] { -0.1f, -1.0f, -1.0f });
        data.Add(new float[] { -1.0f, 1.1f, -1.0f });
        data.Add(new float[] { 2.1f, -3.0f, 1.0f });

        // a*x + b*y + c;
        var a = 1.0f;
        var b = -2.0f;
        var c = -1.0f;

        for (int i = 0; i < 400; ++i)
        {
            var index = Random.Range(0, data.Count);
            var x = data[index][0];
            var y = data[index][1];
            var label = data[index][2];

            var score = a*x + b*y + c;
            var pull = 0.0f;
            if (label == +1.0f && score < +1.0f) pull = +1.0f;
            if (label == -1.0f && score > -1.0f) pull = -1.0f;

            var step = 0.01f;
            a += step * (x * pull - a);
            b += step * (y * pull - b);
            c += step * (1.0f * pull);

            if (i % 25 == 0)
            {
                var correct = 0;
                for (int j = 0; j < data.Count; ++j)
                {
                    var predictedValue = a*data[j][0] + b*data[j][1] + c;
                    var predictedLabel = predictedValue > 0.0f ? +1.0f : -1.0f;
                    var actualLabel = data[j][2];
                    if (actualLabel == predictedLabel)
                        correct++;
                }
                var accuracy = (float)correct / (float)data.Count;

                Debug.LogFormat("training: {0}: accuracy: {1}", i, accuracy);
            }
        }
    }

    public void Test5()
    {
        var data = new List<float[]>();

        data.Add(new float[] { 1.2f, 0.7f, 1.0f, });
        data.Add(new float[] { -0.3f, -0.5f, -1.0f });
        data.Add(new float[] { 3.0f, 0.1f, 1.0f });
        data.Add(new float[] { -0.1f, -1.0f, -1.0f });
        data.Add(new float[] { -1.0f, 1.1f, -1.0f });
        data.Add(new float[] { 2.1f, -3.0f, 1.0f });

        float a0,b0,c0;
        float a1,b1,c1;
        float a2,b2,c2;
        float a3,b3,c3,d3;

        a0 = Random.Range(-0.5f, 0.5f);
        b0 = Random.Range(-0.5f, 0.5f);
        c0 = Random.Range(-0.5f, 0.5f);

        a1 = Random.Range(-0.5f, 0.5f);
        b1 = Random.Range(-0.5f, 0.5f);
        c1 = Random.Range(-0.5f, 0.5f);

        a2 = Random.Range(-0.5f, 0.5f);
        b2 = Random.Range(-0.5f, 0.5f);
        c2 = Random.Range(-0.5f, 0.5f);

        a3 = Random.Range(-0.5f, 0.5f);
        b3 = Random.Range(-0.5f, 0.5f);
        c3 = Random.Range(-0.5f, 0.5f);
        d3 = Random.Range(-0.5f, 0.5f);

        for (int i = 0; i < 400; ++i)
        {
            var index = Random.Range(0, data.Count); 
            var x = data[index][0];
            var y = data[index][1];
            var label = data[index][2];

            var n0 = Mathf.Max(0.0f, a0*x + b0*y + c0);
            var n1 = Mathf.Max(0.0f, a1*x + b1*y + c1);
            var n2 = Mathf.Max(0.0f, a2*x + b2*y + c2);
            var score = a3*n0 + b3*n1 + c3*n2 + d3;

            var pull = 0.0f;

            if (label == +1.0f && score < +1.0f) pull = +1.0f;
            if (label == -1.0f && score > -1.0f) pull = -1.0f;

            var dscore = pull;
            var da3 = n0 * dscore;
            var dn0 = a3 * dscore;
            var db3 = n1 * dscore;
            var dn1 = b3 * dscore;
            var dc3 = n2 * dscore;
            var dn2 = c3 * dscore;
            var dd3 = 1.0f * dscore;

            dn2 = n2 == 0.0f ? 0.0f : dn2;
            dn1 = n1 == 0.0f ? 0.0f : dn1;
            dn0 = n0 == 0.0f ? 0.0f : dn0;

            var da0 = x * dn0;
            var db0 = y * dn0;
            var dc0 = 1.0f * dn0;

            var da1 = x * dn1;
            var db1 = y * dn1;
            var dc1 = 1.0f * dn1;

            var da2 = x * dn2;
            var db2 = y * dn2;
            var dc2 = 1.0f * dn2;

            da0 += -a0;
            da1 += -a1;
            da2 += -a2;

            db0 += -b0;
            db1 += -b1;
            db2 += -b2;

            dc0 += -c0;
            dc1 += -c1;
            dc2 += -c2;

            var step = 0.01f;

            a0 += step * da0;
            b0 += step * db0;
            c0 += step * dc0;

            a1 += step * da1;
            b1 += step * db1;
            c1 += step * dc1;

            a2 += step * da2;
            b2 += step * db2;
            c2 += step * dc2;

            a3 += step * da3;
            b3 += step * db3;
            c3 += step * dc3;

            if (i % 25 == 0)
            {
                var correct = 0;
                for (int j = 0; j < data.Count; ++j)
                {
                    var checkX = data[j][0];
                    var checkY = data[j][1];
                    var check0 = Mathf.Max(0.0f, a0 * checkX + b0 * checkY + c0);
                    var check1 = Mathf.Max(0.0f, a1 * checkX + b1 * checkY + c1);
                    var check2 = Mathf.Max(0.0f, a2 * checkX + b2 * checkY + c2);
                    var check3 = a3 * check0 + b3 * check1 + c3 * check2 + d3;

                    var predictedLabel = check3 > 0.0f ? +1.0f : -1.0f;
                    var actualLabel = data[j][2];
                    if (actualLabel == predictedLabel)
                        correct++;
                }
                var accuracy = (float)correct / (float)data.Count;

                Debug.LogFormat("training: {0}: accuracy: {1}", i, accuracy);
            }
        }

        
    }

}

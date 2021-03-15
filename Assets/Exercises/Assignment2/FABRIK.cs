using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfGD.Assignment2
{
    public class FABRIK : MonoBehaviour
    {
        [Tooltip("the joints that we are controlling")]
        public Transform[] joints;

        [Tooltip("target that our end effector is trying to reach")]
        public Transform target;

        [Tooltip("error tolerance, will stop updating after distance between end effector and target is smaller than tolerance.")]
        [Range(.01f, .2f)]
        public float tolerance = 0.05f;

        [Tooltip("maximum number of iterations before we follow to the next frame")]
        [Range(1, 100)]
        public int maxIterations = 20;

        [Tooltip("rotation constraint. " +
        	"Instead of an elipse with 4 rotation limits, " +
        	"we use a circle with a single rotation limit. " +
        	"Implementation will be a lot simpler than in the paper.")]
        [Range(0f, 180f)]
        public float rotationLimit = 45f;

        // distances/lengths between joints.
        private float[] distances;
        // total length of the system
        private float chainLength;

        private void RotateLink(int i)
        {
            joints[i].rotation = Quaternion.LookRotation(joints[i + 1].position - joints[i].position, Vector3.up);
            joints[i].Rotate(new Vector3(0, -90, 0), Space.Self);
        }

        private Vector3 JointLimit(Vector3 pp, Vector3 p, Vector3 pn)
        {
            float t = rotationLimit * Mathf.Deg2Rad;
            Vector3 l = p - pp;
            Vector3 ln = pn - p;
            if (Vector3.Angle(l, ln) < rotationLimit)
            { // if the angle(l, ln) < limit return early
                return pn;
            }
            Vector3 o = Vector3.Project(ln, l);
            if (Vector3.Dot(o, l) < 0 && rotationLimit < 90)
            { // if the angle(l, ln) > 90 degrees reflect (unless limit is also above 90)
                o = -o;
                ln = Vector3.Reflect(ln, l);
            }
            Vector3 po = p + o;
            Vector3 d = pn - po / (pn - po).magnitude;
            float r = Mathf.Abs(o.magnitude * Mathf.Tan(t));
            return po + r * d;
        }

        private void Solve()
        {
            float targetDistance = (target.position - joints[0].position).magnitude;
            if (targetDistance > chainLength)
            { // target is unreachable
                for (int i = 0; i + 1 < joints.Length; i++)
                {
                    Vector3 pos = joints[i].position;
                    float dist = (target.position - pos).magnitude;
                    float lambda = distances[i] / dist;
                    joints[i + 1].position = (1 - lambda) * pos + lambda * target.position;

                    RotateLink(i);                    
                }
            } else
            { // target is reachable
                Vector3 b = joints[0].position;
                int n = joints.Length - 1;
                float dif = (joints[n].position - target.position).magnitude;
                int iterations = 0;
                while (dif > tolerance)
                {
                    // Forward reaching
                    joints[n].position = target.position;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        // joint limit
                        if (i >= 2)
                        {
                            joints[i].position = JointLimit(joints[i - 2].position, joints[i - 1].position, joints[i].position);
                        }

                        Vector3 pos = joints[i].position;
                        Vector3 pos1 = joints[i + 1].position;
                        float dist = (pos1 - pos).magnitude;
                        float lambda = distances[i] / dist;
                        joints[i].position = (1 - lambda) * pos1 + lambda * pos;

                        RotateLink(i);
                    }

                    //Backward reaching
                    joints[0].position = b;
                    for(int i = 0; i < n; i++)
                    {
                        // joint limit
                        if (i >= 1)
                        {
                            joints[i + 1].position = JointLimit(joints[i - 1].position, joints[i].position, joints[i + 1].position);
                        }

                        Vector3 pos = joints[i].position;
                        Vector3 pos1 = joints[i + 1].position;
                        float dist = (pos1 - pos).magnitude;
                        float lambda = distances[i] / dist;
                        joints[i + 1].position = (1 - lambda) * pos + lambda * pos1;

                        RotateLink(i);
                    }

                    dif = (joints[n].position - target.position).magnitude;

                    // stop after X iterations
                    iterations++;
                    if (iterations >= maxIterations) break;
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // pre-compute segment lenghts and total length of the chain
            // we assume that the segment/bone length is constant during execution
            distances = new float[joints.Length-1];
            chainLength = 0;
            // If we have N joints, then there are N-1 segment/bone lengths connecting these joints
            for (int i = 0; i < joints.Length - 1; i++)
            {
                distances[i] = (joints[i + 1].position - joints[i].position).magnitude;
                chainLength += distances[i];
            }
        }

        void Update()
        {
            Solve();
            for (int i = 1; i < joints.Length - 1; i++)
            {
                DebugJointLimit(joints[i], joints[i - 1], rotationLimit, 2);
            }
        }

        /// <summary>
        /// Helper function to draw the joint limit in the editor
        /// The drawing migh not make sense if you did not complete the 
        /// second task in the assignment (joint rotations)
        /// </summary>
        /// <param name="tr">current joint</param>
        /// <param name="trPrev">previous joint</param>
        /// <param name="angle">angle limit in degrees</param>
        /// <param name="scale"></param>
        void DebugJointLimit(Transform tr, Transform trPrev, float angle, float scale = 1)
        {
            float angleRad = Mathf.Deg2Rad * angle;
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);
            int steps = 36;
            float stepSize = 360f / steps;
            // steps is the number of line segments used to draw the cone
            for (int i = 0; i < steps; i++)
            {
                float twistRad = Mathf.Deg2Rad * i * stepSize;
                Vector3 vec = new Vector3(cosAngle, 0, 0);
                vec.y = Mathf.Cos(twistRad) * sinAngle;
                vec.z = Mathf.Sin(twistRad) * sinAngle;
                vec = trPrev.rotation * vec;
                
                twistRad = Mathf.Deg2Rad * (i+1) * stepSize;
                Vector3 vec2 = new Vector3(cosAngle, 0, 0);
                vec2.y = Mathf.Cos(twistRad) * sinAngle;
                vec2.z = Mathf.Sin(twistRad) * sinAngle;
                vec2 = trPrev.rotation * vec2;

                Debug.DrawLine(tr.position, tr.position + vec * scale, Color.white);
                Debug.DrawLine(tr.position + vec * scale, tr.position + vec2 * scale, Color.white);
            }
        }
    }

}
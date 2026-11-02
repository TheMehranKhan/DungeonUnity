using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DungeonUnity
{
    /// Helpers for rigging body+legs models: name based leg detection, pivot wrapping and static mesh splitting.
    public static class RigUtility
    {
        public enum Part { None, Pelvis, UpperLeg, LowerLeg }

        public struct Detected
        {
            public Transform pelvis, leftUpper, leftLower, rightUpper, rightLower;
            public bool HasLegs => leftUpper && rightUpper;
        }

        public class SplitResult
        {
            public Transform body, leftUpper, leftLower, rightUpper, rightLower;
            public List<Mesh> meshes = new List<Mesh>();
        }

        static readonly Regex Upper = new Regex(@"(up_?leg|upper_?leg|thigh|leg_?upper|hip_?[lr]\b|^leg[_ .]?[lr]$|^[lr][_ .]?leg$)", RegexOptions.IgnoreCase);
        static readonly Regex Lower = new Regex(@"(calf|shin|knee|lower_?leg|leg_?lower|^leg$|foreleg)", RegexOptions.IgnoreCase);
        static readonly Regex Pelvis = new Regex(@"(pelvis|hips\b|^hip$|root_?hip)", RegexOptions.IgnoreCase);
        static readonly Regex LeftRx = new Regex(@"(^|[^a-z])(l|left)([^a-z]|$)|left", RegexOptions.IgnoreCase);
        static readonly Regex RightRx = new Regex(@"(^|[^a-z])(r|right)([^a-z]|$)|right", RegexOptions.IgnoreCase);

        /// -1 left, +1 right, 0 unknown.
        public static int Side(string name)
        {
            string n = name.Replace("mixamorig:", "");
            if (n.StartsWith("Left")) return -1; if (n.StartsWith("Right")) return 1;
            bool l = LeftRx.IsMatch(n), r = RightRx.IsMatch(n);
            return l == r ? 0 : (l ? -1 : 1);
        }

        public static Part Classify(string name)
        {
            string n = name.Replace("mixamorig:", "");
            if (Regex.IsMatch(n, @"^(Left|Right)Leg$")) return Part.LowerLeg;      // Mixamo naming
            if (Regex.IsMatch(n, @"^(Left|Right)UpLeg$")) return Part.UpperLeg;
            if (Regex.IsMatch(n, @"foot|toe|ankle", RegexOptions.IgnoreCase)) return Part.None;
            if (Lower.IsMatch(n)) return Part.LowerLeg;
            if (Upper.IsMatch(n) || Regex.IsMatch(n, @"leg", RegexOptions.IgnoreCase)) return Part.UpperLeg;
            if (Pelvis.IsMatch(n)) return Part.Pelvis;
            return Part.None;
        }

        public static Detected Detect(Transform root)
        {
            var d = new Detected();
            if (!root) return d;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) continue;
                var p = Classify(t.name); int s = Side(t.name);
                if (s == 0 && p != Part.Pelvis && p != Part.None)
                {
                    // Unnamed side: use position relative to the root.
                    float x = Vector3.Dot(t.position - root.position, root.right);
                    if (Mathf.Abs(x) > 0.01f) s = x < 0 ? -1 : 1;
                }
                switch (p)
                {
                    case Part.Pelvis: if (!d.pelvis) d.pelvis = t; break;
                    case Part.UpperLeg: if (s < 0 && !d.leftUpper) d.leftUpper = t; else if (s > 0 && !d.rightUpper) d.rightUpper = t; break;
                    case Part.LowerLeg: if (s < 0 && !d.leftLower) d.leftLower = t; else if (s > 0 && !d.rightLower) d.rightLower = t; break;
                }
            }
            return d;
        }

        public static bool HasRenderer(Transform t) => t && t.GetComponent<Renderer>();
        /// A transform with no renderer of its own is treated as a bone/joint.
        public static bool IsBone(Transform t) => t && !t.GetComponent<Renderer>();

        public static Bounds RendererBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// Puts a mesh part under a new pivot placed at the top centre of its bounds, so rotating the pivot swings it like a joint.
        public static Transform WrapInPivot(Transform part, Transform parent, string name)
        {
            var b = RendererBounds(part);
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, true);
            pivot.position = new Vector3(b.center.x, b.max.y, b.center.z);
            pivot.rotation = parent ? parent.rotation : Quaternion.identity;
            part.SetParent(pivot, true);
            return pivot;
        }

        /// Splits a single static mesh (body + two legs) into body, left/right upper and lower leg meshes with hip and knee pivots.
        /// legFraction: crotch height as a fraction of mesh height (0 = auto). Mesh space is assumed Y up, X right (Unity glTF import).
        public static SplitResult SplitStaticMesh(Transform root, MeshFilter mf, float legFraction = 0f, bool splitKnees = true, float kneeFraction = 0.5f)
        {
            var res = new SplitResult();
            var src = mf ? mf.sharedMesh : null;
            if (!src || !src.isReadable) return null;
            var mr = mf.GetComponent<MeshRenderer>();
            var v = src.vertices; var b = src.bounds;
            float cx = b.center.x;
            float crotch = legFraction > 0f ? b.min.y + b.size.y * legFraction : AutoCrotch(src, v, b);
            float knee = b.min.y + (crotch - b.min.y) * Mathf.Clamp(kneeFraction, 0.1f, 0.9f);

            // bucket 0 body, 1 L upper, 2 L lower, 3 R upper, 4 R lower
            var buckets = new List<int>[5, src.subMeshCount];
            for (int k = 0; k < 5; k++) for (int s = 0; s < src.subMeshCount; s++) buckets[k, s] = new List<int>();
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tris = src.GetTriangles(s);
                for (int i = 0; i + 2 < tris.Length; i += 3)
                {
                    Vector3 c = (v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f;
                    int k = 0;
                    if (c.y < crotch)
                    {
                        bool left = c.x < cx;
                        bool lower = splitKnees && c.y < knee;
                        k = left ? (lower ? 2 : 1) : (lower ? 4 : 3);
                    }
                    buckets[k, s].Add(tris[i]); buckets[k, s].Add(tris[i + 1]); buckets[k, s].Add(tris[i + 2]);
                }
            }

            var t = mf.transform;
            Vector3 hipL = t.TransformPoint(new Vector3(Mathf.Lerp(b.min.x, cx, 0.5f), crotch, b.center.z));
            Vector3 hipR = t.TransformPoint(new Vector3(Mathf.Lerp(cx, b.max.x, 0.5f), crotch, b.center.z));
            Vector3 kneeL = t.TransformPoint(new Vector3(Mathf.Lerp(b.min.x, cx, 0.5f), knee, b.center.z));
            Vector3 kneeR = t.TransformPoint(new Vector3(Mathf.Lerp(cx, b.max.x, 0.5f), knee, b.center.z));

            res.body = MakePart(root, t, mr, SubMesh(src, buckets, 0, src.name + "_Body"), "Body", root, res);
            var hl = Pivot("Hip_L", root, hipL); var hr = Pivot("Hip_R", root, hipR);
            res.leftUpper = hl; res.rightUpper = hr;
            MakePart(root, t, mr, SubMesh(src, buckets, 1, src.name + "_UpperLeg_L"), "UpperLeg_L", hl, res);
            MakePart(root, t, mr, SubMesh(src, buckets, 3, src.name + "_UpperLeg_R"), "UpperLeg_R", hr, res);
            if (splitKnees)
            {
                var kl = Pivot("Knee_L", hl, kneeL); var kr = Pivot("Knee_R", hr, kneeR);
                res.leftLower = kl; res.rightLower = kr;
                MakePart(root, t, mr, SubMesh(src, buckets, 2, src.name + "_LowerLeg_L"), "LowerLeg_L", kl, res);
                MakePart(root, t, mr, SubMesh(src, buckets, 4, src.name + "_LowerLeg_R"), "LowerLeg_R", kr, res);
            }
            if (mr) mr.enabled = false;
            return res;
        }

        /// Finds the crotch: scanning up from the feet, the highest height where the cross section still has a gap at the centre line.
        static float AutoCrotch(Mesh m, Vector3[] v, Bounds b)
        {
            const int bins = 40; var centre = new bool[bins]; float cx = b.center.x, gap = b.size.x * 0.04f;
            foreach (var p in v)
            {
                int i = Mathf.Clamp((int)((p.y - b.min.y) / Mathf.Max(1e-5f, b.size.y) * bins), 0, bins - 1);
                if (Mathf.Abs(p.x - cx) < gap) centre[i] = true;
            }
            int top = 0; for (int i = 0; i < bins * 3 / 4; i++) { if (centre[i]) break; top = i; }
            float f = top > 1 ? (top + 1f) / bins : 0.45f;
            return b.min.y + b.size.y * f;
        }

        static Transform Pivot(string name, Transform parent, Vector3 world)
        {
            var p = new GameObject(name).transform; p.SetParent(parent, false);
            p.position = world; p.rotation = parent.rotation; return p;
        }

        static Transform MakePart(Transform root, Transform src, MeshRenderer mr, Mesh mesh, string name, Transform parent, SplitResult res)
        {
            if (!mesh) return null;
            res.meshes.Add(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(src.position, src.rotation);
            Vector3 ls = src.lossyScale, ps = parent.lossyScale;
            go.transform.localScale = new Vector3(ls.x / Safe(ps.x), ls.y / Safe(ps.y), ls.z / Safe(ps.z));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); if (mr) r.sharedMaterials = mr.sharedMaterials;
            return go.transform;
        }

        static float Safe(float f) => Mathf.Abs(f) < 1e-6f ? 1f : f;

        static Mesh SubMesh(Mesh src, List<int>[,] buckets, int k, string name)
        {
            int count = 0; for (int s = 0; s < src.subMeshCount; s++) count += buckets[k, s].Count;
            if (count == 0) return null;
            var m = new Mesh { name = name, indexFormat = src.indexFormat };
            m.vertices = src.vertices; m.normals = src.normals; m.tangents = src.tangents;
            m.uv = src.uv; m.uv2 = src.uv2; m.colors = src.colors;
            m.subMeshCount = src.subMeshCount;
            for (int s = 0; s < src.subMeshCount; s++) m.SetTriangles(buckets[k, s], s);
            m.RecalculateBounds();
            return m;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace StylizedGrass
{
    /// <summary>
    /// Procedural low-poly meshes. All are 1 unit tall and sit on y = 0.
    /// Vertex color rgb = base color, vertex alpha = "takes the instance's palette color".
    /// </summary>
    public static class GrassMeshes
    {
        class MB
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector3> n = new List<Vector3>();
            public List<Color> c = new List<Color>();
            public List<Vector2> uv = new List<Vector2>();
            public List<int> t = new List<int>();

            public int Add(Vector3 p, Color col, Vector3 nor)
            {
                v.Add(p); c.Add(col); n.Add(nor); uv.Add(Vector2.zero);
                return v.Count - 1;
            }

            public void Tri(int a, int b, int d) { t.Add(a); t.Add(b); t.Add(d); }

            // Adds a triangle whose front face points toward 'hint'
            public void TriOut(int a, int b, int d, Vector3 hint)
            {
                Vector3 nn = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
                if (Vector3.Dot(nn, hint) < 0f) { int tmp = b; b = d; d = tmp; }
                t.Add(a); t.Add(b); t.Add(d);
            }

            public Mesh ToMesh(string name, bool explicitNormals)
            {
                var m = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
                m.SetVertices(v);
                m.SetColors(c);
                m.SetUVs(0, uv);
                m.SetTriangles(t, 0);
                if (explicitNormals) m.SetNormals(n); else m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        public static Mesh Build(DetailShape shape)
        {
            switch (shape)
            {
                case DetailShape.Daisy: return BuildFlower(12, 0.14f, 0.28f, 0.10f, "Daisy");
                case DetailShape.Mushroom: return BuildMushroom();
                case DetailShape.Rock: return BuildRock();
                default: return BuildFlower(5, 0.38f, 0.30f, 0.25f, "Flower");
            }
        }

        // ------------------------------------------------------------------ Grass tuft
        public static Mesh BuildTuft(int blades)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var rng = new System.Random(1234);

            for (int b = 0; b < blades; b++)
            {
                float a = (b + (float)rng.NextDouble() * 0.4f) / blades * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0, dir.x);
                Vector3 basePos = dir * 0.3f;
                float height = 0.75f + (float)rng.NextDouble() * 0.25f;
                const float lean = 0.25f;

                int s = verts.Count;
                float[] ts = { 0f, 0.5f };
                float[] ws = { 0.20f, 0.14f };
                for (int k = 0; k < 2; k++)
                {
                    float t = ts[k];
                    Vector3 center = basePos + dir * (lean * t * t) + Vector3.up * (t * height);
                    verts.Add(center - side * ws[k] * 0.5f); uvs.Add(new Vector2(0, t));
                    verts.Add(center + side * ws[k] * 0.5f); uvs.Add(new Vector2(1, t));
                }
                verts.Add(basePos + dir * lean + Vector3.up * height); uvs.Add(new Vector2(0.5f, 1f));

                tris.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s + 2, s + 4, s + 3 });
            }

            var m = new Mesh { name = "GrassTuft", hideFlags = HideFlags.HideAndDontSave };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.bounds = new Bounds(Vector3.up * 0.5f, new Vector3(1, 1, 1));
            return m;
        }

        /// <summary>Far-away LOD: 3 single-triangle blades (9 verts, 3 tris vs 15 verts, 9 tris).</summary>
        public static Mesh BuildTuftLow()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var rng = new System.Random(4321);

            for (int b = 0; b < 3; b++)
            {
                float a = (b + (float)rng.NextDouble() * 0.4f) / 3f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0, dir.x);
                Vector3 basePos = dir * 0.3f;
                float height = 0.75f + (float)rng.NextDouble() * 0.25f;

                int s = verts.Count;
                verts.Add(basePos - side * 0.13f); uvs.Add(new Vector2(0, 0));
                verts.Add(basePos + side * 0.13f); uvs.Add(new Vector2(1, 0));
                verts.Add(basePos + dir * 0.25f + Vector3.up * height); uvs.Add(new Vector2(0.5f, 1f));
                tris.AddRange(new[] { s, s + 2, s + 1 });
            }

            var m = new Mesh { name = "GrassTuftLow", hideFlags = HideFlags.HideAndDontSave };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.bounds = new Bounds(Vector3.up * 0.5f, new Vector3(1, 1, 1));
            return m;
        }

        // ------------------------------------------------------------------ Flower / Daisy
        static Mesh BuildFlower(int petals, float petalHalfWidth, float headRadius, float lift, string name)
        {
            var m = new MB();
            Vector3 up = Vector3.up;
            Color stemC = new Color(0.22f, 0.62f, 0.22f, 0f);
            Color centerC = new Color(1f, 0.82f, 0.2f, 0f);
            Color petalC = new Color(1f, 1f, 1f, 1f);

            const float stemTop = 0.82f;
            const float headY = 0.85f;

            // stem: two crossed tapered quads
            for (int k = 0; k < 2; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                int b0 = m.Add(-d * 0.028f, stemC, up);
                int b1 = m.Add(d * 0.028f, stemC, up);
                int t0 = m.Add(-d * 0.014f + up * stemTop, stemC, up);
                int t1 = m.Add(d * 0.014f + up * stemTop, stemC, up);
                m.Tri(b0, t0, b1); m.Tri(b1, t0, t1);
            }

            // petals
            Vector3 c0 = new Vector3(0, headY, 0);
            for (int p = 0; p < petals; p++)
            {
                float a = (float)p / petals * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0, dir.x);
                Vector3 inner = c0 + dir * (headRadius * 0.12f);
                Vector3 mid = c0 + dir * (headRadius * 0.55f) + up * (lift * headRadius * 0.5f);
                Vector3 tip = c0 + dir * headRadius + up * (lift * headRadius);
                int i0 = m.Add(inner, petalC, up);
                int i1 = m.Add(mid - side * (petalHalfWidth * headRadius), petalC, up);
                int i2 = m.Add(tip, petalC, up);
                int i3 = m.Add(mid + side * (petalHalfWidth * headRadius), petalC, up);
                m.Tri(i0, i1, i2); m.Tri(i0, i2, i3);
            }

            // center disc
            float cr = headRadius * 0.24f;
            int hub = m.Add(c0 + up * 0.012f, centerC, up);
            int ring = m.v.Count;
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                m.Add(c0 + new Vector3(Mathf.Cos(a) * cr, 0.01f, Mathf.Sin(a) * cr), centerC, up);
            }
            for (int i = 0; i < 6; i++) m.Tri(hub, ring + i, ring + (i + 1) % 6);

            return m.ToMesh(name, true);
        }

        // ------------------------------------------------------------------ Mushroom
        static int Ring(MB m, float y, float r, int sides, Color col)
        {
            int start = m.v.Count;
            for (int i = 0; i < sides; i++)
            {
                float a = (float)i / sides * Mathf.PI * 2f;
                m.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), col, Vector3.up);
            }
            return start;
        }

        static void Strip(MB m, int a, int b, int sides, float hx, float hy)
        {
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Vector3 cen = (m.v[a + i] + m.v[a + j] + m.v[b + i] + m.v[b + j]) * 0.25f;
                Vector3 hint = new Vector3(cen.x * hx, hy, cen.z * hx);
                m.TriOut(a + i, b + i, b + j, hint);
                m.TriOut(a + i, b + j, a + j, hint);
            }
        }

        static void Fan(MB m, int ringStart, int apex, int sides, float hx, float hy)
        {
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Vector3 cen = (m.v[ringStart + i] + m.v[ringStart + j] + m.v[apex]) / 3f;
                m.TriOut(ringStart + i, apex, ringStart + j, new Vector3(cen.x * hx, hy, cen.z * hx));
            }
        }

        static Mesh BuildMushroom()
        {
            var m = new MB();
            const int sides = 8;
            Color stemC = new Color(0.93f, 0.88f, 0.78f, 0f);
            Color capC = new Color(1f, 1f, 1f, 1f);

            // stem
            int s0 = Ring(m, 0f, 0.11f, sides, stemC);
            int s1 = Ring(m, 0.55f, 0.08f, sides, stemC);
            Strip(m, s0, s1, sides, 1f, 0f);

            // cap
            int u0 = Ring(m, 0.54f, 0.09f, sides, capC);   // underside, near stem
            int u1 = Ring(m, 0.50f, 0.42f, sides, capC);   // rim (bottom)
            int d1 = Ring(m, 0.60f, 0.43f, sides, capC);   // rim (top)
            int d2 = Ring(m, 0.78f, 0.32f, sides, capC);
            int d3 = Ring(m, 0.93f, 0.17f, sides, capC);
            int apex = m.Add(new Vector3(0, 1f, 0), capC, Vector3.up);

            Strip(m, u0, u1, sides, 0f, -1f);   // underside faces down
            Strip(m, u1, d1, sides, 1f, 0f);
            Strip(m, d1, d2, sides, 1f, 0.6f);
            Strip(m, d2, d3, sides, 1f, 0.6f);
            Fan(m, d3, apex, sides, 1f, 0.6f);

            return m.ToMesh("Mushroom", false);
        }

        // ------------------------------------------------------------------ Rock
        static Mesh BuildRock()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] ico =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            var rng = new System.Random(7);
            var pts = new Vector3[ico.Length];
            for (int i = 0; i < ico.Length; i++)
            {
                float r = 0.72f + (float)rng.NextDouble() * 0.28f;
                Vector3 p = ico[i].normalized * r;
                p.y *= 0.75f;
                pts[i] = p;
            }

            var m = new MB();
            float maxY = 0.001f;
            for (int f = 0; f < faces.Length; f += 3)
            {
                Vector3 q0 = pts[faces[f]], q1 = pts[faces[f + 1]], q2 = pts[faces[f + 2]];
                if (q0.y < -0.05f && q1.y < -0.05f && q2.y < -0.05f) continue; // hidden bottom faces

                float tone = 0.82f + (float)rng.NextDouble() * 0.18f;
                Color col = new Color(tone, tone, tone, 1f);
                Vector3 hint = (q0 + q1 + q2) / 3f;

                Vector3 p0 = new Vector3(q0.x, Mathf.Max(q0.y, 0f), q0.z);
                Vector3 p1 = new Vector3(q1.x, Mathf.Max(q1.y, 0f), q1.z);
                Vector3 p2 = new Vector3(q2.x, Mathf.Max(q2.y, 0f), q2.z);
                maxY = Mathf.Max(maxY, p0.y, p1.y, p2.y);

                int a = m.Add(p0, col, Vector3.up);
                int b = m.Add(p1, col, Vector3.up);
                int c = m.Add(p2, col, Vector3.up);
                m.TriOut(a, b, c, hint);
            }

            // normalize to height 1
            for (int i = 0; i < m.v.Count; i++) m.v[i] = m.v[i] / maxY;
            return m.ToMesh("Rock", false);
        }
    }
}

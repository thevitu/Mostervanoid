using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Fábrica/cache de materiais (URP Lit).</summary>
    public static class Mat
    {
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string BaseColor = "_BaseColor", BaseMap = "_BaseMap";
        static Material baseMat, trailMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static Material Base
        {
            get
            {
                if (baseMat == null)
                {
                    // Material em Resources garante que o shader URP/Lit entre no build.
                    baseMat = Resources.Load<Material>("Materials/BaseMat");
                    if (baseMat == null) baseMat = new Material(Shader.Find(LitShader));
                }
                return baseMat;
            }
        }

        public static Material New(Color c, float smoothness = 0.35f, float metallic = 0f)
        {
            var m = new Material(Base);
            m.SetColor(BaseColor, c);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static Material Color(Color c, float smoothness = 0.35f, float metallic = 0f)
        {
            string key = $"c{c}{smoothness}{metallic}";
            if (!cache.TryGetValue(key, out var m) || m == null)
            {
                m = New(c, smoothness, metallic);
                cache[key] = m;
            }
            return m;
        }

        public static Material Glow(Color c, float intensity = 2f)
        {
            string key = $"g{c}{intensity}";
            if (!cache.TryGetValue(key, out var m) || m == null)
            {
                m = New(c, 0.6f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", c * intensity);
                cache[key] = m;
            }
            return m;
        }

        public static Material Textured(Texture tex, Color tint, float smoothness = 0.2f, float metallic = 0f, Vector2? tiling = null)
        {
            var m = New(tint, smoothness, metallic);
            m.SetTexture(BaseMap, tex);
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
            return m;
        }

        /// <summary>Material aditivo/transparente para rastros de energia (TrailRenderer).</summary>
        public static Material Trail
        {
            get
            {
                if (trailMat == null)
                {
                    trailMat = Resources.Load<Material>("Materials/TrailMat");
                    if (trailMat == null) trailMat = new Material(Shader.Find("Sprites/Default"));
                }
                return trailMat;
            }
        }

        public static void SetGlow(Material m, Color c)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
        }
    }

    /// <summary>Criação de primitivas 3D sem colisores (a colisão do jogo é por grade).</summary>
    public static class Prim
    {
        static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh MeshOf(PrimitiveType t)
        {
            if (!meshes.TryGetValue(t, out var mesh) || mesh == null)
            {
                var tmp = GameObject.CreatePrimitive(t);
                mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tmp);
                meshes[t] = mesh;
            }
            return mesh;
        }

        public static GameObject Make(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 localScale,
            Material mat, Vector3 euler = default, string name = null)
        {
            return MakeMesh(MeshOf(t), parent, localPos, localScale, mat, euler, name ?? t.ToString());
        }

        public static GameObject MakeMesh(Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale,
            Material mat, Vector3 euler = default, string name = "Mesh")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return go;
        }

        public static Transform Pivot(Transform parent, Vector3 localPos, string name = "Pivot")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        static Mesh cone;

        /// <summary>Cone com base de raio 0.5 em y=0 e ponta em y=1 (para estacas/espinhos).</summary>
        public static Mesh Cone
        {
            get
            {
                if (cone != null) return cone;
                const int seg = 12;
                var verts = new List<Vector3>();
                var norms = new List<Vector3>();
                var tris = new List<int>();
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg, am = (a0 + a1) * 0.5f;
                    var b0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                    var b1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                    int k = verts.Count;
                    verts.Add(Vector3.up); norms.Add(new Vector3(Mathf.Cos(am), 0.5f, Mathf.Sin(am)).normalized);
                    verts.Add(b1); norms.Add(new Vector3(Mathf.Cos(a1), 0.5f, Mathf.Sin(a1)).normalized);
                    verts.Add(b0); norms.Add(new Vector3(Mathf.Cos(a0), 0.5f, Mathf.Sin(a0)).normalized);
                    tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
                }
                cone = new Mesh { name = "Cone" };
                cone.SetVertices(verts); cone.SetNormals(norms); cone.SetTriangles(tris, 0);
                cone.RecalculateBounds();
                return cone;
            }
        }

        /// <summary>Tubo cilíndrico (só a lateral) com UV: u dá a volta (u=0.5 voltado para a câmera, -Z), v = altura.</summary>
        public static Mesh Tube(int segments = 40)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float a = u * Mathf.PI * 2f;
                var dir = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts.Add(dir * 0.5f + Vector3.down * 0.5f); norms.Add(dir); uvs.Add(new Vector2(u, 0f));
                verts.Add(dir * 0.5f + Vector3.up * 0.5f); norms.Add(dir); uvs.Add(new Vector2(u, 1f));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                tris.Add(a); tris.Add(b); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
            var m = new Mesh { name = "Tube" };
            m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}

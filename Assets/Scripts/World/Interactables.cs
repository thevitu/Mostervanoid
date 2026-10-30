using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Ponto de teleporte (criado após a sala do chefe). Ativo quando o chefe da fase foi derrotado.</summary>
    public class TeleportPad : MonoBehaviour
    {
        public bool IsActive { get; private set; }
        Renderer ring, core;
        Transform floaters;
        Material on, off;

        public static TeleportPad Create(Transform parent, Vector2 feet)
        {
            var go = new GameObject("TeleportPad");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(feet.x, feet.y, 0.3f);
            var t = go.AddComponent<TeleportPad>();
            var metal = Mat.Color(new Color(0.45f, 0.47f, 0.52f), 0.8f, 0.8f);
            Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.08f, 0), new Vector3(2.0f, 0.08f, 2.0f), metal);
            t.on = Mat.Glow(new Color(0.2f, 0.9f, 1f), 2.5f);
            t.off = Mat.Color(new Color(0.2f, 0.25f, 0.3f), 0.5f);
            t.ring = Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.17f, 0), new Vector3(1.6f, 0.02f, 1.6f), t.off).GetComponent<Renderer>();
            t.core = Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.2f, 0), new Vector3(0.8f, 0.02f, 0.8f), metal).GetComponent<Renderer>();
            t.floaters = Prim.Pivot(go.transform, new Vector3(0, 1.4f, 0), "Floaters");
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2;
                Prim.Make(PrimitiveType.Cube, t.floaters, new Vector3(Mathf.Cos(a) * 0.9f, 0, Mathf.Sin(a) * 0.9f), Vector3.one * 0.18f, t.on, new Vector3(45, 45, 0));
            }
            t.floaters.gameObject.AddComponent<Spin>().speed = new Vector3(0, 120, 0);
            t.SetActive(false);
            return t;
        }

        public void SetActive(bool a)
        {
            IsActive = a;
            ring.sharedMaterial = a ? on : off;
            floaters.gameObject.SetActive(a);
        }

        void Update()
        {
            if (!IsActive) return;
            floaters.localPosition = new Vector3(0, 1.3f + Mathf.Sin(Time.time * 2) * 0.2f, 0);
        }
    }

    /// <summary>Jato de vapor (uma célula): rajadas brilhantes correndo na direção do empurrão.</summary>
    public class WindFx : MonoBehaviour
    {
        Transform[] streaks;
        float[] phase;
        int dir;

        public static GameObject Create(Transform parent, int x, int y, int dir)
        {
            var go = new GameObject("SteamJet");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0.3f);
            var w = go.AddComponent<WindFx>();
            w.dir = dir;
            var mat = Mat.Glow(new Color(0.85f, 0.95f, 1f), 1.6f);
            w.streaks = new Transform[4];
            w.phase = new float[4];
            for (int i = 0; i < 4; i++)
            {
                w.streaks[i] = Prim.Make(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.45f, 0.05f, 0.05f), mat, default, "Streak").transform;
                w.phase[i] = Random.value;
            }
            // névoa de fundo
            Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(0, 0, 0.8f), new Vector3(1.1f, 0.9f, 0.5f), Mat.Color(new Color(0.8f, 0.85f, 0.9f), 0.2f), default, "Mist");
            return go;
        }

        void Update()
        {
            for (int i = 0; i < streaks.Length; i++)
            {
                phase[i] = Mathf.Repeat(phase[i] + Time.deltaTime * 2.5f, 1f);
                streaks[i].localPosition = new Vector3(dir * (phase[i] - 0.5f), -0.35f + i * 0.23f, -0.2f);
            }
        }
    }

    /// <summary>Portal de acesso à próxima fase (após a sala do chefe).</summary>
    public class ExitPortal : MonoBehaviour
    {
        public bool IsActive { get; private set; }
        Renderer disc;
        Transform swirl;
        Material on, off;

        public static ExitPortal Create(Transform parent, Vector2 feet)
        {
            var go = new GameObject("ExitPortal");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(feet.x, feet.y, 0.8f);
            var e = go.AddComponent<ExitPortal>();
            var frame = Mat.Color(new Color(0.3f, 0.3f, 0.35f), 0.7f, 0.8f);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(-1.1f, 1.4f, 0), new Vector3(0.35f, 2.8f, 0.6f), frame);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(1.1f, 1.4f, 0), new Vector3(0.35f, 2.8f, 0.6f), frame);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 2.9f, 0), new Vector3(2.6f, 0.4f, 0.6f), frame);
            e.on = Mat.Glow(new Color(0.9f, 0.25f, 1f), 2.2f);
            e.off = Mat.Color(new Color(0.12f, 0.1f, 0.15f));
            e.disc = Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.4f, 0.1f), new Vector3(1.9f, 0.02f, 2.6f), e.off, new Vector3(90, 0, 0)).GetComponent<Renderer>();
            e.swirl = Prim.Pivot(go.transform, new Vector3(0, 1.4f, -0.05f), "Swirl");
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Cube, e.swirl, Quaternion.Euler(0, 0, i * 60) * new Vector3(0.6f, 0, 0), new Vector3(0.5f, 0.08f, 0.02f), Mat.Glow(Color.white, 1.5f), new Vector3(0, 0, i * 60 + 30));
            e.swirl.gameObject.AddComponent<Spin>().speed = new Vector3(0, 0, -200);
            e.SetActive(false);
            return e;
        }

        public void SetActive(bool a)
        {
            IsActive = a;
            disc.sharedMaterial = a ? on : off;
            swirl.gameObject.SetActive(a);
        }
    }
}

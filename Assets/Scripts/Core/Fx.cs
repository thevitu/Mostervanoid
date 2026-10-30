using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Efeitos simples feitos com primitivas (explosões, faíscas, poeira).</summary>
    public static class Fx
    {
        public static void Explode(Vector2 pos, Color color, int pieces = 14, float power = 7f, float size = 0.22f)
        {
            Sfx.Play(Sfx.Explode);
            var root = GameManager.I != null ? GameManager.I.LevelRoot : null;
            var flash = Prim.Make(PrimitiveType.Sphere, root, new Vector3(pos.x, pos.y, -0.3f), Vector3.one * 0.5f, Mat.Glow(new Color(1f, 0.9f, 0.5f), 3f), default, "Flash");
            flash.AddComponent<Debris>().InitFlash(0.25f, 2.2f);
            for (int i = 0; i < pieces; i++)
            {
                var c = i % 3 == 0 ? new Color(1f, 0.85f, 0.3f) : color;
                var go = Prim.Make(PrimitiveType.Cube, root, new Vector3(pos.x, pos.y, -0.2f), Vector3.one * size * Random.Range(0.6f, 1.4f),
                    i % 3 == 0 ? Mat.Glow(c, 2f) : Mat.Color(c), Random.insideUnitSphere * 180f, "Debris");
                var v = Random.insideUnitCircle.normalized * Random.Range(power * 0.4f, power) + Vector2.up * 3f;
                go.AddComponent<Debris>().Init(new Vector3(v.x, v.y, Random.Range(-3f, 3f)), 25f, Random.Range(0.5f, 0.9f));
            }
        }

        public static void Sparks(Vector2 pos, Color color, int n)
        {
            var root = GameManager.I != null ? GameManager.I.LevelRoot : null;
            var mat = Mat.Glow(color, 2.5f);
            for (int i = 0; i < n; i++)
            {
                var go = Prim.Make(PrimitiveType.Cube, root, new Vector3(pos.x, pos.y, -0.3f), Vector3.one * 0.08f, mat, default, "Spark");
                var v = Random.insideUnitCircle * 6f;
                go.AddComponent<Debris>().Init(new Vector3(v.x, v.y, 0), 10f, 0.25f);
            }
        }

        /// <summary>Fogo de artifício: explosão radial de faíscas brilhantes que caem devagar.</summary>
        public static void Firework(Vector3 pos, Color color, int n = 36)
        {
            var root = GameManager.I != null ? GameManager.I.LevelRoot : null;
            var mat = Mat.Glow(color, 3f);
            var core = Prim.Make(PrimitiveType.Sphere, root, pos, Vector3.one * 0.6f, Mat.Glow(Color.white, 4f), default, "FireworkCore");
            core.AddComponent<Debris>().InitFlash(0.25f, 3f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var go = Prim.Make(PrimitiveType.Cube, root, pos, Vector3.one * 0.14f, mat, default, "Firework");
                var v = new Vector3(Mathf.Cos(a), Mathf.Sin(a), Random.Range(-0.3f, 0.3f)) * Random.Range(5f, 8f);
                go.AddComponent<Debris>().Init(v, 4f, Random.Range(1.0f, 1.6f));
            }
        }

        public static void Dust(Vector2 pos, int n = 6)
        {
            var root = GameManager.I != null ? GameManager.I.LevelRoot : null;
            var mat = Mat.Color(new Color(0.85f, 0.82f, 0.75f));
            for (int i = 0; i < n; i++)
            {
                var go = Prim.Make(PrimitiveType.Sphere, root, new Vector3(pos.x + Random.Range(-0.4f, 0.4f), pos.y + 0.1f, Random.Range(-0.4f, 0.4f)), Vector3.one * 0.2f, mat, default, "Dust");
                go.AddComponent<Debris>().Init(new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 2f), 0), 0f, 0.4f);
            }
        }

        public static void Shockwave(Vector2 pos, float radius)
        {
            var root = GameManager.I != null ? GameManager.I.LevelRoot : null;
            var ring = Prim.Make(PrimitiveType.Cylinder, root, new Vector3(pos.x, pos.y + 0.05f, 0.5f), new Vector3(0.5f, 0.05f, 0.5f), Mat.Glow(new Color(1f, 0.7f, 0.3f), 2f), default, "Shock");
            ring.AddComponent<Debris>().InitFlash(0.3f, radius * 2f, true);
        }
    }

    public class Debris : MonoBehaviour
    {
        Vector3 vel;
        float gravity, life, maxLife;
        bool flash, flat;
        float grow;
        Vector3 baseScale;

        public void Init(Vector3 v, float g, float l) { vel = v; gravity = g; life = maxLife = l; baseScale = transform.localScale; }
        public void InitFlash(float l, float g, bool flatRing = false) { flash = true; flat = flatRing; life = maxLife = l; grow = g; baseScale = transform.localScale; }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0) { Destroy(gameObject); return; }
            float k = life / maxLife;
            if (flash)
            {
                float s = Mathf.Lerp(grow, 1f, k);
                transform.localScale = flat ? new Vector3(baseScale.x * s * 2, baseScale.y, baseScale.z * s * 2) : baseScale * s;
                return;
            }
            vel.y -= gravity * dt;
            transform.position += vel * dt;
            transform.Rotate(300 * dt, 200 * dt, 0);
            transform.localScale = baseScale * Mathf.Clamp01(k * 1.5f);
        }
    }
}

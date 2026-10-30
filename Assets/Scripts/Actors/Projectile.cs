using UnityEngine;

namespace Mostervanoid
{
    public enum HitResult { Hit, Deflected, Ignored }
    public enum DamageSource { Shot, Special, Pound, WallImpact }
    public enum ShotShape { Ball, Wave, Bomb, Wall }

    public class Projectile : MonoBehaviour
    {
        public static int PlayerShots;
        public Vector2 Velocity => vel;

        Vector2 pos, vel;
        Vector2 box;            // != zero: colisão por caixa (muros/ondas altas)
        float radius, life, gravity;
        int dmg;
        bool special, fromPlayer, deflected, countsShot, ignoreWalls;
        Color color;

        public static Projectile Fire(Vector2 pos, Vector2 vel, int dmg, bool fromPlayer, bool special, float radius, Color color,
            float life = 1.4f, float gravity = 0f, ShotShape shape = ShotShape.Ball, bool countsShot = false, bool ignoreWalls = false, Vector2? box = null)
        {
            var gm = GameManager.I;
            var go = new GameObject(fromPlayer ? "PlayerShot" : "EnemyShot");
            go.transform.SetParent(gm.LevelRoot, false);
            go.transform.position = pos;
            var p = go.AddComponent<Projectile>();
            p.pos = pos; p.vel = vel; p.dmg = dmg; p.fromPlayer = fromPlayer; p.special = special;
            p.radius = radius; p.life = life; p.gravity = gravity; p.color = color; p.ignoreWalls = ignoreWalls;
            p.countsShot = countsShot;
            p.box = box ?? Vector2.zero;
            if (countsShot) PlayerShots++;

            var glow = Mat.Glow(color, special ? 3f : 2f);
            switch (shape)
            {
                case ShotShape.Ball:
                    p.BuildEnergyBall(go.transform, color, radius, special);
                    break;
                case ShotShape.Wave:
                    Prim.Make(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(radius * 1.6f, radius * 2.2f, 1.2f), glow, new Vector3(0, 0, 0));
                    Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(0, radius * 0.9f, 0), new Vector3(radius * 1.8f, radius, 1.4f), Mat.Glow(Color.white, 2.5f));
                    AddTrail(go, color, radius * 2.2f, 0.12f);
                    break;
                case ShotShape.Wall:   // muro de gelo/espuma deslizando pelo chão
                    var bh = p.box;
                    Prim.Make(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(bh.x * 2f, bh.y * 2f, 1.8f), Mat.Color(Color.Lerp(color, Color.white, 0.5f), 0.95f, 0.1f));
                    Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(-Mathf.Sign(vel.x) * bh.x * 0.2f, 0, -0.5f), new Vector3(bh.x * 1.2f, bh.y * 1.7f, 0.9f), Mat.Glow(color, 2.2f));
                    for (int i = 0; i < 4; i++)
                        Prim.MakeMesh(Prim.Cone, go.transform, new Vector3((i - 1.5f) * bh.x * 0.5f, bh.y - 0.05f, 0), new Vector3(0.35f, 0.6f, 0.35f), Mat.Glow(Color.Lerp(color, Color.white, 0.7f), 1.5f), default, "Shard");
                    AddTrail(go, color, bh.y * 1.6f, 0.15f);
                    break;
                case ShotShape.Bomb:
                    Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * radius * 2f, Mat.Color(new Color(0.15f, 0.15f, 0.18f), 0.6f, 0.6f));
                    Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(0, radius, -radius * 0.4f), Vector3.one * radius * 0.6f, Mat.Glow(color, 4f));
                    break;
            }
            return p;
        }

        // ------------------------------------------------ visual de energia

        Transform shell, orbit;
        float pulse;

        /// <summary>Esfera de energia: núcleo branco HDR, casca colorida pulsante, faíscas orbitando, rastro e luz.</summary>
        void BuildEnergyBall(Transform t, Color c, float r, bool big)
        {
            Prim.Make(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * r * 1.15f, Mat.Glow(Color.Lerp(c, Color.white, 0.75f), big ? 6f : 4f), default, "Core");
            shell = Prim.Make(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * r * 2f, Mat.Glow(c, big ? 3f : 2.2f), default, "Shell").transform;
            orbit = Prim.Pivot(t, Vector3.zero, "Orbit");
            int n = big ? 4 : 2;
            var spark = Mat.Glow(Color.Lerp(c, Color.white, 0.5f), 5f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Prim.Make(PrimitiveType.Cube, orbit, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r * 1.5f, Vector3.one * r * 0.35f, spark, new Vector3(45, 45, 0), "Spark");
            }
            if (big)
                for (int i = 0; i < 3; i++)   // anéis de plasma
                    Prim.Make(PrimitiveType.Cylinder, orbit, Vector3.zero, new Vector3(r * 3f, 0.01f, r * 3f), Mat.Glow(c, 2.5f), new Vector3(90, i * 60, 0), "Ring");
            AddTrail(t.gameObject, c, r * (big ? 2.2f : 1.7f), big ? 0.22f : 0.12f);
            if (big || fromPlayer)
            {
                var l = t.gameObject.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = big ? 4f : 2.2f;
                l.intensity = big ? 4f : 2f;
                l.shadows = LightShadows.None;
            }
        }

        static void AddTrail(GameObject go, Color c, float width, float time)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = Mat.Trail;
            tr.time = time;
            tr.widthMultiplier = width;
            tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            tr.minVertexDistance = 0.05f;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.6f), 0), new GradientColorKey(c, 0.4f), new GradientColorKey(c, 1) },
                new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0, 1) });
            tr.colorGradient = g;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (shell != null)
            {
                pulse += dt * 30f;
                shell.localScale = Vector3.one * radius * 2f * (1f + Mathf.Sin(pulse) * 0.15f);
                orbit.Rotate(dt * 200f, dt * 300f, dt * 900f * -Mathf.Sign(vel.x));
            }
            var gm = GameManager.I;
            if (gm == null || gm.Grid == null) { Destroy(gameObject); return; }

            vel.y -= gravity * dt;
            pos += vel * dt;
            transform.position = new Vector3(pos.x, pos.y, 0);
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (pos.y < -4f) { Destroy(gameObject); return; }

            if (!ignoreWalls && !deflected && gm.Grid.IsSolid(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y)))
            {
                Fx.Sparks(pos, color, 5);
                Destroy(gameObject);
                return;
            }
            if (deflected) return;

            if (fromPlayer)
            {
                var all = Enemy.All;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    var e = all[i];
                    if (e == null || !e.Alive || !e.Active) continue;
                    if (!TileCollision.CircleAabb(pos, radius, e.pos, e.half)) continue;
                    var r = e.TakeHit(dmg, special, Mathf.Sign(vel.x), special ? DamageSource.Special : DamageSource.Shot);
                    if (r == HitResult.Hit) { Fx.Sparks(pos, color, 6); Destroy(gameObject); return; }
                    if (r == HitResult.Deflected)
                    {
                        Sfx.Play(Sfx.Tink);
                        deflected = true;
                        vel = new Vector2(-vel.x * 0.4f, 10f);
                        gravity = 35f;
                        life = 0.6f;
                        return;
                    }
                }
            }
            else
            {
                var p = gm.Player;
                bool touch = p != null && (box != Vector2.zero ? TileCollision.AabbOverlap(pos, box, p.Pos, p.Half) : TileCollision.CircleAabb(pos, radius, p.Pos, p.Half));
                if (p != null && p.CanBeHit && !p.ProjectileImmune && touch)
                {
                    p.Hurt(dmg, pos.x);
                    Destroy(gameObject);
                }
            }
        }

        void OnDestroy()
        {
            if (countsShot) PlayerShots = Mathf.Max(0, PlayerShots - 1);
        }
    }
}

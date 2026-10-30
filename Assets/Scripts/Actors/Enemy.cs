using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Base dos inimigos: vida, dano de contato, piscar ao levar dano, ativação por distância.</summary>
    public abstract class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public Vector2 pos;
        public Vector2 half = new Vector2(0.4f, 0.4f);
        public int hp = 2, maxHp = 2;
        public int contactDamage = 3;
        public Color color = Color.gray;
        public bool Alive { get; private set; } = true;
        public bool Active { get; protected set; }
        protected float activationRange = 13f;

        protected Transform model;
        Renderer[] renderers;
        float flashTimer;
        protected float invuln;          // i-frames (chefes)
        protected float invulnOnHit;

        protected LevelGrid G => GameManager.I.Grid;
        protected PlayerController P => GameManager.I.Player;

        public static T Spawn<T>(Transform parent, Vector2 p) where T : Enemy
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<T>();
            e.pos = p;
            e.model = Prim.Pivot(go.transform, Vector3.zero, "Model");
            e.Setup();
            // as fábricas usam "pé" na base da célula: sobe meia altura
            if (e.SpawnsOnFeet) e.pos.y += e.half.y;
            e.maxHp = e.hp;
            e.renderers = go.GetComponentsInChildren<Renderer>(true);
            go.transform.position = e.pos;
            return e;
        }

        protected virtual bool SpawnsOnFeet => true;
        protected abstract void Setup();
        protected abstract void Tick(float dt);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        protected void RefreshRenderers() => renderers = GetComponentsInChildren<Renderer>(true);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !Alive) return;
            var p = P;
            if (p == null) return;

            if (!Active)
            {
                var d = p.Pos - pos;
                if (Mathf.Abs(d.x) < activationRange && Mathf.Abs(d.y) < activationRange * 0.7f) Active = true;
                else return;
            }

            if (invuln > 0) invuln -= dt;
            Tick(dt);
            transform.position = new Vector3(pos.x, pos.y, 0);

            if (pos.y < -4f) { Alive = false; Destroy(gameObject); return; }

            if (p.CanBeHit && TileCollision.AabbOverlap(pos, half * 0.9f, p.Pos, p.Half))
                OnTouchPlayer(p);

            if (flashTimer > 0)
            {
                flashTimer -= dt;
                bool vis = flashTimer <= 0 || Mathf.FloorToInt(flashTimer * 30) % 2 == 0;
                SetVisible(vis);
            }
            else if (invuln > 0)
                SetVisible(Mathf.FloorToInt(invuln * 20) % 2 == 0);
            else SetVisible(true);
        }

        protected virtual void OnTouchPlayer(PlayerController p) => p.Hurt(contactDamage, pos.x);

        protected void SetVisible(bool v)
        {
            if (renderers == null) return;
            foreach (var r in renderers) if (r != null) r.enabled = v;
        }

        /// <summary>Recebe um ataque. special = tiro especial / investidas (forma 0, 4, 5, 7).</summary>
        public virtual HitResult TakeHit(int dmg, bool special, float dir, DamageSource src = DamageSource.Shot)
        {
            if (!Alive) return HitResult.Ignored;
            if (invuln > 0) return HitResult.Ignored;
            if (GameManager.I.Cheats.oneHitKill) dmg = 999;
            hp -= dmg;
            flashTimer = 0.12f;
            invuln = invulnOnHit;
            OnDamaged(dmg, special, dir);
            if (hp <= 0) Die();
            return HitResult.Hit;
        }

        protected virtual void OnDamaged(int dmg, bool special, float dir) { }

        public virtual void Die()
        {
            if (!Alive) return;
            Alive = false;
            Fx.Explode(pos, color);
            GameManager.I.OnEnemyKilled(this);
            Destroy(gameObject);
        }

        // ------------------------------------------------ utilitários de movimento

        protected bool Grounded() => TileCollision.Grounded(G, pos, half);

        protected bool GroundAhead(float dir)
        {
            int cx = Mathf.FloorToInt(pos.x + dir * (half.x + 0.1f));
            int cy = Mathf.FloorToInt(pos.y - half.y - 0.1f);
            return G.IsSolid(cx, cy) || G.IsOneWay(cx, cy);
        }

        protected bool HazardAhead(float dir)
        {
            int cx = Mathf.FloorToInt(pos.x + dir * (half.x + 0.1f));
            int cy = Mathf.FloorToInt(pos.y - half.y + 0.1f);
            return G.Get(cx, cy) == Cell.Spike;
        }

        protected void Shoot(Vector2 from, Vector2 dir, float speed = 6f, int dmg = 3, Color? color = null)
        {
            Projectile.Fire(from, dir.normalized * speed, dmg, false, false, 0.16f, color ?? new Color(1f, 0.35f, 0.2f), 3f);
        }
    }

    // ===================================================================== Andador
    /// <summary>Garrafinha-robô que patrulha e vira nas bordas/paredes.</summary>
    public class EnemyWalker : Enemy
    {
        float dir = -1, vy;
        Transform wheelL, wheelR;

        protected override void Setup()
        {
            half = new Vector2(0.4f, 0.4f);
            hp = 2; contactDamage = 3;
            color = new Color(0.2f, 0.7f, 0.3f);
            var body = Mat.Color(color, 0.6f, 0.2f);
            Prim.Make(PrimitiveType.Cube, model, new Vector3(0, 0.05f, 0), new Vector3(0.75f, 0.55f, 0.7f), body);
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0, 0.45f, 0), new Vector3(0.3f, 0.15f, 0.3f), Mat.Color(new Color(0.8f, 0.8f, 0.85f), 0.8f, 0.8f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0, 0.62f, 0), Vector3.one * 0.16f, Mat.Glow(new Color(1, 0.2f, 0.2f), 2f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.18f, 0.12f, -0.33f), new Vector3(0.18f, 0.22f, 0.1f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.18f, 0.12f, -0.33f), new Vector3(0.18f, 0.22f, 0.1f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.2f, 0.12f, -0.37f), new Vector3(0.08f, 0.1f, 0.05f), Mat.Color(Color.black));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.16f, 0.12f, -0.37f), new Vector3(0.08f, 0.1f, 0.05f), Mat.Color(Color.black));
            var tire = Mat.Color(new Color(0.1f, 0.1f, 0.1f), 0.2f);
            wheelL = Prim.Make(PrimitiveType.Cylinder, model, new Vector3(-0.25f, -0.25f, -0.3f), new Vector3(0.3f, 0.06f, 0.3f), tire, new Vector3(90, 0, 0)).transform;
            wheelR = Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0.25f, -0.25f, -0.3f), new Vector3(0.3f, 0.06f, 0.3f), tire, new Vector3(90, 0, 0)).transform;
        }

        protected override void Tick(float dt)
        {
            float speed = 1.8f;
            if (TileCollision.MoveX(G, ref pos, half, dir * speed * dt) || (Grounded() && (!GroundAhead(dir) || HazardAhead(dir))))
                dir = -dir;
            vy = Mathf.Max(vy - 40f * dt, -18f);
            if (TileCollision.MoveY(G, ref pos, half, vy * dt, false, out _)) vy = 0;
            model.localRotation = Quaternion.Euler(0, dir > 0 ? -25 : 25, 0);
            wheelL.Rotate(0, -dir * 400 * dt, 0, Space.Self);
            wheelR.Rotate(0, -dir * 400 * dt, 0, Space.Self);
        }
    }

    // ===================================================================== Voador
    /// <summary>Mosca gaseificada: persegue o jogador ondulando.</summary>
    public class EnemyFlyer : Enemy
    {
        Transform wingL, wingR;
        float t;
        Vector2 home;

        protected override bool SpawnsOnFeet => false;

        protected override void Setup()
        {
            half = new Vector2(0.35f, 0.35f);
            hp = 1; contactDamage = 3;
            activationRange = 11f;
            color = new Color(0.9f, 0.5f, 0.1f);
            Prim.Make(PrimitiveType.Sphere, model, Vector3.zero, new Vector3(0.7f, 0.6f, 0.6f), Mat.Color(color, 0.7f, 0.2f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.12f, 0.08f, -0.25f), new Vector3(0.2f, 0.24f, 0.12f), Mat.Glow(new Color(1, 0.1f, 0.1f), 1.5f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.12f, 0.08f, -0.25f), new Vector3(0.2f, 0.24f, 0.12f), Mat.Glow(new Color(1, 0.1f, 0.1f), 1.5f));
            var wing = Mat.Color(new Color(0.85f, 0.95f, 1f), 0.9f, 0.1f);
            wingL = Prim.Pivot(model, new Vector3(-0.15f, 0.25f, 0.1f));
            wingR = Prim.Pivot(model, new Vector3(0.15f, 0.25f, 0.1f));
            Prim.Make(PrimitiveType.Sphere, wingL, new Vector3(-0.3f, 0.05f, 0), new Vector3(0.6f, 0.08f, 0.35f), wing);
            Prim.Make(PrimitiveType.Sphere, wingR, new Vector3(0.3f, 0.05f, 0), new Vector3(0.6f, 0.08f, 0.35f), wing);
        }

        const float Leash = 7f;      // distância máxima do ponto de nascimento

        protected override void Tick(float dt)
        {
            if (t == 0) home = pos;
            t += dt;
            // persegue só enquanto o jogador está perto do "território"; senão volta para casa
            bool chase = Vector2.Distance(P.Pos, home) < Leash + 3f;
            var target = chase ? P.Pos + new Vector2(0, 1.2f) : home;
            var off = target - home;
            if (off.magnitude > Leash) target = home + off.normalized * Leash;
            var d = target - pos;
            Vector2 v = d.magnitude > 0.1f ? d.normalized * (chase ? 2.6f : 2f) : Vector2.zero;
            v.y += Mathf.Sin(t * 4f) * 2.2f;
            // colide com o cenário (não atravessa paredes nem portões de chefe)
            TileCollision.MoveX(G, ref pos, half, v.x * dt);
            TileCollision.MoveY(G, ref pos, half, v.y * dt, true, out _);
            float flap = Mathf.Sin(t * 40f) * 35f;
            wingL.localRotation = Quaternion.Euler(0, 0, flap);
            wingR.localRotation = Quaternion.Euler(0, 0, -flap);
            model.localRotation = Quaternion.Euler(0, (P.Pos.x - pos.x) > 0 ? -25 : 25, 0);
        }

        public float DistanceFromHome => Vector2.Distance(pos, home);
    }

    // ===================================================================== Blindado
    /// <summary>Capacete blindado: reflete tiros normais. Só morre com tiro especial (ou investidas especiais).</summary>
    public class EnemyArmored : Enemy
    {
        float dir = -1, vy, shootTimer = 2f, peek;
        Transform helmet;

        protected override void Setup()
        {
            half = new Vector2(0.48f, 0.45f);
            hp = 5; contactDamage = 4;
            color = new Color(0.55f, 0.58f, 0.65f);
            var metal = Mat.Color(color, 0.9f, 0.95f);
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0, -0.25f, 0), new Vector3(0.8f, 0.2f, 0.8f), Mat.Color(new Color(0.15f, 0.15f, 0.18f)));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0, -0.05f, -0.2f), new Vector3(0.5f, 0.4f, 0.4f), Mat.Color(new Color(0.95f, 0.85f, 0.2f)));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.1f, 0f, -0.39f), Vector3.one * 0.1f, Mat.Glow(new Color(0.2f, 1f, 1f), 2f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.1f, 0f, -0.39f), Vector3.one * 0.1f, Mat.Glow(new Color(0.2f, 1f, 1f), 2f));
            helmet = Prim.Pivot(model, new Vector3(0, -0.05f, 0), "Helmet");
            Prim.Make(PrimitiveType.Sphere, helmet, new Vector3(0, 0.12f, 0), new Vector3(1.0f, 0.75f, 0.95f), metal);
            Prim.Make(PrimitiveType.Cylinder, helmet, new Vector3(0, -0.05f, 0), new Vector3(1.1f, 0.03f, 1.05f), metal);
            Prim.Make(PrimitiveType.Cube, helmet, new Vector3(0, 0.48f, 0), new Vector3(0.12f, 0.12f, 0.6f), Mat.Color(new Color(0.85f, 0.15f, 0.1f)));
        }

        public override HitResult TakeHit(int dmg, bool special, float dir, DamageSource src = DamageSource.Shot)
        {
            if (!special && !GameManager.I.Cheats.oneHitKill) return HitResult.Deflected;
            return base.TakeHit(dmg, special, dir, src);
        }

        protected override void Tick(float dt)
        {
            float toPlayer = Mathf.Sign(P.Pos.x - pos.x);
            shootTimer -= dt;
            if (shootTimer < 0.6f)
            {
                peek = Mathf.MoveTowards(peek, 1, dt * 5);
                if (shootTimer <= 0)
                {
                    shootTimer = 2.8f;
                    var from = pos + new Vector2(toPlayer * 0.5f, 0);
                    Shoot(from, new Vector2(toPlayer, 0));
                    Shoot(from, new Vector2(toPlayer, 0.35f));
                }
            }
            else
            {
                peek = Mathf.MoveTowards(peek, 0, dt * 5);
                dir = toPlayer;
                if (Mathf.Abs(P.Pos.x - pos.x) > 1.0f && GroundAhead(dir) && !HazardAhead(dir))
                    TileCollision.MoveX(G, ref pos, half, dir * 1.0f * dt);
            }
            vy = Mathf.Max(vy - 40f * dt, -18f);
            if (TileCollision.MoveY(G, ref pos, half, vy * dt, false, out _)) vy = 0;
            helmet.localPosition = new Vector3(0, -0.05f + peek * 0.28f, 0);
            model.localRotation = Quaternion.Euler(0, dir > 0 ? -20 : 20, 0);
        }
    }

    // ===================================================================== Torreta
    /// <summary>Torreta fixa que mira no jogador.</summary>
    public class EnemyTurret : Enemy
    {
        float timer = 1.5f;
        Transform barrel;

        protected override void Setup()
        {
            half = new Vector2(0.45f, 0.45f);
            hp = 3; contactDamage = 3;
            activationRange = 12f;
            color = new Color(0.7f, 0.2f, 0.25f);
            Prim.Make(PrimitiveType.Cube, model, new Vector3(0, -0.2f, 0), new Vector3(0.9f, 0.5f, 0.9f), Mat.Color(new Color(0.3f, 0.3f, 0.35f), 0.6f, 0.6f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0, 0.1f, 0), Vector3.one * 0.7f, Mat.Color(color, 0.6f, 0.3f));
            barrel = Prim.Pivot(model, new Vector3(0, 0.1f, -0.1f), "Barrel");
            Prim.Make(PrimitiveType.Cylinder, barrel, new Vector3(0.4f, 0, 0), new Vector3(0.18f, 0.3f, 0.18f), Mat.Color(new Color(0.2f, 0.2f, 0.22f), 0.6f, 0.8f), new Vector3(0, 0, 90));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0, 0.3f, -0.3f), Vector3.one * 0.15f, Mat.Glow(new Color(1, 0.8f, 0.2f), 2f));
        }

        protected override void Tick(float dt)
        {
            var d = (P.Pos - pos);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            barrel.localRotation = Quaternion.Euler(0, 0, ang);
            timer -= dt;
            if (timer <= 0)
            {
                timer = 2.2f;
                if (d.magnitude < 12f)
                    Shoot(pos + new Vector2(0, 0.1f) + d.normalized * 0.7f, d, 6.5f);
            }
        }
    }
}

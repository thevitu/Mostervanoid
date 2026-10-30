using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Jarra de Limonada (Fábrica de Vidro — explora a Forma 7, IMPACTO NA PAREDE):
    /// lança um copo de vidro que emborca sobre a lata e a prende — só o impacto na parede quebra o vidro
    /// (em 4s o copo aperta: dano e solta). Também rola fatias de limão pelo chão (pule) e arremessa gelo.
    /// Toma dano de tiros e (mais) do impacto na parede. Ritmo: no mínimo 5s entre um copo e outro (recarga 3s).
    /// </summary>
    public class BossGlassJar : Boss
    {
        public enum S { Intro, Idle, Throw, Lemons, Ice, Charge }
        public S State { get; private set; } = S.Intro;
        public bool PlayerTrapped => cage.Count > 0 && CageIntact();

        static readonly Color Lemon = new Color(1f, 0.95f, 0.3f);
        const float TrapCadence = 5f, SqueezeTime = 4f;

        readonly List<Vector2Int> cage = new List<Vector2Int>();
        float lastTrap = -99f, trapTimer;
        int lemons, ice, dir = -1;
        Vector2 vel;
        bool grounded;
        Transform glassShot;
        Vector2 glassPos, glassVel;
        Transform jar;

        protected override void Setup()
        {
            displayName = "JARRA DE LIMONADA";
            half = new Vector2(1.3f, 1.7f);
            hp = 32; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(0.8f, 0.95f, 0.6f);
            var glass = Mat.Color(new Color(0.8f, 0.95f, 0.95f), 0.97f, 0.1f);
            var lemonade = Mat.Color(new Color(1f, 0.95f, 0.45f), 0.9f);
            jar = Prim.Pivot(model, Vector3.zero, "Jar");
            Prim.Make(PrimitiveType.Cylinder, jar, new Vector3(0, -0.2f, 0), new Vector3(2.4f, 1.5f, 2.4f), glass);
            Prim.Make(PrimitiveType.Cylinder, jar, new Vector3(0, -0.45f, 0), new Vector3(2.2f, 1.2f, 2.2f), lemonade);
            Prim.Make(PrimitiveType.Cylinder, jar, new Vector3(0, 1.35f, 0), new Vector3(2.6f, 0.08f, 2.6f), glass);
            // bico e alça
            Prim.Make(PrimitiveType.Cube, jar, new Vector3(1.25f, 1.25f, 0), new Vector3(0.6f, 0.15f, 0.6f), glass, new Vector3(0, 0, -20));
            Prim.Make(PrimitiveType.Cylinder, jar, new Vector3(-1.45f, 0.2f, 0), new Vector3(0.9f, 0.12f, 0.25f), glass, new Vector3(90, 0, 0));
            // rodelas de limão boiando e cubos de gelo
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cylinder, jar, new Vector3(-0.6f + i * 0.6f, 0.6f, -0.9f), new Vector3(0.7f, 0.04f, 0.7f), Mat.Color(Lemon, 0.6f), new Vector3(70, 0, i * 20));
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cube, jar, new Vector3(-0.5f + i * 0.5f, 0.2f - i * 0.3f, -0.5f), Vector3.one * 0.4f, Mat.Color(new Color(0.85f, 0.95f, 1f), 0.95f), new Vector3(20, 30 * i, 10));
            // rosto
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, jar, new Vector3(-0.38f, 0.0f, -1.18f), new Vector3(0.32f, 0.32f, 0.1f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, jar, new Vector3(0.38f, 0.0f, -1.18f), new Vector3(0.32f, 0.32f, 0.1f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, jar, new Vector3(-0.35f, -0.02f, -1.23f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Sphere, jar, new Vector3(0.35f, -0.02f, -1.23f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Cube, jar, new Vector3(0, -0.35f, -1.2f), new Vector3(0.5f, 0.07f, 0.04f), black);
        }

        public override HitResult TakeHit(int dmg, bool special, float dir, DamageSource src = DamageSource.Shot)
        {
            if (src == DamageSource.WallImpact) dmg += 3;   // investida na jarra dói mais
            return base.TakeHit(dmg, special, dir, src);
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0) { MeasureRoom(); stateTimer = 1.2f; }
            stateTimer -= dt;

            vel.y = Mathf.Max(vel.y - 38f * dt, -22f);
            bool was = grounded;
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt))
            {
                if (State == S.Charge) { GameManager.I.Shake(0.3f); Sfx.Play(Sfx.Pound); Enter(S.Idle, 1.0f); }
                vel.x = 0;
            }
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, true, out bool landed)) vel.y = 0;
            grounded = landed || TileCollision.Grounded(G, pos, half, true);

            UpdateGlassShot(dt);
            UpdateCage(dt);

            switch (State)
            {
                case S.Intro:
                    if (stateTimer <= 0 && grounded) Enter(S.Idle, 0.8f);
                    break;
                case S.Idle:
                    vel.x = 0;
                    FacePlayer();
                    if (stateTimer <= 0)
                    {
                        float r = Random.value;
                        bool canTrap = Time.time - lastTrap >= TrapCadence && cage.Count == 0 && glassShot == null;
                        if (canTrap && r < 0.5f) Enter(S.Throw, 0.6f);
                        else if (r < 0.7f) { lemons = Enraged ? 3 : 2; Enter(S.Lemons, 0.4f); }
                        else if (r < 0.88f) { ice = Enraged ? 5 : 3; Enter(S.Ice, 0.4f); }
                        else { Enter(S.Charge, 3f); vel.x = dir * (Enraged ? 9f : 7f); Sfx.Play(Sfx.Dash); }
                    }
                    break;
                case S.Throw:    // lança o copo em arco até onde o jogador está
                    jar.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30f) * 8f);
                    if (stateTimer <= 0)
                    {
                        jar.localRotation = Quaternion.identity;
                        lastTrap = Time.time;
                        glassPos = pos + new Vector2(0, 2f);
                        float t = 1.0f;
                        glassVel = new Vector2((P.Pos.x - glassPos.x) / t, 0.5f * 30f * t + (P.Pos.y - glassPos.y) / t);
                        glassShot = Prim.Pivot(transform.parent, glassPos, "ThrownGlass");
                        var gm = Mat.Color(new Color(0.8f, 0.95f, 1f), 0.97f, 0.1f);
                        Prim.Make(PrimitiveType.Cylinder, glassShot, Vector3.zero, new Vector3(1.2f, 0.8f, 1.2f), gm, new Vector3(180, 0, 0));
                        Sfx.Play(Sfx.Jump);
                        Enter(S.Idle, 1.2f);
                    }
                    break;
                case S.Lemons:   // fatias de limão rolando pelo chão (pule)
                    if (stateTimer <= 0)
                    {
                        Projectile.Fire(new Vector2(pos.x + dir * (half.x + 0.4f), floorY + 0.38f), new Vector2(dir * (Enraged ? 8f : 6.5f), 0), 3, false, false, 0.38f, Lemon, 5f, 0, ShotShape.Wave);
                        lemons--;
                        stateTimer = 0.9f;
                        if (lemons <= 0) Enter(S.Idle, 0.8f);
                    }
                    break;
                case S.Ice:      // cubos de gelo em arco
                    if (stateTimer <= 0)
                    {
                        var from = pos + new Vector2(dir * 1.2f, 1.6f);
                        Projectile.Fire(from, new Vector2((P.Pos.x - from.x) * 0.9f + Random.Range(-1.5f, 1.5f), 10f), 3, false, false, 0.25f, new Color(0.7f, 0.9f, 1f), 3f, 18f);
                        ice--;
                        stateTimer = 0.35f;
                        if (ice <= 0) Enter(S.Idle, 0.9f);
                    }
                    break;
                case S.Charge:   // escorrega pelo chão até a parede
                    if (stateTimer <= 0) Enter(S.Idle, 0.8f);
                    break;
            }

            model.localRotation = Quaternion.Euler(0, dir > 0 ? -20 : 20, 0);
        }

        void FacePlayer()
        {
            float d = P.Pos.x - pos.x;
            if (Mathf.Abs(d) > 0.3f) dir = d > 0 ? 1 : -1;
        }

        // ---------------------------------------------------------- copo / armadilha

        void UpdateGlassShot(float dt)
        {
            if (glassShot == null) return;
            glassVel.y -= 30f * dt;
            glassPos += glassVel * dt;
            glassShot.position = glassPos;
            glassShot.Rotate(0, 0, 360 * dt);
            bool hitPlayer = P.CanBeHit && TileCollision.CircleAabb(glassPos, 0.7f, P.Pos, P.Half);
            bool hitFloor = glassVel.y < 0 && glassPos.y <= floorY + 0.4f;
            if (hitPlayer) { Destroy(glassShot.gameObject); glassShot = null; Trap(); }
            else if (hitFloor || glassPos.y < floorY - 2f)
            {
                Fx.Sparks(glassPos, Color.white, 10);
                Sfx.Play(Sfx.Tink);
                Destroy(glassShot.gameObject);
                glassShot = null;
            }
        }

        /// <summary>Emborca o copo: gaiola de vidro (células quebráveis) em volta do jogador.</summary>
        void Trap()
        {
            int cx = Mathf.FloorToInt(P.Pos.x);
            int fy = Mathf.FloorToInt(P.Pos.y - P.Half.y + 0.05f);
            if (!P.IsGrounded || !P.TrySnapToCell(cx)) { P.Hurt(3, glassPos.x); return; }   // no ar o copo só quebra na cabeça
            Sfx.Play(Sfx.Tink);
            GameManager.I.Shake(0.2f);
            var cells = new[]
            {
                new Vector2Int(cx - 1, fy), new Vector2Int(cx - 1, fy + 1),
                new Vector2Int(cx + 1, fy), new Vector2Int(cx + 1, fy + 1),
                new Vector2Int(cx - 1, fy + 2), new Vector2Int(cx, fy + 2), new Vector2Int(cx + 1, fy + 2),
            };
            cage.Clear();
            foreach (var c in cells)
            {
                if (G.Get(c.x, c.y) != Cell.Empty) continue;
                var vis = LevelBuilder.BlockVisual(transform.parent, c.x, c.y, ProcTex.Ice, "GlassCage");
                vis.GetComponent<Renderer>().sharedMaterial = Mat.Color(new Color(0.75f, 0.95f, 1f), 0.97f, 0.15f);
                G.Set(c.x, c.y, Cell.Breakable, vis);
                cage.Add(c);
            }
            trapTimer = SqueezeTime;
            GameManager.I.Toast("Preso no copo! IMPACTO NA PAREDE (L/3) para quebrar o vidro!", 2.5f);
        }

        bool CageIntact()
        {
            foreach (var c in cage) if (G.Get(c.x, c.y) == Cell.Breakable) return true;
            return false;
        }

        void UpdateCage(float dt)
        {
            if (cage.Count == 0) return;
            if (!CageIntact()) { cage.Clear(); return; }   // quebrou com o impacto na parede
            trapTimer -= dt;
            if (trapTimer <= 0)
            {
                // o copo aperta: dano e solta
                P.Hurt(4, P.Pos.x + 0.1f);
                ClearCage();
            }
        }

        void ClearCage()
        {
            foreach (var c in cage)
                if (G.Get(c.x, c.y) == Cell.Breakable)
                {
                    Fx.Sparks(LevelGrid.CellCenter(c.x, c.y), Color.white, 4);
                    G.Set(c.x, c.y, Cell.Empty);
                }
            cage.Clear();
        }

        public override void Die()
        {
            ClearCage();
            if (glassShot != null) Destroy(glassShot.gameObject);
            base.Die();
        }

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

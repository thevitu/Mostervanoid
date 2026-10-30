using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Base dos chefes: barra de vida, i-frames, limites da sala e aviso ao GameManager.</summary>
    public abstract class Boss : Enemy
    {
        public string displayName = "CHEFE";
        protected float floorY, ceilY, roomL, roomR;
        protected float stateTimer;
        public float StateTime => stateTimer;
        protected bool Enraged => hp <= maxHp / 2;

        protected override bool SpawnsOnFeet => false;

        protected void MeasureRoom()
        {
            int cx = Mathf.FloorToInt(pos.x), cy = Mathf.FloorToInt(pos.y);
            int y = cy; while (y > 0 && !G.IsSolid(cx, y - 1)) y--; floorY = y;
            y = cy; while (y < G.H - 1 && !G.IsSolid(cx, y + 1)) y++; ceilY = y + 1;
            int row = Mathf.FloorToInt(floorY) + 1;
            int x = cx; while (x > 0 && !G.IsSolid(x - 1, row)) x--; roomL = x;
            x = cx; while (x < G.W - 1 && !G.IsSolid(x + 1, row)) x++; roomR = x + 1;
        }

        protected override void OnDamaged(int dmg, bool special, float dir) => Sfx.Play(Sfx.BossHit);

        public override void Die()
        {
            if (!Alive) return;
            for (int i = 0; i < 4; i++)
                Fx.Explode(pos + Random.insideUnitCircle * 1.2f, color, 12, 9f, 0.3f);
            base.Die();
            GameManager.I.OnBossDefeated(this);
        }
    }

    // ===================================================================== Galão d'Água
    /// <summary>Galão d'Água: galão de 20 L que salta pela sala, espirra ondas d'água pelo chão e atira gotas em leque.</summary>
    public class BossWaterJug : Boss
    {
        enum S { Intro, Idle, Hop, Spread }
        S state = S.Intro;
        Vector2 vel;
        int shotsLeft;
        bool grounded;
        Transform body;
        static readonly Color Water = new Color(0.25f, 0.65f, 1f);

        protected override void Setup()
        {
            displayName = "GALÃO D'ÁGUA";
            half = new Vector2(1.0f, 1.3f);
            hp = 28; contactDamage = 4;
            invulnOnHit = 0.2f;
            activationRange = 999f;
            color = new Color(0.45f, 0.75f, 1f);
            var plastic = Mat.Color(new Color(0.45f, 0.72f, 0.98f), 0.92f, 0.05f);
            var plasticDark = Mat.Color(new Color(0.30f, 0.55f, 0.90f), 0.9f, 0.05f);
            var capMat = Mat.Color(new Color(0.08f, 0.25f, 0.75f), 0.6f, 0.1f);
            var label = Mat.Color(Color.white, 0.4f);
            body = Prim.Pivot(model, new Vector3(0, -1.3f, 0), "Body");   // pivô na base: "respira" sem afundar
            // corpo com anéis e ombro arredondado
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 1.05f, 0), new Vector3(2.0f, 0.85f, 2.0f), plastic);
            foreach (float y in new[] { 0.45f, 1.0f, 1.55f })
                Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, y, 0), new Vector3(2.06f, 0.05f, 2.06f), plasticDark);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0, 1.85f, 0), new Vector3(2.0f, 0.95f, 2.0f), plastic);
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 2.38f, 0), new Vector3(0.6f, 0.14f, 0.6f), plastic);
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 2.55f, 0), new Vector3(0.72f, 0.1f, 0.72f), capMat);
            // água por dentro (nível) + bolhas
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 1.0f, 0), new Vector3(1.85f, 0.78f, 1.85f), Mat.Color(new Color(0.15f, 0.45f, 0.95f), 0.95f));
            for (int i = 0; i < 5; i++)
                Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.6f + i * 0.3f, 0.7f + (i % 2) * 0.5f, -0.97f), Vector3.one * (0.08f + i % 3 * 0.03f), Mat.Glow(new Color(0.8f, 0.95f, 1f), 1.2f));
            // rótulo com gota
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 0.75f, 0), new Vector3(2.04f, 0.2f, 2.04f), label);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0, 0.7f, -1.03f), new Vector3(0.2f, 0.2f, 0.06f), capMat);
            Prim.MakeMesh(Prim.Cone, body, new Vector3(0, 0.75f, -1.03f), new Vector3(0.2f, 0.18f, 0.06f), capMat, default, "Drop");
            // rosto zangado
            var white = Mat.Color(Color.white);
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.35f, 1.35f, -0.96f), new Vector3(0.35f, 0.3f, 0.15f), white);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0.35f, 1.35f, -0.96f), new Vector3(0.35f, 0.3f, 0.15f), white);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.3f, 1.32f, -1.03f), new Vector3(0.14f, 0.16f, 0.06f), black);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0.3f, 1.32f, -1.03f), new Vector3(0.14f, 0.16f, 0.06f), black);
            Prim.Make(PrimitiveType.Cube, body, new Vector3(-0.35f, 1.57f, -1.0f), new Vector3(0.4f, 0.08f, 0.05f), black, new Vector3(0, 0, -18));
            Prim.Make(PrimitiveType.Cube, body, new Vector3(0.35f, 1.57f, -1.0f), new Vector3(0.4f, 0.08f, 0.05f), black, new Vector3(0, 0, 18));
            Prim.Make(PrimitiveType.Cube, body, new Vector3(0, 1.12f, -1.0f), new Vector3(0.5f, 0.08f, 0.05f), black);
            // pés
            var feet = Mat.Color(new Color(0.15f, 0.15f, 0.18f));
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.55f, -1.2f, 0), new Vector3(0.6f, 0.3f, 0.7f), feet);
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.55f, -1.2f, 0), new Vector3(0.6f, 0.3f, 0.7f), feet);
        }

        protected override void Tick(float dt)
        {
            if (state == S.Intro && stateTimer == 0) { MeasureRoom(); stateTimer = 1.2f; }

            vel.y = Mathf.Max(vel.y - 38f * dt, -22f);
            bool wasGrounded = grounded;
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt)) vel.x = 0;
            bool hitY = TileCollision.MoveY(G, ref pos, half, vel.y * dt, true, out bool landed);
            if (hitY) vel.y = 0;
            grounded = landed || TileCollision.Grounded(G, pos, half, true);

            if (grounded && !wasGrounded && state != S.Intro)
            {
                vel.x = 0;
                Sfx.Play(Sfx.Pound);
                GameManager.I.Shake(0.3f);
                Fx.Dust(pos - new Vector2(0, half.y), 10);
                if (state == S.Hop)
                {
                    float wy = pos.y - half.y + 0.38f;
                    float sp = Enraged ? 8.5f : 7f;
                    Projectile.Fire(new Vector2(pos.x - half.x, wy), new Vector2(-sp, 0), 3, false, false, 0.38f, Water, 5f, 0, ShotShape.Wave);
                    Projectile.Fire(new Vector2(pos.x + half.x, wy), new Vector2(sp, 0), 3, false, false, 0.38f, Water, 5f, 0, ShotShape.Wave);
                    Enter(S.Idle, Enraged ? 0.5f : 0.8f);
                }
            }

            stateTimer -= dt;
            switch (state)
            {
                case S.Intro:
                    if (stateTimer <= 0 && grounded) Enter(S.Idle, 0.6f);
                    break;
                case S.Idle:
                    if (stateTimer <= 0)
                    {
                        if (Random.value < 0.55f) StartHop();
                        else { shotsLeft = Enraged ? 3 : 2; Enter(S.Spread, 0.35f); }
                    }
                    break;
                case S.Spread:
                    if (stateTimer <= 0)
                    {
                        var d = (P.Pos - pos).normalized;
                        float baseAng = Mathf.Atan2(d.y, d.x);
                        for (int i = -1; i <= 1; i++)
                        {
                            float a = baseAng + i * 0.3f;
                            Shoot(pos + new Vector2(0, 0.6f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Enraged ? 7.5f : 6f, 3, Water);
                        }
                        shotsLeft--;
                        if (shotsLeft > 0) stateTimer = 0.45f;
                        else Enter(S.Idle, 0.6f);
                    }
                    break;
            }

            // olha para o jogador e "respira"
            float face = Mathf.Sign(P.Pos.x - pos.x);
            model.localRotation = Quaternion.Euler(0, face > 0 ? -20 : 20, 0);
            float sq = grounded ? 1 + Mathf.Sin(Time.time * 6) * 0.03f : 1.05f;
            body.localScale = new Vector3(2 - sq, sq, 2 - sq);
        }

        void StartHop()
        {
            state = S.Hop;
            float air = 1.2f;
            vel.y = Enraged ? 17f : 15.5f;
            float target = Mathf.Clamp(P.Pos.x, roomL + half.x + 0.2f, roomR - half.x - 0.2f);
            vel.x = Mathf.Clamp((target - pos.x) / air * 1.4f, -8f, 8f);
            grounded = false;
        }

        void Enter(S s, float t) { state = s; stateTimer = t; }
    }
}

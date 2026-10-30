using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Bule de Chá (Caverna Efervescente — explora a Forma 5, IMPACTO NO CHÃO):
    /// a porcelana reflete tiros; só o tremor do impacto no chão perto dele racha o bule.
    /// Ataques: chá fervendo em arco, saltos com ondas de chá (pule) e jato de vapor vertical (saia de cima).
    /// Ritmo: o impacto tem recarga de 3s; o bule sempre dá janelas de ~3s+ entre os momentos de aproximação.
    /// </summary>
    public class BossTeapot : Boss
    {
        public enum S { Intro, Idle, Pour, Hop, Steam }
        public S State { get; private set; } = S.Intro;

        static readonly Color Tea = new Color(0.85f, 0.45f, 0.15f);
        Vector2 vel;
        bool grounded;
        int pours;
        float steamX;
        Transform body, lid, steamFx;
        int cracks;

        protected override void Setup()
        {
            displayName = "BULE DE CHÁ";
            half = new Vector2(1.5f, 1.2f);
            hp = 30; contactDamage = 4;
            invulnOnHit = 0.4f;
            activationRange = 999f;
            color = new Color(0.92f, 0.92f, 0.95f);
            var porcelain = Mat.Color(new Color(0.95f, 0.95f, 0.98f), 0.9f, 0.05f);
            var blue = Mat.Color(new Color(0.15f, 0.3f, 0.75f), 0.8f);
            body = Prim.Pivot(model, new Vector3(0, -1.2f, 0), "Body");
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0, 1.1f, 0), new Vector3(2.6f, 2.0f, 2.2f), porcelain);
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 0.15f, 0), new Vector3(1.6f, 0.15f, 1.6f), porcelain);
            foreach (float y in new[] { 0.55f, 1.65f })
                Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, y, 0), new Vector3(2.45f, 0.05f, 2.1f), blue);
            // bico e alça (bico aponta para +X local)
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(1.55f, 1.35f, 0), new Vector3(0.35f, 0.6f, 0.35f), porcelain, new Vector3(0, 0, -55));
            for (int i = 0; i < 6; i++)
            {
                float a = -60 + i * 30;
                Prim.Make(PrimitiveType.Cube, body, new Vector3(-1.35f, 1.1f, 0) + Quaternion.Euler(0, 0, a) * new Vector3(-0.55f, 0, 0), new Vector3(0.22f, 0.32f, 0.22f), porcelain, new Vector3(0, 0, a));
            }
            lid = Prim.Pivot(body, new Vector3(0, 2.05f, 0), "Lid");
            Prim.Make(PrimitiveType.Sphere, lid, Vector3.zero, new Vector3(1.3f, 0.45f, 1.3f), porcelain);
            Prim.Make(PrimitiveType.Sphere, lid, new Vector3(0, 0.3f, 0), Vector3.one * 0.3f, blue);
            // rosto (flores azuis nas bochechas)
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.38f, 1.25f, -1.05f), new Vector3(0.32f, 0.3f, 0.12f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0.38f, 1.25f, -1.05f), new Vector3(0.32f, 0.3f, 0.12f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.35f, 1.23f, -1.12f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0.35f, 1.23f, -1.12f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Cube, body, new Vector3(-0.38f, 1.47f, -1.08f), new Vector3(0.36f, 0.07f, 0.05f), black, new Vector3(0, 0, -15));
            Prim.Make(PrimitiveType.Cube, body, new Vector3(0.38f, 1.47f, -1.08f), new Vector3(0.36f, 0.07f, 0.05f), black, new Vector3(0, 0, 15));
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(-0.8f, 0.95f, -0.9f), new Vector3(0.3f, 0.3f, 0.08f), blue);
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0.8f, 0.95f, -0.9f), new Vector3(0.3f, 0.3f, 0.08f), blue);
            // jato de vapor (mostrado durante o ataque)
            steamFx = Prim.Pivot(model, Vector3.zero, "SteamFx");
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Sphere, steamFx, new Vector3(0, i * 1.6f, 0), Vector3.one * (1.2f + i * 0.25f), Mat.Glow(new Color(1f, 0.95f, 0.9f), 1.3f));
            steamFx.gameObject.SetActive(false);
        }

        public override HitResult TakeHit(int dmg, bool special, float dir, DamageSource src = DamageSource.Shot)
        {
            if (src != DamageSource.Pound && !GameManager.I.Cheats.oneHitKill) return HitResult.Deflected;   // porcelana: só o tremor racha
            var r = base.TakeHit(dmg, special, dir, src);
            if (r == HitResult.Hit) Crack();
            return r;
        }

        void Crack()
        {
            cracks++;
            var dark = Mat.Color(new Color(0.25f, 0.25f, 0.28f));
            for (int i = 0; i < 3; i++)   // rachaduras aparecem no bule
                Prim.Make(PrimitiveType.Cube, body, new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0.5f, 1.7f), -1.05f),
                    new Vector3(Random.Range(0.3f, 0.6f), 0.04f, 0.04f), dark, new Vector3(0, 0, Random.Range(0, 180)), "Crack");
            RefreshRenderers();
            GameManager.I.Shake(0.4f);
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0) { MeasureRoom(); stateTimer = 1.2f; }
            stateTimer -= dt;

            vel.y = Mathf.Max(vel.y - 38f * dt, -22f);
            bool was = grounded;
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt)) vel.x = 0;
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, true, out bool landed)) vel.y = 0;
            grounded = landed || TileCollision.Grounded(G, pos, half, true);
            if (grounded && !was && State == S.Hop)
            {
                vel.x = 0;
                GameManager.I.Shake(0.3f);
                Sfx.Play(Sfx.Pound);
                float wy = floorY + 0.38f;
                Projectile.Fire(new Vector2(pos.x - half.x, wy), new Vector2(-7f, 0), 3, false, false, 0.38f, Tea, 5f, 0, ShotShape.Wave);
                Projectile.Fire(new Vector2(pos.x + half.x, wy), new Vector2(7f, 0), 3, false, false, 0.38f, Tea, 5f, 0, ShotShape.Wave);
                Enter(S.Idle, Enraged ? 1.0f : 1.4f);   // janela para chegar perto e dar o impacto
            }

            switch (State)
            {
                case S.Intro:
                    if (stateTimer <= 0 && grounded) Enter(S.Idle, 1.0f);
                    break;
                case S.Idle:
                    if (stateTimer <= 0)
                    {
                        float r = Random.value;
                        if (r < 0.4f) { pours = Enraged ? 4 : 3; Enter(S.Pour, 0.4f); }
                        else if (r < 0.75f) StartHop();
                        else { steamX = P.Pos.x; Enter(S.Steam, 1.9f); }
                    }
                    break;
                case S.Pour:     // chá fervendo em arco na direção do jogador
                    lid.localPosition = new Vector3(0, 2.05f + Mathf.Abs(Mathf.Sin(Time.time * 20f)) * 0.15f, 0);
                    if (stateTimer <= 0)
                    {
                        float dx = P.Pos.x - pos.x;
                        float t = 1.0f;
                        var from = pos + new Vector2(Mathf.Sign(dx) * 1.9f, 0.6f);
                        var v = new Vector2((P.Pos.x - from.x) / t + Random.Range(-1f, 1f), 9f);
                        Projectile.Fire(from, v, 3, false, false, 0.22f, Tea, 3f, 18f);
                        pours--;
                        stateTimer = 0.45f;
                        if (pours <= 0) { lid.localPosition = new Vector3(0, 2.05f, 0); Enter(S.Idle, Enraged ? 1.2f : 1.6f); }
                    }
                    break;
                case S.Steam:    // marca o chão e solta um jato de vapor vertical
                    steamFx.gameObject.SetActive(stateTimer < 1.1f);
                    steamFx.position = new Vector3(steamX, floorY + 0.6f, 0);
                    if (stateTimer > 1.1f && Random.value < 0.4f) Fx.Sparks(new Vector2(steamX, floorY + 0.1f), Color.white, 1);
                    if (stateTimer < 1.1f && P.CanBeHit && Mathf.Abs(P.Pos.x - steamX) < 1.1f) P.Hurt(3, steamX);
                    if (stateTimer <= 0) { steamFx.gameObject.SetActive(false); Enter(S.Idle, 1.2f); }
                    break;
            }

            float face = Mathf.Sign(P.Pos.x - pos.x);
            model.localRotation = Quaternion.Euler(0, face > 0 ? -20 : 20, 0);
            model.localScale = new Vector3(face > 0 ? 1 : -1, 1, 1);   // bico para o lado do jogador
            steamFx.localScale = new Vector3(face > 0 ? 1 : -1, 1, 1);
        }

        void StartHop()
        {
            State = S.Hop;
            vel.y = Enraged ? 15f : 13f;
            float target = Mathf.Clamp(P.Pos.x, roomL + half.x + 0.3f, roomR - half.x - 0.3f);
            vel.x = Mathf.Clamp((target - pos.x) / 1.1f, -8f, 8f);
            grounded = false;
        }

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

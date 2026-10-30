using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Coqueteleira (Geleira — explora a Forma 3, DASH; durante o dash a lata atravessa projéteis):
    /// - Muro de gelo: um muro alto (4.6) desliza pelo chão — o pulo não passa; atravesse com o DASH.
    /// - Rajada: coluna de estilhaços em várias alturas — atravesse com o DASH.
    /// - Salto: pula por cima do jogador até o outro lado e esmaga o chão — use o DASH para sair de baixo.
    /// - Chuva de gelo: pingentes caem do teto perto do jogador.
    /// </summary>
    public class BossShaker : Boss
    {
        public enum S { Intro, Idle, Shake, Volley, Leap, Rain }
        public S State { get; private set; } = S.Intro;

        static readonly Color Ice = new Color(0.55f, 0.85f, 1f);

        const float DashCadence = 3.4f;   // recarga do dash é 3s: nunca exige dois dashes em menos que isso
        int dir = -1, icicles;
        float lastDashAttack = -99f;
        float vy, vx;
        Transform shaker;

        protected override void Setup()
        {
            displayName = "COQUETELEIRA";
            half = new Vector2(0.9f, 1.6f);
            hp = 32; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(0.8f, 0.82f, 0.88f);
            var steel = Mat.Color(new Color(0.85f, 0.87f, 0.93f), 0.75f, 0.45f);
            var band = Mat.Color(new Color(0.15f, 0.55f, 0.85f), 0.7f, 0.4f);
            shaker = Prim.Pivot(model, Vector3.zero, "Shaker");
            Prim.Make(PrimitiveType.Cylinder, shaker, new Vector3(0, -0.5f, 0), new Vector3(1.8f, 1.1f, 1.8f), steel);
            Prim.Make(PrimitiveType.Cylinder, shaker, new Vector3(0, 0.62f, 0), new Vector3(1.9f, 0.08f, 1.9f), band);
            Prim.MakeMesh(Prim.Cone, shaker, new Vector3(0, 0.65f, 0), new Vector3(1.85f, 1.2f, 1.85f), steel, default, "Top");
            Prim.Make(PrimitiveType.Cylinder, shaker, new Vector3(0, 1.45f, 0), new Vector3(0.55f, 0.15f, 0.55f), band);
            Prim.Make(PrimitiveType.Cylinder, shaker, new Vector3(0, -1.2f, 0), new Vector3(1.9f, 0.08f, 1.9f), band);
            // rosto e "suor" gelado
            var white = Mat.Color(Color.white);
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, shaker, new Vector3(-0.3f, 0.1f, -0.86f), new Vector3(0.32f, 0.3f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, shaker, new Vector3(0.3f, 0.1f, -0.86f), new Vector3(0.32f, 0.3f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, shaker, new Vector3(-0.27f, 0.08f, -0.92f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Sphere, shaker, new Vector3(0.27f, 0.08f, -0.92f), new Vector3(0.13f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Cube, shaker, new Vector3(-0.3f, 0.32f, -0.88f), new Vector3(0.36f, 0.07f, 0.05f), black, new Vector3(0, 0, -15));
            Prim.Make(PrimitiveType.Cube, shaker, new Vector3(0.3f, 0.32f, -0.88f), new Vector3(0.36f, 0.07f, 0.05f), black, new Vector3(0, 0, 15));
            Prim.Make(PrimitiveType.Cube, shaker, new Vector3(0, -0.25f, -0.9f), new Vector3(0.45f, 0.07f, 0.05f), black);
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Sphere, shaker, Quaternion.Euler(0, i * 60 + 15, 0) * new Vector3(0.92f, -0.8f + (i % 3) * 0.3f, 0), Vector3.one * 0.12f, Mat.Glow(Ice, 1.5f));
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0) { MeasureRoom(); stateTimer = 1.4f; }
            stateTimer -= dt;
            float groundY = floorY + half.y;

            switch (State)
            {
                case S.Intro:
                    vy = Mathf.Max(vy - 38f * dt, -20f);
                    pos.y = Mathf.Max(groundY, pos.y + vy * dt);
                    if (stateTimer <= 0 && pos.y <= groundY + 0.01f) Enter(S.Idle, 0.6f);
                    break;

                case S.Idle:
                    FacePlayer();
                    if (stateTimer <= 0)
                    {
                        float r = Random.value;
                        bool dashReady = Time.time - lastDashAttack >= DashCadence - 0.7f;   // o ataque só chega ~0.7s depois
                        if (dashReady && r < 0.6f)
                        {
                            if (r < 0.3f) Enter(S.Shake, 0.7f);
                            else Enter(S.Volley, 0.6f);
                        }
                        else if (r < 0.8f) StartLeap();
                        else { icicles = Enraged ? 7 : 5; Enter(S.Rain, 0.3f); }
                    }
                    break;

                case S.Shake:    // chacoalha (aviso) e bate no chão: muro de gelo
                    Wobble(true);
                    if (stateTimer <= 0)
                    {
                        Wobble(false);
                        GameManager.I.Shake(0.35f);
                        Sfx.Play(Sfx.Pound);
                        var wallHalf = new Vector2(0.55f, 2.3f);   // 4.6 de altura: o pulo normal não passa
                        Projectile.Fire(new Vector2(pos.x + dir * (half.x + 0.7f), floorY + wallHalf.y), new Vector2(dir * (Enraged ? 7.5f : 6.5f), 0),
                            4, false, false, 0.5f, Ice, 6f, 0, ShotShape.Wall, false, false, wallHalf);
                        lastDashAttack = Time.time;
                        Enter(S.Idle, Enraged ? 0.7f : 1.0f);
                    }
                    break;

                case S.Volley:   // coluna de estilhaços do chão até acima do pulo: só o dash atravessa
                    Wobble(stateTimer < 0.4f);
                    if (stateTimer <= 0)
                    {
                        Wobble(false);
                        Sfx.Play(Sfx.Shoot, 0.6f);
                        for (int i = 0; i < 5; i++)
                            Projectile.Fire(new Vector2(pos.x + dir * (half.x + 0.3f), floorY + 0.35f + i * 0.85f), new Vector2(dir * 8f, 0),
                                3, false, false, 0.22f, Ice, 4f);
                        lastDashAttack = Time.time;
                        Enter(S.Idle, Enraged ? 0.6f : 0.8f);
                    }
                    break;

                case S.Leap:     // salta por cima do jogador e esmaga o chão do outro lado
                    vy = Mathf.Max(vy - 40f * dt, -24f);
                    pos.x += vx * dt;
                    pos.y += vy * dt;
                    model.localRotation = Quaternion.Euler(0, 0, -vx * 4f);
                    if (pos.y <= groundY && vy < 0)
                    {
                        pos.y = groundY;
                        GameManager.I.Shake(0.4f);
                        Sfx.Play(Sfx.Pound);
                        Fx.Dust(new Vector2(pos.x, floorY), 12);
                        float wy = floorY + 0.38f;
                        Projectile.Fire(new Vector2(pos.x - 1f, wy), new Vector2(-8f, 0), 3, false, false, 0.38f, Ice, 5f, 0, ShotShape.Wave);
                        Projectile.Fire(new Vector2(pos.x + 1f, wy), new Vector2(8f, 0), 3, false, false, 0.38f, Ice, 5f, 0, ShotShape.Wave);
                        Enter(S.Idle, 0.9f);
                    }
                    break;

                case S.Rain:     // chuva de pingentes perto do jogador
                    if (stateTimer <= 0)
                    {
                        float x = Mathf.Clamp(P.Pos.x + Random.Range(-3f, 3f), roomL + 0.6f, roomR - 0.6f);
                        Projectile.Fire(new Vector2(x, ceilY - 0.6f), Vector2.zero, 3, false, false, 0.25f, Ice, 3f, 22f);
                        icicles--;
                        stateTimer = 0.3f;
                        if (icicles <= 0) Enter(S.Idle, 0.9f);
                    }
                    break;
            }

            if (State != S.Leap && State != S.Intro) pos.y = Mathf.MoveTowards(pos.y, groundY, 6f * dt);
            pos.x = Mathf.Clamp(pos.x, roomL + half.x, roomR - half.x);
            if (State != S.Leap) model.localRotation = Quaternion.Euler(0, dir > 0 ? -20 : 20, 0);
        }

        void StartLeap()
        {
            FacePlayer();
            float targetX = dir > 0 ? roomR - half.x - 1f : roomL + half.x + 1f;   // outro lado da sala, por cima do jogador
            const float air = 1.3f;
            vy = 40f * air * 0.5f;
            vx = (targetX - pos.x) / air;
            State = S.Leap;
            Sfx.Play(Sfx.Dash);
        }

        void Wobble(bool on)
        {
            shaker.localPosition = on ? new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 30f)) * 0.15f, 0) : Vector3.zero;
            shaker.localRotation = on ? Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 40f) * 12f) : Quaternion.identity;
        }

        void FacePlayer()
        {
            float d = P.Pos.x - pos.x;
            if (Mathf.Abs(d) > 0.3f) dir = d > 0 ? 1 : -1;
        }

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

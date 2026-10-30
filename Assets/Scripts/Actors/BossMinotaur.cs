using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Minotauro Vermelho (chefe final): enorme, muita vida, vencível com qualquer forma.
    /// Três fases de fúria (por vida restante), cada uma com padrões novos e mais rápida:
    ///  I  : investida abaixado (pule por cima), murro no chão com 2 ondas (pule), pedras arremessadas.
    ///  II : investida de ida e volta, marteladas duplas, sopro de fogo na altura da cabeça (NÃO pule), chuva de pedras.
    ///  III: investida tripla, salto pela sala com terremoto + chuva de pedras, fogo + pedras juntos.
    /// Toda esquiva funciona com o pulo normal (ou ficando no chão no sopro de fogo).
    /// </summary>
    public class BossMinotaur : Boss
    {
        public enum S { Intro, Idle, ChargePrep, Charge, SlamPrep, Slam, Toss, BreathPrep, Breath, Rain, Leap, Roar }
        public S State { get; private set; } = S.Intro;
        public int Phase { get; private set; } = 1;

        static readonly Vector2 StandHalf = new Vector2(2.0f, 2.6f);
        static readonly Vector2 ChargeHalf = new Vector2(2.2f, 1.1f);   // abaixado (2.2 de altura): o pulo normal passa por cima com folga
        static readonly Color Fire = new Color(1f, 0.45f, 0.1f);
        static readonly Color Rock = new Color(0.45f, 0.38f, 0.35f);

        int dir = -1, repeats, lastAttack = -1;
        float vy, vx;
        Transform body, head, mouth, armF, armB, legL, legR;
        Material eyeMat, furMat;

        float Speed => Phase == 1 ? 1f : Phase == 2 ? 1.3f : 1.6f;

        protected override void Setup()
        {
            displayName = "MINOTAURO VERMELHO";
            half = StandHalf;
            hp = 150; contactDamage = 5;
            invulnOnHit = 0.08f;
            activationRange = 999f;
            color = new Color(0.75f, 0.12f, 0.08f);
            furMat = Mat.New(new Color(0.62f, 0.12f, 0.08f), 0.3f);
            var furDark = Mat.Color(new Color(0.35f, 0.06f, 0.04f), 0.3f);
            var hoof = Mat.Color(new Color(0.1f, 0.08f, 0.08f), 0.5f);
            var ivory = Mat.Color(new Color(0.95f, 0.9f, 0.78f), 0.6f);
            var gold = Mat.Color(new Color(0.9f, 0.7f, 0.2f), 0.8f, 0.9f);
            eyeMat = Mat.New(new Color(1f, 0.85f, 0.2f), 0.8f);
            Mat.SetGlow(eyeMat, new Color(1f, 0.8f, 0.2f) * 2f);

            body = Prim.Pivot(model, new Vector3(0, -2.6f, 0), "Body");   // pivô nos cascos; frente = +X
            // pernas
            legL = Prim.Pivot(body, new Vector3(-0.6f, 1.6f, -0.3f), "LegL");
            legR = Prim.Pivot(body, new Vector3(0.6f, 1.6f, 0.3f), "LegR");
            foreach (var leg in new[] { legL, legR })
            {
                Prim.Make(PrimitiveType.Cylinder, leg, new Vector3(0, -0.7f, 0), new Vector3(0.75f, 0.75f, 0.75f), furDark);
                Prim.Make(PrimitiveType.Cylinder, leg, new Vector3(0, -1.5f, 0), new Vector3(0.7f, 0.12f, 0.7f), hoof);
            }
            // tronco musculoso e cinto
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0, 2.5f, 0), new Vector3(2.8f, 2.4f, 1.9f), furMat);
            Prim.Make(PrimitiveType.Cylinder, body, new Vector3(0, 1.55f, 0), new Vector3(2.2f, 0.18f, 1.7f), gold);
            Prim.Make(PrimitiveType.Cube, body, new Vector3(0.3f, 1.2f, -0.6f), new Vector3(1.4f, 0.8f, 0.3f), Mat.Color(new Color(0.3f, 0.18f, 0.1f), 0.2f));
            // cabeça de touro virada para a câmera (-Z): focinho claro, argola, orelhas e chifres em L
            var muzzle = Mat.Color(new Color(0.88f, 0.62f, 0.52f), 0.35f);
            var dark = Mat.Color(new Color(0.08f, 0.05f, 0.05f), 0.3f);
            head = Prim.Pivot(body, new Vector3(0.15f, 4.35f, -0.35f), "Head");
            Prim.Make(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(1.55f, 1.45f, 1.35f), furMat);
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0, -0.45f, -0.55f), new Vector3(1.05f, 0.75f, 0.7f), muzzle, default, "Muzzle");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.22f, -0.42f, -0.9f), new Vector3(0.18f, 0.14f, 0.08f), dark, default, "Nostril");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.22f, -0.42f, -0.9f), new Vector3(0.18f, 0.14f, 0.08f), dark, default, "Nostril");
            Prim.Make(PrimitiveType.Cylinder, head, new Vector3(0, -0.72f, -0.9f), new Vector3(0.42f, 0.035f, 0.42f), gold, new Vector3(90, 0, 0), "NoseRing");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.33f, 0.18f, -0.6f), new Vector3(0.24f, 0.16f, 0.1f), eyeMat, default, "Eye");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.33f, 0.18f, -0.6f), new Vector3(0.24f, 0.16f, 0.1f), eyeMat, default, "Eye");
            // sobrancelhas grossas e franzidas (expressão de fúria) + ruga entre elas
            Prim.Make(PrimitiveType.Cube, head, new Vector3(-0.36f, 0.4f, -0.64f), new Vector3(0.58f, 0.16f, 0.16f), dark, new Vector3(0, 0, -28), "Brow");
            Prim.Make(PrimitiveType.Cube, head, new Vector3(0.36f, 0.4f, -0.64f), new Vector3(0.58f, 0.16f, 0.16f), dark, new Vector3(0, 0, 28), "Brow");
            Prim.Make(PrimitiveType.Cube, head, new Vector3(0, 0.3f, -0.68f), new Vector3(0.06f, 0.2f, 0.05f), dark, default, "Furrow");
            // boca rosnando com dentes e presas
            mouth = Prim.Pivot(head, new Vector3(0, -0.8f, -0.72f), "Mouth");
            Prim.Make(PrimitiveType.Cube, mouth, Vector3.zero, new Vector3(0.75f, 0.18f, 0.12f), Mat.Color(new Color(0.25f, 0.02f, 0.02f), 0.3f));
            var tooth = Mat.Color(new Color(0.98f, 0.96f, 0.88f), 0.6f);
            for (int t = -2; t <= 2; t++)
                Prim.Make(PrimitiveType.Cube, mouth, new Vector3(t * 0.13f, 0.06f, -0.04f), new Vector3(0.09f, 0.08f, 0.06f), tooth);
            for (int sd = -1; sd <= 1; sd += 2)
                Prim.MakeMesh(Prim.Cone, mouth, new Vector3(sd * 0.3f, -0.08f, -0.05f), new Vector3(0.1f, -0.2f, 0.1f), tooth, default, "Fang");
            for (int i = -1; i <= 1; i++)   // topete
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(i * 0.22f, 0.72f, -0.15f), new Vector3(0.35f, 0.3f, 0.35f), furDark, default, "Tuft");
            for (int sd = -1; sd <= 1; sd += 2)
            {
                // orelha para o lado
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(sd * 0.85f, 0.15f, 0), new Vector3(0.55f, 0.24f, 0.32f), furDark, new Vector3(0, 0, sd * -15), "Ear");
                // chifre em L: sai na horizontal para o lado e sobe na ponta
                Prim.Make(PrimitiveType.Cylinder, head, new Vector3(sd * 1.05f, 0.5f, 0), new Vector3(0.26f, 0.5f, 0.26f), ivory, new Vector3(0, 0, 90), "HornBase");
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(sd * 1.55f, 0.52f, 0), Vector3.one * 0.27f, ivory, default, "HornElbow");
                Prim.MakeMesh(Prim.Cone, head, new Vector3(sd * 1.55f, 0.55f, 0), new Vector3(0.27f, 1.05f, 0.27f), ivory, default, "HornTip");
                Prim.MakeMesh(Prim.Cone, head, new Vector3(sd * 1.55f, 1.25f, 0), new Vector3(0.12f, 0.35f, 0.12f), dark, default, "HornPoint");
            }
            // braços nos dois lados com punhos fechados e braceletes (sem arma: o rosto fica à mostra)
            armB = Prim.Pivot(body, new Vector3(-1.35f, 3.5f, 0), "ArmL");
            armF = Prim.Pivot(body, new Vector3(1.35f, 3.5f, 0), "ArmR");
            // ombros e trapézio: ligam os braços ao tronco e o pescoço à cabeça
            Prim.Make(PrimitiveType.Sphere, body, new Vector3(0, 3.7f, 0.05f), new Vector3(2.5f, 1.0f, 1.4f), furMat, default, "Trapezius");
            for (int sd = -1; sd <= 1; sd += 2)
                Prim.Make(PrimitiveType.Sphere, body, new Vector3(sd * 1.3f, 3.45f, 0), new Vector3(1.15f, 1.05f, 1.1f), furMat, default, "Shoulder");
            foreach (var arm in new[] { armB, armF })
            {
                Prim.Make(PrimitiveType.Sphere, arm, new Vector3(0, -0.35f, 0), new Vector3(0.85f, 0.95f, 0.85f), furMat, default, "Bicep");
                Prim.Make(PrimitiveType.Cylinder, arm, new Vector3(0, -0.8f, 0), new Vector3(0.65f, 0.8f, 0.65f), furMat);
                Prim.Make(PrimitiveType.Cylinder, arm, new Vector3(0, -1.35f, 0), new Vector3(0.72f, 0.12f, 0.72f), gold);
                Prim.Make(PrimitiveType.Sphere, arm, new Vector3(0, -1.8f, 0), Vector3.one * 0.8f, furDark);
            }
        }

        void RaiseArms(float deg)
        {
            armB.localRotation = Quaternion.Euler(0, 0, -deg);
            armF.localRotation = Quaternion.Euler(0, 0, deg);
        }

        protected override void OnDamaged(int dmg, bool special, float d)
        {
            base.OnDamaged(dmg, special, d);
            int newPhase = hp > maxHp * 2 / 3 ? 1 : hp > maxHp / 3 ? 2 : 3;
            if (newPhase != Phase && hp > 0)
            {
                Phase = newPhase;
                State = S.Roar;
                stateTimer = 1.6f;
                invuln = 1.6f;
                SetCharge(false);
                vx = 0;
                GameManager.I.ShowBanner(Phase == 2 ? "FÚRIA II !!" : "FÚRIA III !!!", 2f);
                Sfx.Play(Sfx.Explode);
                Mat.SetGlow(eyeMat, new Color(1f, Phase == 2 ? 0.4f : 0.1f, 0.1f) * (Phase == 2 ? 3.5f : 5f));
                Mat.SetGlow(furMat, new Color(0.6f, 0.05f, 0.02f) * (Phase == 2 ? 0.25f : 0.6f));
            }
        }

        void SetCharge(bool on)
        {
            half = on ? ChargeHalf : StandHalf;
            pos.y = floorY + half.y;
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0)
            {
                MeasureRoom();
                stateTimer = 1.5f;
                pos.y = Mathf.Max(pos.y, floorY + half.y);
            }
            stateTimer -= dt;
            float sp = Speed;

            switch (State)
            {
                case S.Intro:
                    vy -= 40f * dt;
                    pos.y = Mathf.Max(floorY + half.y, pos.y + vy * dt);
                    if (pos.y <= floorY + half.y + 0.01f && stateTimer < 1.2f && vy < 0) { GameManager.I.Shake(0.6f); vy = 0; }
                    if (stateTimer <= 0) Enter(S.Idle, 0.8f);
                    break;

                case S.Roar:
                    head.localRotation = Quaternion.Euler(0, 0, 25 + Mathf.Sin(Time.time * 40f) * 6f);
                    GameManager.I.Shake(0.25f);
                    if (stateTimer <= 0) { head.localRotation = Quaternion.identity; Enter(S.Idle, 0.6f); }
                    break;

                case S.Idle:
                    FacePlayer();
                    if (stateTimer <= 0) ChooseAttack();
                    break;

                case S.ChargePrep:   // raspa o casco no chão (aviso)
                    legR.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 25f) * 30f);
                    if (Random.value < 0.4f) Fx.Dust(new Vector2(pos.x - dir * 0.8f, floorY), 1);
                    if (stateTimer <= 0)
                    {
                        legR.localRotation = Quaternion.identity;
                        SetCharge(true);
                        State = S.Charge;
                        vx = dir * 10f * sp;
                        Sfx.Play(Sfx.Dash);
                    }
                    break;

                case S.Charge:       // abaixado: pule por cima
                    if (Random.value < 0.5f) Fx.Dust(new Vector2(pos.x, floorY), 1);
                    if (TileCollision.MoveX(G, ref pos, half, vx * dt))
                    {
                        GameManager.I.Shake(0.5f);
                        Sfx.Play(Sfx.Pound);
                        SetCharge(false);
                        repeats--;
                        if (repeats > 0) { dir = -dir; Enter(S.ChargePrep, 0.75f / sp); }
                        else Enter(S.Idle, 1.1f / sp);
                    }
                    break;

                case S.SlamPrep:     // ergue os dois punhos
                    RaiseArms(Mathf.Lerp(0, 160, 1 - stateTimer / (0.7f / sp)));
                    if (stateTimer <= 0) Enter(S.Slam, 0f);
                    break;

                case S.Slam:         // martelada: ondas de choque para os dois lados (pule)
                    RaiseArms(-20);
                    GameManager.I.Shake(0.5f);
                    Sfx.Play(Sfx.Pound);
                    Fx.Dust(new Vector2(pos.x + dir * 2.5f, floorY), 14);
                    Waves(7f * sp);
                    repeats--;
                    if (repeats > 0) Enter(S.SlamPrep, 0.7f / sp);
                    else { RaiseArms(0); Enter(S.Idle, 1.0f / sp); }
                    break;

                case S.Toss:         // arremessa pedras em arco
                    if (stateTimer <= 0)
                    {
                        var from = pos + new Vector2(dir * 1.5f, 2.5f);
                        float t = 1.1f;
                        var v = new Vector2((P.Pos.x - from.x) / t + Random.Range(-1f, 1f), 0.5f * 22f * t + (P.Pos.y - from.y) / t);
                        Projectile.Fire(from, v, 4, false, false, 0.45f, Rock, 4f, 22f, ShotShape.Bomb);
                        repeats--;
                        stateTimer = 0.5f / sp;
                        if (repeats <= 0) Enter(S.Idle, 0.9f / sp);
                    }
                    break;

                case S.BreathPrep:   // inspira (aviso): fique no chão!
                    head.localRotation = Quaternion.Euler(0, 0, 20);
                    if (Random.value < 0.5f) Fx.Sparks(pos + new Vector2(dir * 1.8f, 1.8f), Fire, 1);
                    if (stateTimer <= 0) { head.localRotation = Quaternion.Euler(0, 0, -10); Enter(S.Breath, 1.3f); }
                    break;

                case S.Breath:       // faixa de fogo na altura da cabeça (1.7 a 3.2 do chão): quem fica no chão passa
                    if (Mathf.Repeat(stateTimer, 0.07f) < dt)
                        Projectile.Fire(new Vector2(pos.x + dir * 2.2f, floorY + 2.45f), new Vector2(dir * 13f, Random.Range(-0.4f, 0.4f)), 3, false, false, 0.7f, Fire, 2.5f);
                    if (Phase == 3 && Random.value < dt * 2.5f) RockFromCeiling(Random.Range(roomL + 1, roomR - 1));
                    if (stateTimer <= 0) { head.localRotation = Quaternion.identity; Enter(S.Idle, 1.0f / sp); }
                    break;

                case S.Rain:         // chuva de pedras do teto
                    if (stateTimer <= 0)
                    {
                        RockFromCeiling(Mathf.Clamp(P.Pos.x + Random.Range(-4f, 4f), roomL + 1, roomR - 1));
                        repeats--;
                        stateTimer = 0.25f / sp;
                        if (repeats <= 0) Enter(S.Idle, 0.8f / sp);
                    }
                    break;

                case S.Leap:         // salto pela sala por cima do jogador: terremoto ao cair
                    vy -= 40f * dt;
                    pos.x += vx * dt;
                    pos.y += vy * dt;
                    if (pos.y <= floorY + half.y && vy < 0)
                    {
                        pos.y = floorY + half.y;
                        GameManager.I.Shake(0.8f);
                        Sfx.Play(Sfx.Pound);
                        Fx.Dust(new Vector2(pos.x, floorY), 20);
                        Waves(8f * sp);
                        for (int i = 0; i < 5; i++) RockFromCeiling(Random.Range(roomL + 1, roomR - 1));
                        Enter(S.Idle, 1.0f);
                    }
                    break;
            }

            pos.x = Mathf.Clamp(pos.x, roomL + half.x, roomR - half.x);
            Animate(dt);
        }

        void ChooseAttack()
        {
            // cada fase tem seu repertório; não repete o mesmo ataque em seguida
            int[] pool = Phase == 1 ? new[] { 0, 1, 2 } : Phase == 2 ? new[] { 0, 1, 3, 4 } : new[] { 0, 3, 5, 1 };
            int a;
            do a = pool[Random.Range(0, pool.Length)]; while (a == lastAttack && pool.Length > 1);
            lastAttack = a;
            float sp = Speed;
            switch (a)
            {
                case 0: repeats = Phase; Enter(S.ChargePrep, 0.9f / sp); break;                 // 1, 2 ou 3 investidas
                case 1: repeats = Phase == 1 ? 1 : Phase; Enter(S.SlamPrep, 0.7f / sp); break;   // marteladas
                case 2: repeats = 3; Enter(S.Toss, 0.4f); break;
                case 3: Enter(S.BreathPrep, 0.9f / sp); break;
                case 4: repeats = 8; Enter(S.Rain, 0.4f); break;
                case 5:
                    FacePlayer();
                    float targetX = dir > 0 ? roomR - half.x - 0.5f : roomL + half.x + 0.5f;
                    const float air = 1.2f;
                    vy = 40f * air * 0.5f;
                    vx = (targetX - pos.x) / air;
                    State = S.Leap;
                    Sfx.Play(Sfx.Dash);
                    break;
            }
        }

        void Waves(float speed)
        {
            float wy = floorY + 0.38f;
            Projectile.Fire(new Vector2(pos.x - half.x, wy), new Vector2(-speed, 0), 4, false, false, 0.4f, Fire, 5f, 0, ShotShape.Wave);
            Projectile.Fire(new Vector2(pos.x + half.x, wy), new Vector2(speed, 0), 4, false, false, 0.4f, Fire, 5f, 0, ShotShape.Wave);
        }

        void RockFromCeiling(float x)
        {
            Fx.Sparks(new Vector2(x, ceilY - 0.3f), Rock, 3);
            Projectile.Fire(new Vector2(x, ceilY - 0.7f), Vector2.zero, 4, false, false, 0.45f, Rock, 3f, 20f, ShotShape.Bomb);
        }

        void FacePlayer()
        {
            float d = P.Pos.x - pos.x;
            if (Mathf.Abs(d) > 0.5f) dir = d > 0 ? 1 : -1;
        }

        void Animate(float dt)
        {
            model.localScale = new Vector3(dir, 1, 1);
            bool charging = State == S.Charge;
            body.localRotation = Quaternion.Euler(0, 0, charging ? -55f : 0f);
            body.localPosition = new Vector3(charging ? -0.6f : 0, charging ? -1.1f : -2.6f, 0);
            float walk = charging ? Time.time * 25f : 0;
            legL.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(walk) * 35f);
            if (State != S.ChargePrep) legR.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(walk) * 35f);
            model.localRotation = Quaternion.Euler(0, dir > 0 ? -15 : 15, 0) * Quaternion.identity;
            if (State == S.Idle) body.localScale = new Vector3(1, 1 + Mathf.Sin(Time.time * 3f) * 0.02f, 1);   // respiração
            // boca abre ao rugir / soprar fogo; bufa pelas narinas de vez em quando
            bool open = State == S.Roar || State == S.Breath || State == S.BreathPrep;
            mouth.localScale = new Vector3(1, open ? 2.4f : 1f, 1);
            if (State == S.Idle && Random.value < dt * 1.5f)
                Fx.Sparks(head.position + new Vector3(0, -0.5f, -0.9f), Phase == 3 ? Fire : new Color(0.9f, 0.9f, 0.9f), 4);
        }

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

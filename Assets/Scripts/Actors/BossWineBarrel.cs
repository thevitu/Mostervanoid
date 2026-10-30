using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Barril de Vinho: barril deitado que flutua sobre jatos deixando um vão baixo sob o casco.
    /// A FRENTE é blindada (reflete tudo) — o ponto fraco é a rolha nas COSTAS.
    /// Ele investe pela sala: em pé você é atropelado; deslizando (Forma 1) você passa por baixo,
    /// fica atrás dele e, quando ele bate na parede e fica atordoado, atira na rolha.
    /// </summary>
    public class BossWineBarrel : Boss
    {
        public enum S { Intro, Aim, Shots, Telegraph, Charge, Stunned, Turn, Hop }
        public S State { get; private set; } = S.Intro;
        public int FacingDir => facing;
        public float FloorY => floorY;
        public const float Clearance = 0.8f;     // vão entre o chão e o casco (deslizando a lata tem 0.68 de altura)

        int facing = -1;                         // lado para onde a frente blindada aponta
        int shotsLeft, chargesLeft;
        float speed, vy, hoverY;
        Transform hull, jets;
        Material coreMat;

        protected override void Setup()
        {
            displayName = "BARRIL DE VINHO";
            half = new Vector2(2.0f, 1.35f);
            hp = 30; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(0.45f, 0.25f, 0.12f);
            var wood = Mat.Textured(ProcTex.Wood, new Color(0.85f, 0.6f, 0.4f), 0.3f, 0f, new Vector2(3, 1));
            var iron = Mat.Color(new Color(0.22f, 0.22f, 0.25f), 0.7f, 0.9f);
            var armor = Mat.Color(new Color(0.50f, 0.52f, 0.56f), 0.85f, 0.95f);
            var dark = Mat.Color(new Color(0.12f, 0.12f, 0.14f), 0.5f, 0.6f);
            var wine = new Color(0.65f, 0.05f, 0.25f);

            hull = Prim.Pivot(model, Vector3.zero, "Hull");                     // frente = +X local
            // barril deitado: aduelas de madeira (cilindro + "barriga" esférica) com aros de ferro
            Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(-0.1f, 0, 0), new Vector3(2.3f, 1.75f, 2.3f), wood, new Vector3(0, 0, 90));
            Prim.Make(PrimitiveType.Sphere, hull, new Vector3(-0.1f, 0, 0), new Vector3(3.6f, 2.55f, 2.55f), wood);
            foreach (float x in new[] { -1.45f, -0.6f, 0.4f, 1.25f })
                Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(x, 0, 0), new Vector3(x * x < 1 ? 2.62f : 2.42f, 0.08f, x * x < 1 ? 2.62f : 2.42f), iron, new Vector3(0, 0, 90));
            // tampa da frente blindada + torneira
            Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(1.7f, 0, 0), new Vector3(2.45f, 0.18f, 2.45f), armor, new Vector3(0, 0, 90));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Prim.Make(PrimitiveType.Sphere, hull, new Vector3(1.9f, Mathf.Sin(a) * 0.95f, Mathf.Cos(a) * 0.95f), Vector3.one * 0.18f, iron);
            }
            Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(2.1f, -0.4f, -0.3f), new Vector3(0.22f, 0.25f, 0.22f), iron, new Vector3(0, 0, 90));
            Prim.Make(PrimitiveType.Cube, hull, new Vector3(2.3f, -0.2f, -0.3f), new Vector3(0.1f, 0.4f, 0.1f), iron);
            Prim.Make(PrimitiveType.Cube, hull, new Vector3(1.95f, 0.45f, -0.75f), new Vector3(0.05f, 0.16f, 0.45f), Mat.Glow(new Color(1f, 0.2f, 0.1f), 3f));
            Prim.Make(PrimitiveType.Cube, hull, new Vector3(1.95f, 0.45f, 0.75f), new Vector3(0.05f, 0.16f, 0.45f), Mat.Glow(new Color(1f, 0.2f, 0.1f), 3f));
            // tampa de trás com a rolha exposta (ponto fraco)
            Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(-1.85f, 0, 0), new Vector3(2.3f, 0.1f, 2.3f), wood, new Vector3(0, 0, 90));
            coreMat = Mat.New(wine, 0.8f);
            Mat.SetGlow(coreMat, wine * 2f);
            Prim.Make(PrimitiveType.Sphere, hull, new Vector3(-1.95f, 0.1f, 0), new Vector3(0.6f, 0.95f, 0.95f), coreMat, default, "Core");
            Prim.Make(PrimitiveType.Cylinder, hull, new Vector3(-2.2f, 0.1f, 0), new Vector3(0.55f, 0.25f, 0.55f), Mat.Color(new Color(0.75f, 0.58f, 0.38f), 0.2f), new Vector3(0, 0, 90), "Cork");
            // jatos sob o barril
            jets = Prim.Pivot(model, new Vector3(0, -1.15f, 0), "Jets");
            var flame = Mat.Glow(new Color(0.4f, 0.8f, 1f), 3.5f);
            for (int i = -1; i <= 1; i++)
            {
                Prim.Make(PrimitiveType.Cylinder, jets, new Vector3(i * 1.2f, 0.05f, 0), new Vector3(0.6f, 0.12f, 0.9f), dark);
                Prim.MakeMesh(Prim.Cone, jets, new Vector3(i * 1.2f, -0.05f, 0), new Vector3(0.45f, -0.3f, 0.6f), flame, default, "Flame");
            }
        }

        public override HitResult TakeHit(int dmg, bool special, float dir, DamageSource src = DamageSource.Shot)
        {
            // o ataque viaja na mesma direção para onde a frente aponta => veio por trás e acertou o núcleo
            bool fromBack = Mathf.Sign(dir) == facing;
            if (!fromBack && !GameManager.I.Cheats.oneHitKill) return HitResult.Deflected;
            return base.TakeHit(dmg, special, dir, src);
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0)
            {
                MeasureRoom();
                hoverY = floorY + Clearance + half.y;
                pos.y = Mathf.Min(ceilY - half.y - 0.2f, hoverY + 6f);
                stateTimer = 1.6f;
            }
            stateTimer -= dt;
            float jetFlicker = 1f + Mathf.Sin(Time.time * 50f) * 0.15f;

            switch (State)
            {
                case S.Intro:
                    pos.y = Mathf.MoveTowards(pos.y, hoverY, 5f * dt);
                    if (stateTimer <= 0 && pos.y <= hoverY + 0.01f) Enter(S.Aim, 0.7f);
                    break;

                case S.Aim:
                    FacePlayer();
                    if (stateTimer <= 0)
                    {
                        if (Random.value < 0.3f) { State = S.Hop; vy = 13f; Sfx.Play(Sfx.Dash); }
                        else { shotsLeft = Enraged ? 4 : 3; Enter(S.Shots, 0.3f); }
                    }
                    break;

                case S.Shots:   // tiros rasantes: pule por cima ou deslize por baixo
                    if (stateTimer <= 0)
                    {
                        var from = new Vector2(pos.x + facing * (half.x + 0.2f), floorY + 1.05f);
                        Projectile.Fire(from, new Vector2(facing * (Enraged ? 8.5f : 7f), 0), 3, false, false, 0.16f, new Color(0.75f, 0.1f, 0.35f), 4f);
                        Sfx.Play(Sfx.Shoot, 0.5f);
                        shotsLeft--;
                        stateTimer = 0.5f;
                        if (shotsLeft <= 0) { chargesLeft = Enraged ? 2 : 1; StartTelegraph(1.0f); }
                    }
                    break;

                case S.Telegraph:
                    hull.localPosition = new Vector3(Mathf.Sin(Time.time * 70f) * 0.05f, 0, 0);
                    jetFlicker = 1.6f;
                    if (stateTimer <= 0)
                    {
                        hull.localPosition = Vector3.zero;
                        State = S.Charge;
                        speed = 2f;
                        Sfx.Play(Sfx.Dash);
                    }
                    break;

                case S.Charge:
                    speed = Mathf.MoveTowards(speed, Enraged ? 15f : 13f, 40f * dt);
                    jetFlicker = 1.8f;
                    if (Random.value < 0.5f) Fx.Sparks(new Vector2(pos.x - facing * half.x, floorY + 0.4f), new Color(0.4f, 0.8f, 1f), 1);
                    if (TileCollision.MoveX(G, ref pos, half, facing * speed * dt))
                    {
                        GameManager.I.Shake(0.45f);
                        Sfx.Play(Sfx.Pound);
                        Fx.Dust(new Vector2(pos.x + facing * half.x, floorY), 12);
                        chargesLeft--;
                        Enter(S.Stunned, chargesLeft > 0 ? 1.3f : 2.4f);   // 1.3 + virar 0.45 + aviso 1.44 > 3s da recarga do slide
                    }
                    break;

                case S.Stunned:  // núcleo pulsando: hora de atirar nas costas!
                    jetFlicker = 0.4f;
                    hull.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 20f) * 2f);
                    if (Random.value < 0.15f) Fx.Sparks(pos + new Vector2(0, half.y), Color.yellow, 2);
                    if (stateTimer <= 0) { hull.localRotation = Quaternion.identity; Enter(S.Turn, 0.45f); }
                    break;

                case S.Turn:
                    if (stateTimer <= 0)
                    {
                        FacePlayer();
                        if (chargesLeft > 0) StartTelegraph(1.6f);   // 2ª investida: tempo para a recarga do slide
                        else Enter(S.Aim, 0.5f);
                    }
                    break;

                case S.Hop:      // salta e esmaga o chão: ondas de choque rasteiras (pule!)
                    vy -= 38f * dt;
                    pos.y += vy * dt;
                    if (pos.y <= hoverY)
                    {
                        pos.y = hoverY;
                        GameManager.I.Shake(0.35f);
                        Sfx.Play(Sfx.Pound);
                        float wy = floorY + 0.38f;
                        var c = new Color(0.4f, 0.8f, 1f);
                        Projectile.Fire(new Vector2(pos.x - half.x, wy), new Vector2(-7.5f, 0), 3, false, false, 0.38f, c, 5f, 0, ShotShape.Wave);
                        Projectile.Fire(new Vector2(pos.x + half.x, wy), new Vector2(7.5f, 0), 3, false, false, 0.38f, c, 5f, 0, ShotShape.Wave);
                        Enter(S.Aim, 0.6f);
                    }
                    break;
            }

            pos.x = Mathf.Clamp(pos.x, roomL + half.x, roomR - half.x);
            // visual: frente para 'facing', leve giro para mostrar volume, jatos e núcleo pulsando
            model.localScale = new Vector3(facing, 1, 1);
            model.localRotation = Quaternion.Euler(0, facing > 0 ? -15 : 15, 0);
            jets.localScale = new Vector3(1, jetFlicker, 1);
            float coreGlow = State == S.Stunned ? 4f + Mathf.Sin(Time.time * 18f) * 2f : 1.5f;
            coreMat.SetColor("_EmissionColor", new Color(0.65f, 0.05f, 0.25f) * coreGlow * 1.5f);
        }

        void FacePlayer()
        {
            float d = P.Pos.x - pos.x;
            if (Mathf.Abs(d) > 0.3f) facing = d > 0 ? 1 : -1;
        }

        void StartTelegraph(float t) => Enter(S.Telegraph, Enraged ? t * 0.9f : t);

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

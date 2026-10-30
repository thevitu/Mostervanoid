using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Garrafa de Champanhe (Céu Borbulhante — explora a Forma 4, PULO DUPLO):
    /// - Flutua alto: só se acerta atirando no meio de um pulo duplo (a sala não tem plataformas).
    /// - Rajada de espuma: vários muros (4.8 de altura) seguidos, alternando o lado e misturados com ondas baixas.
    ///   O pulo simples não passa o muro; o pulo duplo (sem recarga!) passa um atrás do outro.
    ///   Durante a rajada ela sobe para perto do teto (fora do alcance) — é a fase de esquiva.
    /// - Rolha explosiva: estoura para cima e se divide em bolhas que caem em leque.
    /// - Colunas de bolhas: sobem do chão nos pontos marcados (saia de cima).
    /// </summary>
    public class BossChampagne : Boss
    {
        public enum S { Intro, Hover, FoamPrep, Barrage, CorkPrep, Columns }
        public S State { get; private set; } = S.Intro;

        const float HoverHeight = 7.2f;                   // centro acima do chão: alcançável com pulo duplo (apex ~7.3)
        const float SafeHeight = 10.6f;                   // durante a rajada sobe para fora do alcance do pulo duplo (não esbarra no jogador)
        static readonly Color Foam = new Color(1f, 0.92f, 0.6f);

        Vector2 target;
        int shots, lastAttack = -1;
        int wallsLeft, wallIndex;
        bool nextFromLeft;
        float[] columnsX = new float[3];
        float columnTimer;
        Transform bottle;

        protected override void Setup()
        {
            displayName = "GARRAFA DE CHAMPANHE";
            half = new Vector2(0.85f, 1.6f);
            hp = 34; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(0.1f, 0.35f, 0.15f);
            var glass = Mat.Color(new Color(0.08f, 0.32f, 0.14f), 0.95f, 0.15f);
            var foil = Mat.Color(new Color(0.95f, 0.78f, 0.3f), 0.85f, 0.9f);
            var label = Mat.Color(new Color(0.96f, 0.92f, 0.80f), 0.4f);
            bottle = Prim.Pivot(model, Vector3.zero, "Bottle");
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, -0.55f, 0), new Vector3(1.6f, 1.0f, 1.6f), glass);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 1.2f, 1.6f), glass);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 1.15f, 0), new Vector3(0.55f, 0.45f, 0.55f), glass);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 1.3f, 0), new Vector3(0.6f, 0.35f, 0.6f), foil);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 1.72f, 0), new Vector3(0.5f, 0.1f, 0.5f), Mat.Color(new Color(0.75f, 0.58f, 0.38f), 0.2f));
            for (int i = 0; i < 4; i++)   // gaiola de arame
                Prim.Make(PrimitiveType.Cube, bottle, new Vector3(0, 1.6f, 0), new Vector3(0.62f, 0.03f, 0.03f), foil, new Vector3(0, i * 45, 0));
            // rótulo creme com rosto
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, -0.45f, 0), new Vector3(1.64f, 0.45f, 1.64f), label);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 0.02f, 0), new Vector3(1.66f, 0.04f, 1.66f), foil);
            var white = Mat.Color(Color.white);
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(-0.28f, -0.3f, -0.8f), new Vector3(0.3f, 0.3f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0.28f, -0.3f, -0.8f), new Vector3(0.3f, 0.3f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(-0.25f, -0.32f, -0.86f), new Vector3(0.12f, 0.14f, 0.05f), black);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0.25f, -0.32f, -0.86f), new Vector3(0.12f, 0.14f, 0.05f), black);
            Prim.Make(PrimitiveType.Cube, bottle, new Vector3(-0.28f, -0.1f, -0.84f), new Vector3(0.3f, 0.06f, 0.04f), black, new Vector3(0, 0, -15));
            Prim.Make(PrimitiveType.Cube, bottle, new Vector3(0.28f, -0.1f, -0.84f), new Vector3(0.3f, 0.06f, 0.04f), black, new Vector3(0, 0, 15));
            Prim.Make(PrimitiveType.Cube, bottle, new Vector3(0, -0.6f, -0.84f), new Vector3(0.35f, 0.06f, 0.04f), black);
            // bolhas douradas em volta
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Sphere, bottle, Quaternion.Euler(0, i * 60, 0) * new Vector3(1.0f, -1.2f + (i % 3) * 0.5f, 0), Vector3.one * 0.15f, Mat.Glow(Foam, 2f));
        }

        float HoverY => floorY + HoverHeight;

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0)
            {
                MeasureRoom();
                target = new Vector2((roomL + roomR) * 0.5f, HoverY);
                stateTimer = 1.5f;
            }
            stateTimer -= dt;

            switch (State)
            {
                case S.Intro:
                    pos = Vector2.MoveTowards(pos, target, 4f * dt);
                    if (stateTimer <= 0) NextHover();
                    break;

                case S.Hover:    // passeia no alto atirando rolhas mirando o jogador
                    pos = Vector2.MoveTowards(pos, target + new Vector2(0, Mathf.Sin(Time.time * 2.5f) * 0.25f), 4.5f * dt);
                    if (stateTimer <= 0)
                    {
                        if (shots > 0)
                        {
                            Shoot(pos + new Vector2(0, 1.4f), P.Pos - pos, Enraged ? 7.5f : 6.5f, 3, Foam);
                            Sfx.Play(Sfx.Shoot, 0.5f);
                            shots--;
                            stateTimer = 0.7f;
                        }
                        else ChooseAttack();
                    }
                    break;

                case S.FoamPrep: // sobe perto do teto, chacoalha e começa a rajada de espuma
                    pos = Vector2.MoveTowards(pos, new Vector2(pos.x, floorY + SafeHeight), 8f * dt);
                    bottle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 45f) * 10f);
                    if (Random.value < 0.4f) Fx.Sparks(pos + new Vector2(0, 1.6f), Foam, 2);
                    if (stateTimer <= 0)
                    {
                        bottle.localRotation = Quaternion.identity;
                        State = S.Barrage;
                        wallsLeft = Enraged ? 5 : 3;
                        wallIndex = 0;
                        nextFromLeft = P.Pos.x > (roomL + roomR) * 0.5f;   // o primeiro vem do lado oposto ao jogador
                        stateTimer = 0;
                    }
                    break;

                case S.Barrage:  // muros em sequência (o pulo duplo não tem recarga: dá para encadear)
                    pos = Vector2.MoveTowards(pos, new Vector2(pos.x, floorY + SafeHeight), 8f * dt);
                    bottle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 45f) * 6f);
                    if (stateTimer <= 0)
                    {
                        FireFoam(nextFromLeft, wallIndex % 3 == 2);   // a cada 3, uma onda baixa (pulo simples basta)
                        wallIndex++;
                        wallsLeft--;
                        if (Enraged || wallIndex % 2 == 0) nextFromLeft = !nextFromLeft;
                        stateTimer = Enraged ? 1.8f : 2.1f;   // ~1.3s de pulo duplo + tempo de reagir: encadeia sem respiro
                        if (wallsLeft <= 0) { bottle.localRotation = Quaternion.identity; NextHover(); }
                    }
                    break;

                case S.CorkPrep: // rolha estoura para cima e se divide em bolhas
                    bottle.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 3f, 0.12f));
                    if (stateTimer <= 0)
                    {
                        bottle.localScale = Vector3.one;
                        Sfx.Play(Sfx.Special);
                        int n = Enraged ? 7 : 5;
                        for (int i = 0; i < n; i++)
                        {
                            float vx = (i - (n - 1) * 0.5f) * 2.0f + (P.Pos.x - pos.x) * 0.25f;
                            Projectile.Fire(pos + new Vector2(0, 1.8f), new Vector2(vx, 10f), 3, false, false, 0.22f, Foam, 4f, 16f);
                        }
                        NextHover();
                    }
                    break;

                case S.Columns:  // marca 3 pontos no chão e sobem colunas de bolhas
                    columnTimer -= dt;
                    if (stateTimer > 1.2f)
                    {
                        foreach (var cx in columnsX)
                            if (Random.value < 0.3f) Fx.Sparks(new Vector2(cx, floorY + 0.1f), Foam, 1);
                    }
                    else if (columnTimer <= 0)
                    {
                        columnTimer = 0.12f;
                        foreach (var cx in columnsX)
                            Projectile.Fire(new Vector2(cx + Random.Range(-0.2f, 0.2f), floorY + 0.2f), new Vector2(0, 9f), 3, false, false, 0.28f, Foam, 1.4f);
                    }
                    if (stateTimer <= 0) NextHover();
                    break;
            }

            pos.x = Mathf.Clamp(pos.x, roomL + half.x, roomR - half.x);
            float face = Mathf.Sign(P.Pos.x - pos.x);
            model.localRotation = Quaternion.Euler(0, face > 0 ? -20 : 20, 0);
        }

        void FireFoam(bool fromLeft, bool low)
        {
            GameManager.I.Shake(0.25f);
            Sfx.Play(Sfx.Explode, 0.5f);
            float dir = fromLeft ? 1 : -1;
            float sx = fromLeft ? roomL + 0.8f : roomR - 0.8f;
            float speed = Enraged ? 7.5f : 6.5f;
            if (low)
                Projectile.Fire(new Vector2(sx, floorY + 0.38f), new Vector2(dir * speed, 0), 3, false, false, 0.38f, Foam, 5f, 0, ShotShape.Wave);
            else
            {
                var wallHalf = new Vector2(0.55f, 2.4f);   // 4.8: acima do pulo simples, abaixo do pulo duplo
                Projectile.Fire(new Vector2(sx, floorY + wallHalf.y), new Vector2(dir * speed, 0),
                    4, false, false, 0.5f, Foam, 5f, 0, ShotShape.Wall, false, false, wallHalf);
            }
        }

        void ChooseAttack()
        {
            // não repete o mesmo ataque duas vezes seguidas
            int a;
            do a = Random.Range(0, 3); while (a == lastAttack);
            lastAttack = a;
            switch (a)
            {
                case 0: State = S.FoamPrep; stateTimer = 0.9f; break;
                case 1: State = S.CorkPrep; stateTimer = 0.7f; break;
                default:
                    State = S.Columns;
                    stateTimer = 2.4f;
                    columnTimer = 0;
                    float w = roomR - roomL;
                    columnsX[0] = Mathf.Clamp(P.Pos.x, roomL + 1, roomR - 1);
                    columnsX[1] = roomL + w * Random.Range(0.15f, 0.4f);
                    columnsX[2] = roomL + w * Random.Range(0.6f, 0.85f);
                    break;
            }
        }

        void NextHover()
        {
            State = S.Hover;
            shots = Enraged ? 3 : 2;
            stateTimer = 1.0f;
            target = new Vector2(Random.Range(roomL + 3f, roomR - 3f), HoverY);
        }
    }
}

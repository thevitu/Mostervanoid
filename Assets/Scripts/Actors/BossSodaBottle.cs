using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Garrafa de Refri (Vinhedo — explora a Forma 2, SUPER PULO):
    /// fica empoleirada no alto da sala, na altura de plataformas que só o super pulo alcança, atirando bolhas.
    /// Depois desce e dá um rasante efervescente rente ao chão (pule por cima) e volta ao poleiro do outro lado;
    /// às vezes pousa no centro soltando ondas de espuma (pule).
    /// </summary>
    public class BossSodaBottle : Boss
    {
        public enum S { Intro, Perch, Descend, Rush, Rise, Geyser }
        public S State { get; private set; } = S.Intro;

        const float PerchHeight = 9.5f;       // centro da garrafa acima do chão (plataformas altas em +8)
        const float RushClearance = 0.05f;    // rasante rente ao chão: pule por cima
        static readonly Vector2 UprightHalf = new Vector2(0.85f, 1.35f);
        static readonly Vector2 RushHalf = new Vector2(1.4f, 0.6f);
        static readonly Color Fizz = new Color(0.55f, 1f, 0.45f);

        int side = 1;                          // poleiro atual: -1 esquerda, +1 direita
        int shots;
        float rushDir;
        Vector2 target;
        Transform bottle;

        protected override void Setup()
        {
            displayName = "GARRAFA DE REFRI";
            half = UprightHalf;
            hp = 28; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(0.3f, 0.75f, 0.35f);
            var pet = Mat.Color(new Color(0.35f, 0.80f, 0.40f), 0.95f, 0.05f);
            var cola = Mat.Color(new Color(0.25f, 0.12f, 0.05f), 0.9f);
            var labelMat = Mat.Color(new Color(0.9f, 0.12f, 0.12f), 0.5f);
            var capMat = Mat.Color(new Color(0.85f, 0.1f, 0.1f), 0.6f, 0.2f);
            bottle = Prim.Pivot(model, Vector3.zero, "Bottle");          // garrafa em pé: tampa para +Y
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, -0.45f, 0), new Vector3(1.6f, 0.85f, 1.6f), pet);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, -0.55f, 0), new Vector3(1.5f, 0.72f, 1.5f), cola);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 1.3f, 1.6f), pet);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 1.05f, 0), new Vector3(0.5f, 0.2f, 0.5f), pet);
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, 1.28f, 0), new Vector3(0.6f, 0.08f, 0.6f), capMat);
            // fundo "pétala" das garrafas PET
            for (int i = 0; i < 5; i++)
                Prim.Make(PrimitiveType.Sphere, bottle, Quaternion.Euler(0, i * 72, 0) * new Vector3(0.45f, -1.25f, 0), new Vector3(0.6f, 0.3f, 0.6f), pet);
            // rótulo com rosto
            Prim.Make(PrimitiveType.Cylinder, bottle, new Vector3(0, -0.35f, 0), new Vector3(1.65f, 0.38f, 1.65f), labelMat);
            var white = Mat.Color(Color.white);
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(-0.28f, -0.2f, -0.8f), new Vector3(0.3f, 0.32f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0.28f, -0.2f, -0.8f), new Vector3(0.3f, 0.32f, 0.12f), white);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(-0.25f, -0.22f, -0.86f), new Vector3(0.12f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(0.25f, -0.22f, -0.86f), new Vector3(0.12f, 0.15f, 0.05f), black);
            Prim.Make(PrimitiveType.Cube, bottle, new Vector3(0, -0.55f, -0.82f), new Vector3(0.4f, 0.07f, 0.04f), black);
            // bolhas subindo dentro
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Sphere, bottle, new Vector3(-0.4f + i * 0.16f, -1f + (i % 3) * 0.35f, -0.6f), Vector3.one * 0.1f, Mat.Glow(new Color(0.9f, 0.8f, 0.6f), 1.2f));
        }

        // poleiros afastados das plataformas altas (que ficam junto às paredes): dá para atirar delas sem encostar na garrafa
        Vector2 Perch(int s) => new Vector2(s < 0 ? roomL + 6.5f : roomR - 6.5f, floorY + PerchHeight);

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0)
            {
                MeasureRoom();
                side = P.Pos.x < (roomL + roomR) * 0.5f ? 1 : -1;   // começa do lado oposto ao jogador
                target = Perch(side);
                stateTimer = 1.5f;
            }
            stateTimer -= dt;
            float tilt = 0;

            switch (State)
            {
                case S.Intro:
                    pos = Vector2.MoveTowards(pos, target, 6f * dt);
                    if (stateTimer <= 0) EnterPerch();
                    break;

                case S.Perch:   // no alto: use o SUPER PULO até as plataformas altas (ou atire no ar durante o super pulo)
                    pos = Vector2.MoveTowards(pos, target + new Vector2(0, Mathf.Sin(Time.time * 3f) * 0.3f), 4f * dt);
                    if (stateTimer <= 0)
                    {
                        if (shots > 0)
                        {
                            var d = P.Pos - pos;
                            Shoot(pos + new Vector2(0, 0.6f), d, Enraged ? 7.5f : 6f, 3, Fizz);
                            if (Enraged) Shoot(pos + new Vector2(0, 0.6f), Quaternion.Euler(0, 0, 18) * d, 6f, 3, Fizz);
                            Sfx.Play(Sfx.Shoot, 0.5f);
                            shots--;
                            stateTimer = 0.55f;
                        }
                        else if (Random.value < 0.7f)
                        {
                            State = S.Descend;
                            rushDir = -side;
                            target = new Vector2(Perch(side).x, floorY + RushClearance + RushHalf.y);
                            stateTimer = 0.8f;
                        }
                        else
                        {
                            State = S.Geyser;
                            target = new Vector2((roomL + roomR) * 0.5f, floorY + UprightHalf.y);
                            stateTimer = 2.4f;
                        }
                    }
                    break;

                case S.Descend:  // deita e mira o rasante (aviso: espuma saindo da tampa)
                    pos = Vector2.MoveTowards(pos, target, 12f * dt);
                    half = RushHalf;
                    tilt = rushDir > 0 ? -90 : 90;
                    if (Random.value < 0.5f) Fx.Sparks(pos - new Vector2(rushDir * 1.4f, 0), Fizz, 2);
                    if (stateTimer <= 0 && Vector2.Distance(pos, target) < 0.05f) { State = S.Rush; Sfx.Play(Sfx.Dash); }
                    break;

                case S.Rush:     // rasante rente ao chão: pule por cima
                    half = RushHalf;
                    tilt = rushDir > 0 ? -90 : 90;
                    pos.x += rushDir * (Enraged ? 15f : 12.5f) * dt;
                    Fx.Sparks(pos - new Vector2(rushDir * 1.5f, 0), Fizz, 1);
                    if ((rushDir > 0 && pos.x >= roomR - 2.2f) || (rushDir < 0 && pos.x <= roomL + 2.2f))
                    {
                        side = (int)rushDir;
                        State = S.Rise;
                        target = Perch(side);
                    }
                    break;

                case S.Rise:
                    half = UprightHalf;
                    pos = Vector2.MoveTowards(pos, target, 9f * dt);
                    Fx.Sparks(pos - new Vector2(0, 1.3f), Fizz, 1);
                    if (Vector2.Distance(pos, target) < 0.05f) EnterPerch();
                    break;

                case S.Geyser:   // pousa no centro, chacoalha e solta ondas de espuma pelo chão (pule!) — vulnerável aqui embaixo
                    half = UprightHalf;
                    pos = Vector2.MoveTowards(pos, target, 10f * dt);
                    if (Vector2.Distance(pos, target) < 0.05f)
                    {
                        bottle.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.06f, 0, 0);
                        if (stateTimer < 1.4f && stateTimer + dt >= 1.4f)
                        {
                            GameManager.I.Shake(0.3f);
                            Sfx.Play(Sfx.Pound);
                            float wy = floorY + 0.38f;
                            Projectile.Fire(new Vector2(pos.x - 1f, wy), new Vector2(-7.5f, 0), 3, false, false, 0.38f, Fizz, 5f, 0, ShotShape.Wave);
                            Projectile.Fire(new Vector2(pos.x + 1f, wy), new Vector2(7.5f, 0), 3, false, false, 0.38f, Fizz, 5f, 0, ShotShape.Wave);
                            for (int i = -2; i <= 2; i++)
                                Projectile.Fire(pos + new Vector2(0, 1.4f), new Vector2(i * 2.2f, 9f), 3, false, false, 0.16f, Fizz, 3f, 14f);
                        }
                    }
                    if (stateTimer <= 0)
                    {
                        bottle.localPosition = Vector3.zero;
                        side = P.Pos.x < pos.x ? 1 : -1;
                        State = S.Rise;
                        target = Perch(side);
                    }
                    break;
            }

            bool lying = State == S.Descend || State == S.Rush;
            bottle.localScale = Vector3.one * (lying ? 0.75f : 1f);   // deitada fica do tamanho da caixa do rasante
            float face = Mathf.Sign(P.Pos.x - pos.x);
            model.localRotation = Quaternion.Euler(0, 0, tilt) * Quaternion.Euler(0, face > 0 ? -20 : 20, 0);
        }

        void EnterPerch()
        {
            State = S.Perch;
            half = UprightHalf;
            target = Perch(side);
            shots = Enraged ? 4 : 3;
            stateTimer = 0.9f;
        }
    }
}

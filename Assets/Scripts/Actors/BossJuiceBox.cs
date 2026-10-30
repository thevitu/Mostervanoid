using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Caixinha de Suco (Chaminé do Café — explora a Forma 6, GANCHO):
    /// inunda o chão com suco (2.4s, menos que os 3s que dá para ficar pendurado) — pendure-se nos ganchos do teto.
    /// O gancho não tem recarga, então as enchentes vêm em sequência. Entre elas: gotas mirando o jogador e saltos.
    /// Dá para atirar pendurado.
    /// </summary>
    public class BossJuiceBox : Boss
    {
        public enum S { Intro, Idle, FloodWarn, Flood, Shots, Hop }
        public S State { get; private set; } = S.Intro;
        public const float FloodHeight = 1.3f;     // altura do suco acima do chão
        public const float FloodTime = 2.4f;

        static readonly Color Juice = new Color(1f, 0.55f, 0.1f);
        Vector2 vel;
        bool grounded;
        int floodsLeft, shots;
        Transform box, straw, flood, warnSign;
        float warnTotal;

        protected override void Setup()
        {
            displayName = "CAIXINHA DE SUCO";
            half = new Vector2(1.1f, 1.5f);
            hp = 30; contactDamage = 4;
            invulnOnHit = 0.15f;
            activationRange = 999f;
            color = new Color(1f, 0.6f, 0.15f);
            var carton = Mat.Color(new Color(1f, 0.62f, 0.15f), 0.3f);
            var white = Mat.Color(Color.white, 0.3f);
            var green = Mat.Color(new Color(0.3f, 0.75f, 0.2f), 0.3f);
            box = Prim.Pivot(model, Vector3.zero, "Box");
            Prim.Make(PrimitiveType.Cube, box, Vector3.zero, new Vector3(2.2f, 3.0f, 1.6f), carton);
            Prim.Make(PrimitiveType.Cube, box, new Vector3(0, 0.9f, -0.81f), new Vector3(1.8f, 0.9f, 0.02f), white);
            // laranja desenhada
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(0, -0.6f, -0.82f), new Vector3(1.0f, 1.0f, 0.05f), Mat.Color(new Color(1f, 0.45f, 0.05f), 0.5f));
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(0.25f, -0.05f, -0.84f), new Vector3(0.35f, 0.18f, 0.03f), green, new Vector3(0, 0, 30));
            // canudinho
            straw = Prim.Pivot(box, new Vector3(0.6f, 1.5f, 0), "Straw");
            Prim.Make(PrimitiveType.Cylinder, straw, new Vector3(0, 0.6f, 0), new Vector3(0.12f, 0.6f, 0.12f), Mat.Color(new Color(1f, 0.3f, 0.5f), 0.6f));
            Prim.Make(PrimitiveType.Cylinder, straw, new Vector3(-0.25f, 1.3f, 0), new Vector3(0.12f, 0.3f, 0.12f), Mat.Color(new Color(1f, 0.3f, 0.5f), 0.6f), new Vector3(0, 0, 60));
            // rosto
            var black = Mat.Color(Color.black);
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(-0.4f, 0.95f, -0.83f), new Vector3(0.32f, 0.32f, 0.08f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(0.4f, 0.95f, -0.83f), new Vector3(0.32f, 0.32f, 0.08f), Mat.Color(Color.white));
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(-0.37f, 0.93f, -0.87f), new Vector3(0.13f, 0.15f, 0.04f), black);
            Prim.Make(PrimitiveType.Sphere, box, new Vector3(0.37f, 0.93f, -0.87f), new Vector3(0.13f, 0.15f, 0.04f), black);
            Prim.Make(PrimitiveType.Cube, box, new Vector3(0, 0.62f, -0.83f), new Vector3(0.45f, 0.07f, 0.04f), black);
            // placa de aviso "!" (aparece antes da enchente)
            warnSign = Prim.Pivot(model, new Vector3(0, 3.6f, -0.3f), "WarnSign");
            var warnMat = Mat.Glow(new Color(1f, 0.85f, 0.1f), 3f);
            Prim.Make(PrimitiveType.Cube, warnSign, new Vector3(0, 0.25f, 0), new Vector3(0.28f, 0.75f, 0.2f), warnMat);
            Prim.Make(PrimitiveType.Cube, warnSign, new Vector3(0, -0.45f, 0), new Vector3(0.28f, 0.28f, 0.2f), warnMat);
            warnSign.gameObject.SetActive(false);
            // pernas
            var leg = Mat.Color(new Color(0.15f, 0.15f, 0.18f));
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(-0.5f, -1.45f, 0), new Vector3(0.25f, 0.15f, 0.25f), leg);
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0.5f, -1.45f, 0), new Vector3(0.25f, 0.15f, 0.25f), leg);
        }

        protected override void Tick(float dt)
        {
            if (State == S.Intro && stateTimer == 0)
            {
                MeasureRoom();
                stateTimer = 1.2f;
                // lâmina de suco (fica escondida até a enchente)
                flood = Prim.Make(PrimitiveType.Cube, transform.parent, new Vector3((roomL + roomR) * 0.5f, floorY + FloodHeight * 0.5f, 0.8f),
                    new Vector3(roomR - roomL, FloodHeight, 4.2f), Mat.Glow(Juice, 0.9f), default, "JuiceFlood").transform;
                flood.gameObject.SetActive(false);
            }
            stateTimer -= dt;

            vel.y = Mathf.Max(vel.y - 38f * dt, -22f);
            bool was = grounded;
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt)) vel.x = 0;
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, true, out bool landed)) vel.y = 0;
            grounded = landed || TileCollision.Grounded(G, pos, half, true);
            if (grounded && !was && State == S.Hop) { vel.x = 0; GameManager.I.Shake(0.25f); Sfx.Play(Sfx.Land); Enter(S.Idle, 0.8f); }

            switch (State)
            {
                case S.Intro:
                    if (stateTimer <= 0 && grounded) Enter(S.Idle, 0.8f);
                    break;
                case S.Idle:
                    if (stateTimer <= 0)
                    {
                        float r = Random.value;
                        if (r < 0.5f) { floodsLeft = Enraged ? 3 : 2; StartWarn(1.5f); }
                        else if (r < 0.8f) { shots = Enraged ? 4 : 3; Enter(S.Shots, 0.4f); }
                        else StartHop();
                    }
                    break;
                case S.FloodWarn: // AVISO: "!" piscando, canudinho gotejando e o suco começando a minar do chão (ainda sem dano)
                {
                    float k = 1f - Mathf.Clamp01(stateTimer / warnTotal);           // 0 -> 1 durante o aviso
                    bool blink = Mathf.Repeat(Time.time * 6f, 1f) < 0.55f;
                    warnSign.gameObject.SetActive(blink);
                    warnSign.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 12f) * 0.12f);
                    straw.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30f) * 15f);
                    flood.gameObject.SetActive(blink || k > 0.7f);
                    float h = Mathf.Lerp(0.08f, 0.35f, k);
                    flood.localScale = new Vector3(roomR - roomL, h, 4.2f);
                    flood.position = new Vector3((roomL + roomR) * 0.5f, floorY + h * 0.5f, 0.8f);
                    if (Random.value < 0.8f) Fx.Sparks(new Vector2(Random.Range(roomL, roomR), floorY + h), Juice, 2);
                    if (Random.value < 0.25f) Fx.Sparks(pos + new Vector2(0.35f, 3.1f), Juice, 1);
                    if (Mathf.Repeat(stateTimer, 0.5f) < Time.deltaTime) Sfx.Play(Sfx.Menu, 0.6f);
                    if (stateTimer <= 0)
                    {
                        warnSign.gameObject.SetActive(false);
                        flood.position = new Vector3((roomL + roomR) * 0.5f, floorY + FloodHeight * 0.5f, 0.8f);
                        flood.gameObject.SetActive(true);
                        Enter(S.Flood, FloodTime);
                        Sfx.Play(Sfx.Slide);
                    }
                    break;
                }
                case S.Flood:
                    flood.localScale = new Vector3(roomR - roomL, FloodHeight * (1f + Mathf.Sin(Time.time * 8f) * 0.05f), 4.2f);
                    if (P.CanBeHit && P.Pos.y - P.Half.y < floorY + FloodHeight - 0.1f && P.Pos.x > roomL && P.Pos.x < roomR)
                        P.Hurt(3, P.Pos.x - P.Facing);
                    if (stateTimer <= 0)
                    {
                        flood.gameObject.SetActive(false);
                        straw.localRotation = Quaternion.identity;
                        floodsLeft--;
                        // gancho não tem recarga: a próxima enchente vem logo (tempo de descer e subir de novo)
                        if (floodsLeft > 0) StartWarn(Enraged ? 1.4f : 1.6f);
                        else Enter(S.Idle, 1.0f);
                    }
                    break;
                case S.Shots:
                    if (stateTimer <= 0)
                    {
                        Shoot(pos + new Vector2(0.6f, 3.0f), P.Pos - (pos + new Vector2(0.6f, 3f)), Enraged ? 7.5f : 6.5f, 3, Juice);
                        Sfx.Play(Sfx.Shoot, 0.5f);
                        shots--;
                        stateTimer = 0.5f;
                        if (shots <= 0) Enter(S.Idle, 0.8f);
                    }
                    break;
            }

            float face = Mathf.Sign(P.Pos.x - pos.x);
            model.localRotation = Quaternion.Euler(0, face > 0 ? -20 : 20, 0);
        }

        void StartWarn(float t)
        {
            warnTotal = t;
            Enter(S.FloodWarn, t);
            GameManager.I.Toast("O suco vai subir! Pendure-se num GANCHO (segure L/3 no ar)", 1.5f);
        }

        void StartHop()
        {
            State = S.Hop;
            vel.y = 13f;
            float target = Mathf.Clamp(P.Pos.x, roomL + half.x + 0.3f, roomR - half.x - 0.3f);
            vel.x = Mathf.Clamp((target - pos.x) / 1.1f, -7f, 7f);
            grounded = false;
        }

        public override void Die()
        {
            if (flood != null) Destroy(flood.gameObject);
            base.Die();
        }

        void Enter(S s, float t) { State = s; stateTimer = t; }
    }
}

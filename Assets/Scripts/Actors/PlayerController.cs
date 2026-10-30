using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Protagonista (lata de energético). Física própria por grade, estilo plataforma clássico.
    /// Ação especial (L/3) depende da forma; recarga sempre de 3s (exceto Gancho, que limita a 3s pendurado).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ----------------------------------------------------- ajuste fino
        const float Gravity = 45f, MaxFall = 18f;
        const float RunSpeed = 6.2f;
        const float JumpVel = 18.5f;              // ~3.8 blocos
        const float SuperJumpVel = 32f;           // ~3x a altura do pulo normal
        const float CoyoteTime = 0.08f, JumpBuffer = 0.12f;
        const float SlideSpeed = 11f, SlideTime = 0.5f;
        const float DashSpeed = 17f, DashTime = 0.22f;
        const float PoundSpeed = 28f;
        const float WallChargeSpeed = 13f, WallChargeTime = 0.32f;
        const float HookRange = 2.2f, HookMaxTime = 3f;
        const float ShotSpeed = 15f;
        const float WindPush = 10f;                // mais rápido que a corrida: só o dash vence o vapor
        public const int MaxHp = 28;

        public static readonly Vector2 StandHalf = new Vector2(0.34f, 0.78f);
        public static readonly Vector2 SlideHalf = new Vector2(0.34f, 0.34f);

        enum St { Normal, Slide, Dash, Pound, Hook, Charge, WallCharge, Hurt, Dead }
        St st = St.Normal;

        public Vector2 Pos => pos;
        public Vector2 Half => half;
        public int Hp { get; private set; } = MaxHp;
        public int Form { get; private set; }
        public int Facing => facing;
        public float CooldownFrac => Mathf.Clamp01(cooldown / Forms.SpecialCooldown);
        public float HookFrac => st == St.Hook ? hookTimer / HookMaxTime : 0f;
        public bool IsDead => st == St.Dead;
        /// <summary>Durante o Dash (Forma 3) a lata fica invulnerável: atravessa projéteis, ondas e inimigos.</summary>
        public bool ProjectileImmune => st == St.Dash;
        public bool CanBeHit => st != St.Dead && st != St.Dash && iframes <= 0 && !GameManager.I.Cheats.invincible && !GameManager.I.Cheats.noclip;

        Vector2 pos, vel, half = StandHalf;
        int facing = 1;
        bool grounded;
        float coyote, jumpBuffer;
        bool jumpCut;           // pulo pode ser "cortado" ao soltar o botão
        int airJumps;
        float cooldown, iframes, stateTimer, shootAnim, chargeTime, hookTimer;
        bool hookNeedsRelease;
        Vector2 hookPoint;
        bool flipping;

        CanModel model;
        LevelGrid G => GameManager.I.Grid;

        public static PlayerController Create(Transform parent)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PlayerController>();
            p.model = CanModel.Create(go.transform);
            return p;
        }

        public void SetForm(int f)
        {
            Form = Mathf.Clamp(f, 0, Forms.Count - 1);
            model.SetSkin(SkinLibrary.Get(Form));
            if (st == St.Slide && !CanStand()) { } // continua deslizando até sair da fresta
            else if (st != St.Dead && st != St.Hurt) EndSpecialStates();
        }

        public void RefreshSkin() => model.SetSkin(SkinLibrary.Get(Form));

        public void Spawn(Vector2 feet)
        {
            half = StandHalf;
            pos = feet + new Vector2(0, half.y + 0.01f);
            vel = Vector2.zero;
            st = St.Normal;
            Hp = MaxHp;
            iframes = 0;
            cooldown = 0;
            hookNeedsRelease = false;
            model.SetVisible(true);
            gameObject.SetActive(true);
            transform.position = pos;
        }

        public void Heal() => Hp = MaxHp;

        public bool IsGrounded => grounded;

        /// <summary>Centraliza a lata na coluna cx (usado pela armadilha do copo). Falha se houver parede.</summary>
        public bool TrySnapToCell(int cx)
        {
            var p = new Vector2(cx + 0.5f, pos.y);
            if (TileCollision.Overlaps(G, p, half)) return false;
            pos = p;
            vel.x = 0;
            transform.position = new Vector3(pos.x, pos.y, 0);
            return true;
        }

        public void TeleportTo(Vector2 feet)
        {
            EndSpecialStates();
            half = StandHalf;
            pos = feet + new Vector2(0, half.y + 0.01f);
            vel = Vector2.zero;
        }

        // ================================================================== dano

        public void Hurt(int dmg, float fromX)
        {
            if (!CanBeHit) return;
            if (!GameManager.I.Cheats.infiniteLife) Hp -= dmg;   // vida infinita: leva o golpe (recuo/piscar) mas não perde vida
            Sfx.Play(Sfx.Hurt);
            if (Hp <= 0) { Die(); return; }
            iframes = 1.3f;
            float dir = pos.x < fromX ? -1 : 1;
            if (st == St.Slide && !CanStand())
            {
                // dentro da fresta: só fica invulnerável
                return;
            }
            EndSpecialStates();
            st = St.Hurt;
            stateTimer = 0.35f;
            vel = new Vector2(dir * 3.5f, grounded ? 5f : Mathf.Max(vel.y, 2f));
        }

        public void Die()
        {
            if (st == St.Dead) return;
            Hp = 0;
            st = St.Dead;
            model.SetVisible(false);
            Fx.Explode(pos, Forms.Get(Form).color, 24, 10f, 0.25f);
            GameManager.I.OnPlayerDied();
        }

        // ================================================================== loop

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Grid == null || st == St.Dead) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (dt <= 0f) return;
            bool inputOk = gm.InputAllowed;

            if (gm.Cheats.noclip) { Noclip(dt, inputOk); return; }

            float h = inputOk ? GameInput.Horizontal : 0;
            bool jumpDown = inputOk && GameInput.JumpDown;
            bool jumpHeld = inputOk && GameInput.JumpHeld;
            bool shootDown = inputOk && GameInput.ShootDown;
            bool spDown = inputOk && GameInput.SpecialDown;
            bool spHeld = inputOk && GameInput.SpecialHeld;
            bool down = inputOk && GameInput.Down;

            if (cooldown > 0) cooldown -= dt;
            if (gm.Cheats.noCooldown) cooldown = 0;
            if (iframes > 0) iframes -= dt;
            if (shootAnim > 0) shootAnim -= dt;
            if (jumpBuffer > 0) jumpBuffer -= dt;
            if (jumpDown) jumpBuffer = JumpBuffer;
            if (!spHeld) hookNeedsRelease = false;

            grounded = TileCollision.Grounded(G, pos, half, down && jumpDown);
            if (grounded) { coyote = CoyoteTime; airJumps = 1; flipping = false; }
            else coyote -= dt;

            var form = (FormId)Form;
            switch (st)
            {
                case St.Normal: Normal(dt, h, jumpHeld, down, shootDown, spDown, spHeld, form); break;
                case St.Slide: Slide(dt, h, down); break;
                case St.Dash: Dash(dt, shootDown); break;
                case St.Pound: Pound(dt); break;
                case St.Hook: Hook(dt, h, spHeld, shootDown); break;
                case St.Charge: Charge(dt, h, spHeld); break;
                case St.WallCharge: WallCharge(dt); break;
                case St.Hurt: HurtState(dt); break;
            }
            if (st == St.Dead) return;

            // jatos de vapor empurram (exceto durante o dash)
            int wind = WindAt();
            if (wind != 0 && st != St.Dash) TileCollision.MoveX(G, ref pos, half, wind * WindPush * dt);

            // perigos
            bool cheatAlive = gm.Cheats.invincible || gm.Cheats.infiniteLife;
            if (TileCollision.TouchesCell(G, pos, half, Cell.Spike))
            {
                if (!cheatAlive) { Die(); return; }
                if (vel.y <= 0) { vel.y = JumpVel; Hurt(0, pos.x - facing); }
            }
            if (pos.y < -2f)
            {
                if (cheatAlive) { vel.y = JumpVel * 1.6f; }
                else { Die(); return; }
            }

            transform.position = new Vector3(pos.x, pos.y, 0);
            AnimateModel(dt);
        }

        // ------------------------------------------------------------------ estados

        void Normal(float dt, float h, bool jumpHeld, bool down, bool shootDown, bool spDown, bool spHeld, FormId form)
        {
            if (h != 0) facing = h > 0 ? 1 : -1;
            vel.x = h * RunSpeed;

            // pulo (com "drop" de plataformas: baixo + pulo)
            if (jumpBuffer > 0)
            {
                if (down && grounded && OnOneWay())
                {
                    jumpBuffer = 0;
                    pos.y -= 0.1f;
                    grounded = false;
                    coyote = 0;
                }
                else if (coyote > 0)
                {
                    DoJump(JumpVel);
                }
                else if (form == FormId.DoubleJump && airJumps > 0)
                {
                    airJumps--;
                    DoJump(JumpVel * 0.95f);
                    flipping = true;
                    Fx.Dust(pos - new Vector2(0, half.y), 4);
                }
            }
            if (!jumpHeld && jumpCut && vel.y > 4f) { vel.y = 4f; jumpCut = false; }

            if (shootDown) Shoot();

            // ação especial
            if (cooldown <= 0)
            {
                switch (form)
                {
                    case FormId.Original:
                    case FormId.DoubleJump:
                        if (spDown) SpecialShot();
                        break;
                    case FormId.Slide:
                        if (spDown && grounded) StartSlide();
                        break;
                    case FormId.SuperJump:
                        if (spDown && grounded) { st = St.Charge; chargeTime = 0; }
                        break;
                    case FormId.Dash:
                        if (spDown) StartDash();
                        break;
                    case FormId.GroundPound:
                        if (spDown && !grounded) StartPound();
                        break;
                    case FormId.WallImpact:
                        if (spDown) StartWallCharge();
                        break;
                }
            }
            if (form == FormId.Hook && spHeld && !grounded && !hookNeedsRelease) TryHook();

            ApplyGravityAndMove(dt);
        }

        void DoJump(float v)
        {
            vel.y = v;
            jumpBuffer = 0;
            coyote = 0;
            grounded = false;
            jumpCut = true;
            Sfx.Play(Sfx.Jump, 0.6f);
        }

        void ApplyGravityAndMove(float dt, float gravityScale = 1f)
        {
            vel.y = Mathf.Max(vel.y - Gravity * gravityScale * dt, -MaxFall);
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt)) vel.x = 0;
            bool wasAir = !grounded;
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, false, out bool landed))
            {
                if (landed && wasAir && vel.y < -8f) { Sfx.Play(Sfx.Land, 0.5f); }
                if (vel.y > 0) jumpCut = false;
                vel.y = 0;
            }
            if (landed) grounded = true;
        }

        void Shoot()
        {
            if (st != St.Normal && st != St.Hook) return;
            if (Projectile.PlayerShots >= 3) return;
            shootAnim = 0.25f;
            Sfx.Play(Sfx.Shoot, 0.6f);
            var from = pos + new Vector2(facing * 0.65f, 0.05f);
            Projectile.Fire(from, new Vector2(facing * ShotSpeed, 0), 1, true, false, 0.13f, new Color(1f, 0.95f, 0.3f), 1.1f, 0, ShotShape.Ball, true);
        }

        void SpecialShot()
        {
            cooldown = Forms.SpecialCooldown;
            shootAnim = 0.35f;
            Sfx.Play(Sfx.Special);
            var from = pos + new Vector2(facing * 0.75f, 0.05f);
            Projectile.Fire(from, new Vector2(facing * ShotSpeed * 1.1f, 0), 5, true, true, 0.3f, Forms.Get(Form).accent * 0.5f + new Color(0.2f, 0.9f, 1f) * 0.5f, 1.4f);
        }

        // --- Forma 1: Slide
        void StartSlide()
        {
            cooldown = Forms.SpecialCooldown;
            st = St.Slide;
            stateTimer = SlideTime;
            SetHalf(SlideHalf);
            Sfx.Play(Sfx.Slide);
            Fx.Dust(pos - new Vector2(0, half.y), 5);
        }

        void Slide(float dt, float h, bool down)
        {
            if (h != 0) facing = h > 0 ? 1 : -1;
            stateTimer -= dt;
            vel.x = facing * SlideSpeed;
            bool canStand = CanStand();

            if (jumpBuffer > 0 && canStand && grounded)
            {
                SetHalf(StandHalf);
                st = St.Normal;
                DoJump(JumpVel);
                return;
            }
            if (!grounded && canStand) { SetHalf(StandHalf); st = St.Normal; return; }
            if (stateTimer <= 0 && canStand) { SetHalf(StandHalf); st = St.Normal; vel.x = 0; return; }
            ApplyGravityAndMove(dt);
        }

        int WindAt()
        {
            int x0 = Mathf.FloorToInt(pos.x - half.x + 0.05f), x1 = Mathf.FloorToInt(pos.x + half.x - 0.05f);
            int y0 = Mathf.FloorToInt(pos.y - half.y + 0.05f), y1 = Mathf.FloorToInt(pos.y + half.y - 0.05f);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    var c = G.Get(x, y);
                    if (c == Cell.WindL) return -1;
                    if (c == Cell.WindR) return 1;
                }
            return 0;
        }

        bool CanStand()
        {
            if (half == StandHalf) return true;
            var p = pos + new Vector2(0, StandHalf.y - half.y);
            return !TileCollision.Overlaps(G, p, StandHalf);
        }

        void SetHalf(Vector2 nh)
        {
            // mantém a base (pés) no mesmo lugar
            pos.y += nh.y - half.y;
            half = nh;
        }

        bool OnOneWay()
        {
            int cy = Mathf.FloorToInt(pos.y - half.y - 0.04f);
            int x0 = Mathf.FloorToInt(pos.x - half.x + 0.01f), x1 = Mathf.FloorToInt(pos.x + half.x - 0.01f);
            bool any = false;
            for (int x = x0; x <= x1; x++)
            {
                if (G.IsSolid(x, cy)) return false;
                if (G.IsOneWay(x, cy)) any = true;
            }
            return any;
        }

        // --- Forma 2: Super pulo
        void Charge(float dt, float h, bool spHeld)
        {
            if (h != 0) facing = h > 0 ? 1 : -1;
            vel.x = 0;
            chargeTime += dt;
            if (!grounded) { st = St.Normal; return; }
            if (!spHeld)
            {
                st = St.Normal;
                if (chargeTime >= 0.25f)
                {
                    cooldown = Forms.SpecialCooldown;
                    DoJump(SuperJumpVel);
                    jumpCut = false; // super pulo não é cortado ao soltar o botão
                    Sfx.Play(Sfx.Dash);
                    Fx.Dust(pos - new Vector2(0, half.y), 10);
                }
                return;
            }
            ApplyGravityAndMove(dt);
        }

        // --- Forma 3: Dash
        void StartDash()
        {
            cooldown = Forms.SpecialCooldown;
            st = St.Dash;
            stateTimer = DashTime;
            vel = new Vector2(facing * DashSpeed, 0);
            Sfx.Play(Sfx.Dash);
        }

        void Dash(float dt, bool shootDown)
        {
            stateTimer -= dt;
            vel.y = 0;
            TileCollision.MoveX(G, ref pos, half, vel.x * dt);   // bloqueado por parede: o dash (e a imunidade) dura até o fim
            if (Random.value < 0.6f) Fx.Sparks(pos - new Vector2(facing * 0.4f, 0), Forms.Get(Form).color, 1);
            if (stateTimer <= 0)
            {
                st = St.Normal;
                vel.x = facing * RunSpeed;
            }
        }

        // --- Forma 5: Impacto no chão
        void StartPound()
        {
            cooldown = Forms.SpecialCooldown;
            st = St.Pound;
            vel = new Vector2(0, 4f);
            stateTimer = 0.12f;  // pequena pausa no ar antes de descer
            Sfx.Play(Sfx.Dash);
        }

        void Pound(float dt)
        {
            stateTimer -= dt;
            if (stateTimer > 0) { vel.y = Mathf.Max(0, vel.y - 60 * dt); TileCollision.MoveY(G, ref pos, half, vel.y * dt, false, out _); return; }
            vel.x = 0;
            vel.y = -PoundSpeed;
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, false, out bool landed) && landed)
            {
                // quebra piso rachado e continua descendo
                int cy = Mathf.FloorToInt(pos.y - half.y - 0.05f);
                int x0 = Mathf.FloorToInt(pos.x - half.x + 0.01f), x1 = Mathf.FloorToInt(pos.x + half.x - 0.01f);
                bool broke = false;
                for (int x = x0; x <= x1; x++)
                    if (G.Get(x, cy) == Cell.Cracked)
                    {
                        BreakCells(x, cy, Cell.Cracked);
                        broke = true;
                    }
                if (broke) { GameManager.I.Shake(0.25f); return; }

                st = St.Normal;
                vel.y = 0;
                Sfx.Play(Sfx.Pound);
                GameManager.I.Shake(0.35f);
                var feet = pos - new Vector2(0, half.y);
                Fx.Shockwave(feet, 2.5f);
                Fx.Dust(feet, 10);
                DamageEnemiesInRadius(feet + new Vector2(0, 0.5f), 2.6f, 5, DamageSource.Pound);
            }
        }

        // --- Forma 6: Gancho
        void TryHook()
        {
            var hooks = GameManager.I.Level.hooks;
            Vector2 top = pos + new Vector2(0, half.y);
            float best = HookRange;
            int idx = -1;
            for (int i = 0; i < hooks.Count; i++)
            {
                float d = Vector2.Distance(top, hooks[i]);
                if (d < best && hooks[i].y > pos.y) { best = d; idx = i; }
            }
            if (idx < 0) return;
            hookPoint = hooks[idx];
            st = St.Hook;
            hookTimer = HookMaxTime;
            vel = Vector2.zero;
            Sfx.Play(Sfx.Tink);
        }

        void Hook(float dt, float h, bool spHeld, bool shootDown)
        {
            if (h != 0) facing = h > 0 ? 1 : -1;
            hookTimer -= dt;
            var target = hookPoint - new Vector2(0, 0.35f + half.y * 2f - 0.6f);
            var np = Vector2.MoveTowards(pos, target, 14f * dt);
            var delta = np - pos;
            TileCollision.MoveX(G, ref pos, half, delta.x);
            TileCollision.MoveY(G, ref pos, half, delta.y, true, out _);
            if (shootDown) Shoot();
            if (jumpBuffer > 0)
            {
                st = St.Normal;
                hookNeedsRelease = true;
                DoJump(JumpVel * 0.9f);
                return;
            }
            if (!spHeld || hookTimer <= 0)
            {
                st = St.Normal;
                hookNeedsRelease = true;
                vel = new Vector2(0, 2f);
            }
        }

        // --- Forma 7: Impacto na parede
        void StartWallCharge()
        {
            cooldown = Forms.SpecialCooldown;
            st = St.WallCharge;
            stateTimer = WallChargeTime;
            Sfx.Play(Sfx.Dash);
        }

        void WallCharge(float dt)
        {
            stateTimer -= dt;
            vel.x = facing * WallChargeSpeed;
            vel.y = Mathf.Max(vel.y - Gravity * 0.4f * dt, -MaxFall);
            if (TileCollision.MoveX(G, ref pos, half, vel.x * dt))
            {
                // destrói portas reforçadas na frente
                int cx = Mathf.FloorToInt(pos.x + facing * (half.x + 0.05f));
                int y0 = Mathf.FloorToInt(pos.y - half.y + 0.01f), y1 = Mathf.FloorToInt(pos.y + half.y - 0.01f);
                bool broke = false;
                for (int y = y0; y <= y1; y++)
                    if (G.Get(cx, y) == Cell.Breakable) { BreakCells(cx, y, Cell.Breakable); broke = true; }
                if (broke) GameManager.I.Shake(0.4f);
                else { stateTimer = 0; GameManager.I.Shake(0.15f); }
            }
            if (TileCollision.MoveY(G, ref pos, half, vel.y * dt, false, out _)) vel.y = 0;
            DamageEnemiesInRadius(pos + new Vector2(facing * 0.5f, 0), 0.9f, 5, DamageSource.WallImpact);
            if (stateTimer <= 0) { st = St.Normal; vel.x = 0; }
        }

        void HurtState(float dt)
        {
            stateTimer -= dt;
            vel.x = Mathf.MoveTowards(vel.x, 0, 10 * dt);
            ApplyGravityAndMove(dt);
            if (stateTimer <= 0) st = St.Normal;
        }

        void EndSpecialStates()
        {
            if (half != StandHalf && CanStand()) SetHalf(StandHalf);
            if (half == StandHalf) st = St.Normal;
            else st = St.Slide;
        }

        void Noclip(float dt, bool inputOk)
        {
            var d = inputOk ? new Vector2(GameInput.Horizontal, GameInput.Vertical) : Vector2.zero;
            if (d.x != 0) facing = d.x > 0 ? 1 : -1;
            pos += d * 14f * dt;
            vel = Vector2.zero;
            if (inputOk && GameInput.ShootDown) Shoot();
            transform.position = new Vector3(pos.x, pos.y, 0);
            AnimateModel(dt);
        }

        // ------------------------------------------------------------------ util

        void DamageEnemiesInRadius(Vector2 c, float r, int dmg, DamageSource src)
        {
            var all = Enemy.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var e = all[i];
                if (e == null || !e.Alive) continue;
                if (TileCollision.CircleAabb(c, r, e.pos, e.half)) e.TakeHit(dmg, true, facing, src);
            }
        }

        /// <summary>Destrói um bloco e todos os blocos do mesmo tipo conectados (uma "porta" inteira).</summary>
        void BreakCells(int x, int y, Cell type)
        {
            var stack = new System.Collections.Generic.Stack<Vector2Int>();
            stack.Push(new Vector2Int(x, y));
            int n = 0;
            while (stack.Count > 0 && n < 64)
            {
                var c = stack.Pop();
                if (G.Get(c.x, c.y) != type) continue;
                var vis = G.VisualAt(c.x, c.y);
                var col = type == Cell.Breakable ? new Color(0.6f, 0.2f, 0.15f) : new Color(0.55f, 0.5f, 0.45f);
                Fx.Explode(LevelGrid.CellCenter(c.x, c.y), col, 6, 6f, 0.25f);
                G.Set(c.x, c.y, Cell.Empty);
                n++;
                stack.Push(new Vector2Int(c.x + 1, c.y));
                stack.Push(new Vector2Int(c.x - 1, c.y));
                stack.Push(new Vector2Int(c.x, c.y + 1));
                stack.Push(new Vector2Int(c.x, c.y - 1));
            }
        }

        void AnimateModel(float dt)
        {
            var pose = new CanModel.Pose
            {
                facing = facing,
                speedX = vel.x,
                grounded = grounded,
                sliding = st == St.Slide,
                shooting = shootAnim > 0,
                charging = st == St.Charge,
                chargeAmount = chargeTime / 0.4f,
                dashing = st == St.Dash || st == St.WallCharge,
                pounding = st == St.Pound,
                hooked = st == St.Hook,
                flipping = flipping && !grounded,
                hurt = st == St.Hurt,
                vy = vel.y,
                hookWorld = new Vector3(hookPoint.x, hookPoint.y, 0),
            };
            model.Animate(pose, dt);
            // origem do modelo = centro da caixa em pé; ao deslizar a caixa é menor e o modelo já está deitado
            model.transform.localPosition = Vector3.zero;
            bool blink = iframes > 0 && Mathf.FloorToInt(iframes * 20) % 2 == 0;
            model.SetVisible(!blink);
        }
    }
}

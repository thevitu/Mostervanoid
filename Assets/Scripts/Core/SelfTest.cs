using System.Collections;
using System.IO;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Teste automático (rode o executável com -mvtest [-mvout pasta]): simula entradas, mede pulos,
    /// testa o slide nas frestas e a luta com chefe, tira screenshots e fecha. Não altera o progresso salvo.
    /// </summary>
    public partial class GameManager
    {
        string testOut;

        void Log(string s) => Debug.Log("[MVTEST] " + s);

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            if (testOut == null) yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(testOut, name + ".png"));
            Log("shot " + name);
            yield return null;
        }

        IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Press(System.Action<bool> set)
        {
            set(true);
            yield return null;
            set(false);
        }

        IEnumerator SelfTest()
        {
            var args = System.Environment.GetCommandLineArgs();
            int oi = System.Array.IndexOf(args, "-mvout");
            testOut = oi >= 0 && oi + 1 < args.Length ? args[oi + 1] : Path.Combine(Application.persistentDataPath, "mvtest");
            Directory.CreateDirectory(testOut);
            Progress.ReadOnly = true;
            Progress.Reset();
            Application.runInBackground = true;   // não pausa quando a janela perde o foco

            // -mvonly coqueteleira: roda só essa seção
            int only = System.Array.IndexOf(args, "-mvonly");
            if (only >= 0 && only + 1 < args.Length)
            {
                yield return Frames(5);
                CloseMenu();
                switch (args[only + 1].ToLowerInvariant())
                {
                    case "coqueteleira": yield return TestShaker(); break;
                    case "ceu": yield return TestSky(); break;
                    case "vapor": yield return TestSteam(); break;
                    case "bule": yield return TestTeapot(); break;
                    case "suco": yield return TestJuice(); break;
                    case "jarra": yield return TestJar(); break;
                    case "minotauro": yield return TestMinotaur(); break;
                    case "fases": yield return TestStageShots(); break;
                    case "finais":
                        yield return TestStageShots(); yield return TestSteam(); yield return TestTeapot();
                        yield return TestJuice(); yield return TestJar(); yield return TestMinotaur(); break;
                    default: Log("seção desconhecida: " + args[only + 1]); break;
                }
                Log("DONE");
                yield return Wait(0.5f);
                Application.Quit();
                yield break;
            }

            yield return Frames(5);
            yield return Shot("00_title");
            CloseMenu();
            LoadStage("stage1", SpawnAt.Start);
            yield return Frames(5);
            yield return Wait(1.2f);
            yield return Shot("01_stage1_start");

            // --- altura do pulo
            float y0 = Player.Pos.y, maxY = y0;
            GameInput.SimJumpHeld = true;
            yield return Press(b => GameInput.SimJumpDown = b);
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { maxY = Mathf.Max(maxY, Player.Pos.y); yield return null; }
            GameInput.SimJumpHeld = false;
            Log($"jump height = {maxY - y0:0.00} (esperado ~3.8)");

            // --- correr para a direita e atirar
            GameInput.SimH = 1;
            for (int i = 0; i < 6; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.15f); }
            yield return Shot("02_run_shoot");
            GameInput.SimH = 0;
            Log($"after run x = {Player.Pos.x:0.0}");

            // --- blindado reflete tiro normal / tiro especial mata
            Cheats.invincible = true;
            Player.TeleportTo(new Vector2(74.5f, 2f));
            yield return Wait(0.6f);
            int armoredBefore = CountEnemies<EnemyArmored>();
            GameInput.SimH = 1; yield return null; GameInput.SimH = 0;
            yield return Press(b => GameInput.SimShootDown = b);
            yield return Wait(0.5f);
            yield return Shot("03_armored_deflect");
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.9f);
            Log($"armored: before={armoredBefore} after special={CountEnemies<EnemyArmored>()} (esperado 1 a menos)");
            yield return Shot("04_special_shot");

            // --- área de espinhos / checkpoint
            Player.TeleportTo(new Vector2(118.5f, 6f));
            yield return Wait(0.8f);
            yield return Shot("05_spikes");

            // --- chefe 1
            Player.TeleportTo(new Vector2(176.5f, 2f));
            GameInput.SimH = 1;
            yield return Wait(1.2f);
            GameInput.SimH = 0;
            Log($"boss1 active={bossActive} playerX={Player.Pos.x:0.0}");
            yield return Wait(2.5f);
            yield return Shot("06_boss1");
            for (int i = 0; i < 10; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.25f); }
            Log($"boss1 hp={(boss != null ? boss.hp : -1)}");
            if (boss != null) boss.Die();
            yield return Wait(1.5f);
            Log($"boss1 defeated={Progress.IsBossDefeated("stage1")} form1={Progress.IsFormUnlocked(1)} curForm={Player.Form} " +
                $"exitShutterOpen={Grid.Get(200, 3) == Cell.Empty} tp={Level.teleportPad.IsActive}");
            yield return Shot("07_boss1_defeated");

            // --- portal -> fase 2
            Player.TeleportTo(new Vector2(213.5f, 2f));
            yield return Wait(1.5f);
            Log($"stage after portal = {Level.info.id}");
            yield return Wait(1.0f);
            yield return Shot("08_stage2_start");

            // --- slide na fresta (fase 2, bolso direito y16, fresta x9..17)
            Cheats.invincible = false;
            Player.SetForm(1);
            Player.TeleportTo(new Vector2(21.5f, 16f));
            yield return Wait(0.5f);
            GameInput.SimH = -1;
            yield return Wait(1.0f);
            Log($"standing walk-left x = {Player.Pos.x:0.00} (esperado >= 18, bloqueado pela fresta)");
            yield return Wait(3.1f); // recarga
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.3f);
            yield return Shot("09_slide_tunnel");
            yield return Wait(1.0f);
            GameInput.SimH = 0;
            Log($"after slide x = {Player.Pos.x:0.00} y = {Player.Pos.y - Player.Half.y:0.00} (esperado x < 9, y = 16)");

            // --- super pulo (forma 2)
            Progress.UnlockForm(2);
            Player.SetForm(2);
            Player.TeleportTo(new Vector2(12.5f, 2f));
            Cheats.noCooldown = true;
            yield return Wait(0.5f);
            y0 = Player.Pos.y; maxY = y0;
            GameInput.SimSpecialHeld = true;
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.5f);
            yield return Shot("10_superjump_charge");
            GameInput.SimSpecialHeld = false;
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { maxY = Mathf.Max(maxY, Player.Pos.y); yield return null; }
            Log($"super jump height = {maxY - y0:0.00} (esperado ~11)");

            // --- outras formas em sequência (visual)
            for (int f = 3; f < Forms.Count; f++)
            {
                Progress.UnlockForm(f);
                Player.SetForm(f);
                Player.TeleportTo(new Vector2(12.5f, 2f));
                yield return Wait(0.3f);
                GameInput.SimJumpHeld = true;
                yield return Press(b => GameInput.SimJumpDown = b);
                yield return Wait(0.3f);
                yield return Press(b => GameInput.SimSpecialDown = b);
                if (f == 4) yield return Press(b => GameInput.SimJumpDown = b);
                yield return Wait(0.1f);
                yield return Shot($"11_form{f}");
                GameInput.SimJumpHeld = false;
                yield return Wait(0.8f);
            }

            // --- chefe 2: Tanque Gaseificador (frente blindada, slide por baixo, tiro nas costas)
            Cheats.invincible = true;
            Cheats.noCooldown = false;
            Progress.UnlockForm(1);
            Player.SetForm(1);
            LoadStage("stage2", SpawnAt.BossWarp);
            yield return Wait(0.5f);
            Player.TeleportTo(new Vector2(6.5f, 87f));
            for (float t = 0; boss == null && t < 4f; t += Time.deltaTime) yield return null;
            var tank = boss as BossWineBarrel;
            Log($"boss2 = {(tank != null ? tank.displayName : "null")}");
            if (tank != null)
            {
                Player.TeleportTo(new Vector2(2.5f, 87f));
                GameInput.SimH = 1; yield return null; GameInput.SimH = 0;
                for (float t = 0; tank.State != BossWineBarrel.S.Shots && t < 8f; t += Time.deltaTime) yield return null;
                int hp0 = tank.hp;
                for (int i = 0; i < 3; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.3f); }
                Log($"tiro na FRENTE: hp {hp0} -> {tank.hp} (esperado igual)");
                yield return Shot("12_boss2");
                for (float t = 0; tank.State != BossWineBarrel.S.Charge && t < 8f; t += Time.deltaTime) yield return null;
                Cheats.invincible = false;
                Player.Heal();
                int php = Player.Hp;
                for (float t = 0; Mathf.Abs(tank.pos.x - Player.Pos.x) > tank.half.x + 2.2f && t < 4f; t += Time.deltaTime) yield return null;
                yield return Press(b => GameInput.SimSpecialDown = b);
                yield return Wait(0.15f);
                yield return Shot("13_boss2_slide_under");
                yield return Wait(0.7f);
                Log($"slide sob o tanque: hp jogador {php} -> {Player.Hp} (esperado igual), jogador x={Player.Pos.x:0.0} tanque x={tank.pos.x:0.0} (jogador atrás)");
                Cheats.invincible = true;
                for (float t = 0; tank.State != BossWineBarrel.S.Stunned && t < 4f; t += Time.deltaTime) yield return null;
                float toTank = Mathf.Sign(tank.pos.x - Player.Pos.x);
                GameInput.SimH = toTank; yield return null; GameInput.SimH = 0;
                hp0 = tank.hp;
                for (int i = 0; i < 6; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.25f); }
                Log($"tiro nas COSTAS (atordoado): hp {hp0} -> {tank.hp} (esperado menor)");
                yield return Shot("14_boss2_core");
            }

            // --- voador: colide com paredes e não se afasta mais que 7 do ponto de nascimento
            LoadStage("stage1", SpawnAt.Start);
            Cheats.invincible = true;
            EnemyFlyer fly = null;
            foreach (var e in Enemy.All) if (e is EnemyFlyer f) { fly = f; break; }
            if (fly != null)
            {
                Player.TeleportTo(new Vector2(fly.pos.x - 2f, 2f));
                yield return Wait(2.5f);
                float near = fly.DistanceFromHome;
                Player.TeleportTo(new Vector2(fly.pos.x + 40f, 2f));
                yield return Wait(4f);
                Log($"voador: perto do jogador afastou {near:0.0} de casa; com o jogador longe ficou a {fly.DistanceFromHome:0.0} (esperado <= 7.5)");
            }

            // --- Vinhedo: Garrafa de Refri (Forma 2 / super pulo até as plataformas altas)
            Progress.UnlockForm(2);
            LoadStage("stage3", SpawnAt.Start);
            yield return Wait(1.0f);
            yield return Shot("20_vineyard_start");
            Player.TeleportTo(new Vector2(75.5f, 10f));
            yield return Wait(0.8f);
            yield return Shot("21_vineyard_plateau");
            LoadStage("stage3", SpawnAt.BossWarp);
            Player.SetForm(2);
            yield return Wait(0.3f);
            Player.TeleportTo(new Vector2(124.5f, 2f));
            for (float t = 0; boss == null && t < 4f; t += Time.deltaTime) yield return null;
            var soda = boss as BossSodaBottle;
            Log($"boss3 = {(soda != null ? soda.displayName : "null")}");
            if (soda != null)
            {
                for (float t = 0; soda.State != BossSodaBottle.S.Perch && t < 6f; t += Time.deltaTime) yield return null;
                // super pulo de verdade até a plataforma alta do lado esquerdo
                Cheats.noCooldown = true;
                Player.TeleportTo(new Vector2(124.5f, 2f));
                yield return Wait(0.3f);
                GameInput.SimSpecialHeld = true;
                yield return Press(b => GameInput.SimSpecialDown = b);
                yield return Wait(0.4f);
                GameInput.SimSpecialHeld = false;
                yield return Wait(1.3f);
                Log($"super pulo até a plataforma: pés em y={Player.Pos.y - Player.Half.y:0.0} (plataforma alta = 11)");
                yield return Shot("22_soda_perch");
                // espera ela pousar de novo num poleiro e atira de cima da plataforma
                for (float t = 0; soda.State == BossSodaBottle.S.Perch && t < 6f; t += Time.deltaTime) yield return null;
                for (float t = 0; soda.State != BossSodaBottle.S.Perch && t < 8f; t += Time.deltaTime) yield return null;
                yield return Wait(0.4f);
                int hp0 = soda.hp;
                GameInput.SimH = Mathf.Sign(soda.pos.x - Player.Pos.x); yield return null; GameInput.SimH = 0;
                for (int i = 0; i < 8; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.2f); }
                Log($"tiros da plataforma alta: hp garrafa {hp0} -> {soda.hp} (esperado menor)");
                Cheats.noCooldown = false;
            }

            yield return TestShaker();
            yield return TestSky();

            // --- verificação: estacas e selva
            LoadStage("stage1", SpawnAt.Start);
            Cheats.invincible = true;
            Player.TeleportTo(new Vector2(117.5f, 6f));
            yield return Wait(0.8f);
            yield return Shot("15_stakes");
            Player.TeleportTo(new Vector2(30.5f, 2f));
            GameInput.SimH = 1; yield return null; GameInput.SimH = 0;
            Player.SetForm(0);
            yield return Wait(0.3f);
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.12f);
            yield return Press(b => GameInput.SimShootDown = b);
            yield return Wait(0.1f);
            yield return Shot("16_energy_shots");

            // --- menus
            OpenMenu(Menu.Forms);
            yield return Frames(3);
            yield return Shot("17_menu_forms");
            CloseMenu();
            OpenMenu(Menu.Cheats);
            yield return Frames(3);
            yield return Shot("18_menu_cheats");
            CloseMenu();

            Log("DONE");
            yield return Wait(0.5f);
            Application.Quit();
        }


        IEnumerator TestShaker()
        {
            Cheats.invincible = true;
            // --- Geleira: Coqueteleira (Forma 3 / dash atravessa o muro de gelo)
            Progress.UnlockForm(3);
            LoadStage("stage4", SpawnAt.Start);
            yield return Wait(1.0f);
            yield return Shot("23_glacier_start");
            LoadStage("stage4", SpawnAt.BossWarp);
            Player.SetForm(3);
            yield return Wait(0.3f);
            Player.TeleportTo(new Vector2(127.5f, 2f));
            for (float t = 0; boss == null && t < 4f; t += Time.deltaTime) yield return null;
            var shaker = boss as BossShaker;
            Player.TeleportTo(new Vector2(130f, 2f));   // espaço para o dash até a parede da sala
            Log($"boss4 = {(shaker != null ? shaker.displayName : "null")}");
            if (shaker != null)
            {
                bool tested = false;
                for (float t = 0; t < 60f && !tested; t += Time.deltaTime)
                {
                    if (shaker.State == BossShaker.S.Shake)
                    {
                        // jogada sensata: fica longe do chefe, do outro lado da sala
                        Player.TeleportTo(new Vector2(shaker.pos.x < 134f ? 141f : 127f, 2f));
                        GameInput.SimH = Mathf.Sign(shaker.pos.x - Player.Pos.x); yield return null; GameInput.SimH = 0;
                        while (shaker.State == BossShaker.S.Shake) yield return null;
                        // espera o muro chegar perto e dá o dash na direção dele
                        Cheats.invincible = false;
                        Player.Heal();
                        int php = Player.Hp;
                        Projectile wall = null;
                        foreach (var pr in LevelRoot.GetComponentsInChildren<Projectile>()) if (pr.name == "EnemyShot" && pr.transform.childCount >= 4) wall = pr;
                        for (float w = 0; wall != null && Mathf.Abs(wall.transform.position.x - Player.Pos.x) > 1.6f && w < 4f; w += Time.deltaTime) yield return null;
                        if (wall != null)
                        {
                            Log($"  antes do dash: jogador x={Player.Pos.x:0.00} muro x={wall.transform.position.x:0.00} chefe x={shaker.pos.x:0.00}");
                            GameInput.SimH = Mathf.Sign(wall.transform.position.x - Player.Pos.x); yield return null; GameInput.SimH = 0;
                            yield return Press(b => GameInput.SimSpecialDown = b);
                            yield return Wait(0.06f);
                            yield return Shot("24_dash_through_wall");
                            for (int k = 0; k < 6; k++)
                            {
                                Log($"  t+{(k + 1) * 0.1f:0.0}s hp={Player.Hp} jogador x={Player.Pos.x:0.00} muro x={(wall != null ? wall.transform.position.x : -1):0.00} chefe={shaker.State} x={shaker.pos.x:0.00}");
                                yield return Wait(0.1f);
                            }
                            Log($"dash pelo muro de gelo: hp jogador {php} -> {Player.Hp} (esperado igual)");
                            tested = true;
                        }
                        Cheats.invincible = true;
                    }
                    yield return null;
                }
                if (!tested) Log("muro de gelo não apareceu a tempo");
                yield return Shot("25_shaker");
            }

        }


        /// <summary>Céu Borbulhante: degrau sob teto só com pulo duplo e muro de espuma do chefe.</summary>
        IEnumerator TestSky()
        {
            Cheats.invincible = true;
            for (int f = 1; f <= 4; f++) Progress.UnlockForm(f);
            LoadStage("stage5", SpawnAt.Start);
            Player.SetForm(4);
            yield return Wait(1.0f);
            yield return Shot("30_sky_start");

            // degrau 1: ilha 5 acima e 6 à frente, teto baixo (super pulo não alcança)
            Player.TeleportTo(new Vector2(12.6f, 6f));
            yield return Wait(0.3f);
            GameInput.SimH = 1; GameInput.SimJumpHeld = true;
            yield return Press(b => GameInput.SimJumpDown = b);
            yield return Wait(0.38f);
            yield return Press(b => GameInput.SimJumpDown = b);
            yield return Wait(0.3f);
            yield return Shot("31_double_jump");
            yield return Wait(1.0f);
            GameInput.SimH = 0; GameInput.SimJumpHeld = false;
            Log($"pulo duplo no degrau: pés y={Player.Pos.y - Player.Half.y:0.0} x={Player.Pos.x:0.0} (esperado y=11, x>=18)");

            // mesmo degrau com super pulo: não deve alcançar
            Player.SetForm(2);
            Cheats.noCooldown = true;
            Player.TeleportTo(new Vector2(12.6f, 6f));
            yield return Wait(0.3f);
            GameInput.SimSpecialHeld = true;
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.4f);
            GameInput.SimSpecialHeld = false;
            GameInput.SimH = 1;
            yield return Wait(1.6f);
            GameInput.SimH = 0;
            Cheats.noCooldown = false;
            Log($"super pulo no mesmo degrau: pés y={Player.Pos.y - Player.Half.y:0.0} (esperado: NÃO chegar em 11)");

            // chefe
            LoadStage("stage5", SpawnAt.BossWarp);
            Player.SetForm(4);
            yield return Wait(0.3f);
            Player.TeleportTo(new Vector2(144.5f, 14f));
            for (float t = 0; boss == null && t < 4f; t += Time.deltaTime) yield return null;
            var champ = boss as BossChampagne;
            Log($"boss5 = {(champ != null ? champ.displayName : "null")}");
            if (champ == null) yield break;
            yield return Wait(2f);
            yield return Shot("32_champagne");
            bool tested = false;
            for (float t = 0; t < 60f && !tested; t += Time.deltaTime)
            {
                if (champ.State == BossChampagne.S.FoamPrep)
                {
                    while (champ.State == BossChampagne.S.FoamPrep) yield return null;
                    yield return Frames(3);
                    Projectile wall = null;
                    foreach (var pr in LevelRoot.GetComponentsInChildren<Projectile>()) if (pr.name == "EnemyShot" && pr.transform.childCount >= 4) wall = pr;
                    if (wall != null)
                    {
                        // fica no chão, no caminho do muro, e pula duas vezes quando ele chega
                        Player.TeleportTo(new Vector2(Mathf.Clamp(wall.transform.position.x + Mathf.Sign(151.5f - wall.transform.position.x) * 7f, 145f, 160f), 14f));
                        Cheats.invincible = false;
                        Player.Heal();
                        int php = Player.Hp;
                        for (float w = 0; Mathf.Abs(wall.transform.position.x - Player.Pos.x) > 3.8f && w < 4f; w += Time.deltaTime) yield return null;
                        Log($"  antes: jogador x={Player.Pos.x:0.00} y={Player.Pos.y - Player.Half.y:0.00} muro x={wall.transform.position.x:0.00} y={wall.transform.position.y:0.00} hp={Player.Hp}");
                        GameInput.SimJumpHeld = true;
                        yield return Press(b => GameInput.SimJumpDown = b);
                        yield return Wait(0.25f);
                        yield return Press(b => GameInput.SimJumpDown = b);
                        yield return Wait(0.15f);
                        yield return Shot("33_double_jump_over_foam");
                        for (int k = 0; k < 8; k++)
                        {
                            Log($"  t+{k * 0.12f:0.00} jogador x={Player.Pos.x:0.00} pés y={Player.Pos.y - Player.Half.y:0.00} muro x={(wall != null ? wall.transform.position.x : -1):0.00} hp={Player.Hp} chefe={champ.State} ({champ.pos.x:0.0},{champ.pos.y:0.0})");
                            yield return Wait(0.12f);
                        }
                        GameInput.SimJumpHeld = false;
                        Log($"pulo duplo sobre o muro de espuma: hp jogador {php} -> {Player.Hp} (esperado igual)");
                        Cheats.invincible = true;
                        tested = true;
                    }
                }
                yield return null;
            }
            if (!tested) Log("muro de espuma não apareceu a tempo");

            // rajada completa: reage a cada muro que se aproxima com um pulo duplo
            Cheats.invincible = true;
            for (float t = 0; champ.State != BossChampagne.S.FoamPrep && t < 40f; t += Time.deltaTime) yield return null;
            if (champ.State == BossChampagne.S.FoamPrep)
            {
                Player.TeleportTo(new Vector2(151.5f, 14f));   // posiciona durante o aviso (chacoalhar)
                while (champ.State == BossChampagne.S.FoamPrep) yield return null;
                Cheats.invincible = false;
                Player.Heal();
                int php = Player.Hp, jumps = 0;
                yield return Shot("34_barrage");
                int lastHp = Player.Hp;
                for (float t = 0; (champ.State == BossChampagne.S.Barrage || HasFoam()) && t < 14f; t += Time.deltaTime)
                {
                    if (Player.Hp != lastHp)
                    {
                        Log($"  dano em t={t:0.00}: jogador x={Player.Pos.x:0.00} pés y={Player.Pos.y - Player.Half.y:0.00} chefe={champ.State} ({champ.pos.x:0.0},{champ.pos.y:0.0})");
                        lastHp = Player.Hp;
                    }
                    bool grounded = Player.Pos.y - Player.Half.y < 14.05f;
                    if (grounded && FoamApproaching(3.8f))
                    {
                        jumps++;
                        GameInput.SimJumpHeld = true;
                        yield return Press(b => GameInput.SimJumpDown = b);
                        yield return Wait(0.25f);
                        yield return Press(b => GameInput.SimJumpDown = b);
                        yield return Wait(0.5f);
                        GameInput.SimJumpHeld = false;
                    }
                    yield return null;
                }
                Log($"rajada de espuma ({jumps} pulos duplos): hp jogador {php} -> {Player.Hp} (esperado igual)");
                Cheats.invincible = true;
            }

            // acertar a garrafa no alto atirando no meio do pulo duplo (sala sem plataformas)
            for (float t = 0; champ.State != BossChampagne.S.Hover && t < 10f; t += Time.deltaTime) yield return null;
            yield return Wait(0.8f);
            int bhp = champ.hp;
            for (int tries = 0; tries < 3; tries++)
            {
                float side = champ.pos.x > 151.5f ? -1 : 1;
                Player.TeleportTo(new Vector2(champ.pos.x + side * 4f, 14f));
                GameInput.SimH = -side; yield return null; GameInput.SimH = 0;
                GameInput.SimJumpHeld = true;
                yield return Press(b => GameInput.SimJumpDown = b);
                yield return Wait(0.35f);
                yield return Press(b => GameInput.SimJumpDown = b);
                yield return Wait(0.3f);
                yield return Press(b => GameInput.SimShootDown = b);
                yield return Wait(0.12f);
                yield return Press(b => GameInput.SimShootDown = b);
                GameInput.SimJumpHeld = false;
                yield return Wait(0.8f);
            }
            Log($"tiros no meio do pulo duplo: hp garrafa {bhp} -> {champ.hp} (esperado menor)");
        }

        static bool IsFoam(Projectile pr) => pr.name == "EnemyShot" && pr.transform.Find("Shell") == null && pr.transform.childCount >= 2;

        bool HasFoam()
        {
            foreach (var pr in LevelRoot.GetComponentsInChildren<Projectile>()) if (IsFoam(pr)) return true;
            return false;
        }

        /// <summary>Algum muro/onda vindo na direção do jogador a menos de 'dist'?</summary>
        bool FoamApproaching(float dist)
        {
            foreach (var pr in LevelRoot.GetComponentsInChildren<Projectile>())
            {
                if (!IsFoam(pr)) continue;
                float d = pr.transform.position.x - Player.Pos.x;
                if (Mathf.Abs(d) < dist && Mathf.Abs(d) > 0.9f && Mathf.Sign(pr.Velocity.x) == -Mathf.Sign(d)) return true;
            }
            return false;
        }

        void UnlockUpTo(int f) { for (int i = 1; i <= f; i++) Progress.UnlockForm(i); }

        IEnumerator BossFight(string stage, float triggerX, float floorY)
        {
            LoadStage(stage, SpawnAt.BossWarp);
            yield return Wait(0.3f);
            Player.TeleportTo(new Vector2(triggerX, floorY));
            for (float t = 0; boss == null && t < 4f; t += Time.deltaTime) yield return null;
            yield return Wait(1.5f);
        }

        IEnumerator TestStageShots()
        {
            Cheats.invincible = true;
            UnlockUpTo(7);
            foreach (var id in new[] { "stage6", "stage7", "stage8", "stage9" })
            {
                LoadStage(id, SpawnAt.Start);
                yield return Wait(1.2f);
                yield return Shot("40_" + id + "_start");
            }
        }

        /// <summary>Jato de vapor: andando não passa; com o dash passa.</summary>
        IEnumerator TestSteam()
        {
            Cheats.invincible = true;
            UnlockUpTo(5);
            LoadStage("stage6", SpawnAt.Start);
            Player.SetForm(3);
            Player.TeleportTo(new Vector2(20.5f, 46f));    // corredor do andar D (vapor empurra para a direita em x16..17)
            yield return Wait(0.3f);
            GameInput.SimH = -1;
            yield return Wait(2.0f);
            Log($"vapor andando: jogador x={Player.Pos.x:0.00} (esperado > 17, não passa)");
            yield return Shot("41_steam_block");
            yield return Press(b => GameInput.SimSpecialDown = b);
            yield return Wait(0.6f);
            GameInput.SimH = 0;
            Log($"vapor com dash: jogador x={Player.Pos.x:0.00} (esperado < 16, passou)");
        }

        /// <summary>Bule: tiro reflete; impacto no chão perto dele causa dano.</summary>
        IEnumerator TestTeapot()
        {
            Cheats.invincible = true;
            UnlockUpTo(5);
            yield return BossFight("stage6", 6.5f, 2f);
            var tp = boss as BossTeapot;
            Log($"boss6 = {(tp != null ? tp.displayName : "null")}");
            if (tp == null) yield break;
            yield return Shot("42_teapot");
            Player.SetForm(0);
            int hp0 = tp.hp;
            for (int i = 0; i < 3; i++)
            {
                GameInput.SimH = Mathf.Sign(tp.pos.x - Player.Pos.x); yield return null; GameInput.SimH = 0;
                yield return Press(b => GameInput.SimShootDown = b);
                yield return Wait(0.4f);
            }
            Log($"bule: tiros {hp0} -> {tp.hp} (esperado igual: porcelana reflete)");
            Player.SetForm(5);
            hp0 = tp.hp;
            for (int i = 0; i < 2; i++)
            {
                for (float t = 0; tp.State == BossTeapot.S.Hop && t < 3f; t += Time.deltaTime) yield return null;
                Player.TeleportTo(new Vector2(Mathf.Clamp(tp.pos.x + 2.2f * (i == 0 ? -1 : 1), 2, 17), 2f));
                yield return Wait(0.1f);
                GameInput.SimJumpHeld = true;
                yield return Press(b => GameInput.SimJumpDown = b);
                yield return Wait(0.35f);
                yield return Press(b => GameInput.SimSpecialDown = b);
                GameInput.SimJumpHeld = false;
                yield return Wait(0.8f);
                if (i == 0) yield return Shot("43_teapot_pound");
                yield return Wait(2.6f);                                  // recarga do impacto
            }
            Log($"bule: impactos no chão {hp0} -> {tp.hp} (esperado menor)");
        }

        /// <summary>Caixinha de Suco: pendurado no gancho durante a enchente não toma dano.</summary>
        IEnumerator TestJuice()
        {
            Cheats.invincible = true;
            UnlockUpTo(6);
            yield return BossFight("stage7", 6.5f, 63f);
            var jb = boss as BossJuiceBox;
            Log($"boss7 = {(jb != null ? jb.displayName : "null")}");
            if (jb == null) yield break;
            Player.SetForm(6);
            bool tested = false, warnShot = false;
            for (float t = 0; t < 40f && !tested; t += Time.deltaTime)
            {
                if (!warnShot && jb.State == BossJuiceBox.S.FloodWarn && jb.StateTime < 0.6f)
                {
                    warnShot = true;
                    yield return Shot("44a_juice_warning");
                }
                if (jb.State == BossJuiceBox.S.FloodWarn && jb.StateTime < 0.35f)
                {
                    Player.TeleportTo(new Vector2(3.5f, 63f));            // sob o gancho (3, 68)
                    Cheats.invincible = false;
                    Player.Heal();
                    int php = Player.Hp;
                    GameInput.SimSpecialHeld = true;
                    GameInput.SimJumpHeld = true;
                    yield return Press(b => GameInput.SimJumpDown = b);
                    yield return Wait(0.6f);
                    GameInput.SimJumpHeld = false;
                    Log($"  pendurado? pés y={Player.Pos.y - Player.Half.y:0.00} (gancho em 68.5)");
                    yield return Shot("44_juice_flood_hook");
                    while (jb.State == BossJuiceBox.S.Flood) yield return null;
                    GameInput.SimSpecialHeld = false;
                    Log($"suco: enchente pendurado no gancho: hp {php} -> {Player.Hp} (esperado igual)");
                    Cheats.invincible = true;
                    tested = true;
                }
                yield return null;
            }
            if (!tested) Log("enchente não aconteceu a tempo");
            int bhp = jb.hp;
            Player.SetForm(6);
            Player.TeleportTo(new Vector2(jb.pos.x - 4f, 63f));
            GameInput.SimH = 1; yield return null; GameInput.SimH = 0;
            for (int i = 0; i < 4; i++) { yield return Press(b => GameInput.SimShootDown = b); yield return Wait(0.25f); }
            Log($"suco: tiros {bhp} -> {jb.hp} (esperado menor)");
        }

        /// <summary>Jarra: o copo prende; o impacto na parede quebra.</summary>
        IEnumerator TestJar()
        {
            Cheats.invincible = false;
            Cheats.infiniteLife = true;
            UnlockUpTo(7);
            yield return BossFight("stage8", 145.5f, 7f);
            var jar = boss as BossGlassJar;
            Log($"boss8 = {(jar != null ? jar.displayName : "null")}");
            if (jar == null) yield break;
            Player.SetForm(7);
            bool trapped = false;
            for (float t = 0; t < 40f && !trapped; t += Time.deltaTime)
            {
                if (jar.PlayerTrapped) trapped = true;
                yield return null;
            }
            Log($"jarra: jogador preso no copo = {trapped}");
            if (trapped)
            {
                yield return Shot("45_jar_trap");
                yield return Wait(0.3f);
                GameInput.SimH = 1; yield return null; GameInput.SimH = 0;
                yield return Press(b => GameInput.SimSpecialDown = b);
                yield return Wait(0.5f);
                Log($"jarra: após o impacto na parede, ainda preso = {jar.PlayerTrapped} (esperado False)");
                yield return Shot("46_jar_free");
            }
            Cheats.infiniteLife = false;
            Cheats.invincible = true;
        }

        /// <summary>Minotauro: pulo sobre a investida, 3 fases de fúria e o final.</summary>
        IEnumerator TestMinotaur()
        {
            Cheats.invincible = true;
            UnlockUpTo(7);
            yield return BossFight("stage9", 217.5f, 7f);
            var mino = boss as BossMinotaur;
            if (mino != null) Player.TeleportTo(new Vector2(mino.pos.x - 9f, 7f));   // espaço para a investida
            Log($"boss9 = {(mino != null ? mino.displayName : "null")} hp={(mino != null ? mino.hp : 0)}");
            if (mino == null) yield break;
            Player.SetForm(0);
            yield return Shot("47_minotaur");
            // pula por cima de uma investida
            bool tested = false;
            for (float t = 0; t < 20f && !tested; t += Time.deltaTime)
            {
                if (mino.State == BossMinotaur.S.Charge)
                {
                    Cheats.invincible = false;
                    Player.Heal();
                    int php = Player.Hp;
                    for (float w = 0; Mathf.Abs(mino.pos.x - Player.Pos.x) > mino.half.x + 2.4f && mino.State == BossMinotaur.S.Charge && w < 3f; w += Time.deltaTime) yield return null;
                    float toward = Mathf.Sign(mino.pos.x - Player.Pos.x);
                    GameInput.SimJumpHeld = true;
                    GameInput.SimH = toward;                              // pula na direção dele para passar por cima
                    yield return Press(b => GameInput.SimJumpDown = b);
                    yield return Wait(0.2f);
                    yield return Shot("48_jump_over_charge");
                    for (int k = 0; k < 7; k++)
                    {
                        Log($"  t+{k * 0.1f:0.0} jogador ({Player.Pos.x:0.0},{Player.Pos.y - Player.Half.y:0.0}) minotauro x={mino.pos.x:0.0} estado={mino.State} hp={Player.Hp}");
                        yield return Wait(0.1f);
                    }
                    GameInput.SimJumpHeld = false;
                    GameInput.SimH = 0;
                    Log($"minotauro: pulo sobre a investida: hp {php} -> {Player.Hp} (esperado igual)");
                    Cheats.invincible = true;
                    tested = true;
                }
                yield return null;
            }
            // fases de fúria
            for (int ph = 2; ph <= 3; ph++)
            {
                while (mino.Phase < ph && mino.hp > 1) { mino.TakeHit(5, true, 1); yield return Wait(0.12f); }
                Log($"minotauro: fase {mino.Phase} com hp={mino.hp}");
                yield return Wait(2.5f);
                yield return Shot($"49_minotaur_phase{ph}");
                yield return Wait(4f);
            }
            mino.Die();
            yield return Wait(6.5f);
            Log($"final: menu={menu}");
            yield return Shot("50_ending");
            yield return Wait(6f);
            yield return Shot("51_ending_credits");
        }

        static int CountEnemies<T>() where T : Enemy
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e is T && e.Alive) n++;
            return n;
        }
    }
}

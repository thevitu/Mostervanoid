using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    public class Cheats
    {
        public bool invincible, noCooldown, oneHitKill, noclip, infiniteLife;
    }

    public enum Menu { None, Title, Forms, Teleport, Cheats, Ending }

    /// <summary>
    /// Núcleo do jogo: cria câmera/luz/jogador, carrega fases, controla chefes, checkpoints, teleportes e menus.
    /// Tudo é montado por código — basta haver um GameObject com este componente (o Bootstrap cria se não houver).
    /// </summary>
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public PlayerController Player { get; private set; }
        public Level Level { get; private set; }
        public LevelGrid Grid => Level?.grid;
        public Transform LevelRoot => Level != null ? Level.root : transform;
        public readonly Cheats Cheats = new Cheats();

        public Menu menu = Menu.Title;
        public bool InputAllowed => menu == Menu.None && Time.frameCount > menuClosedFrame + 1 && !transitioning;

        CameraRig camRig;
        Light sun;
        Transform world;

        int menuClosedFrame;
        bool transitioning;

        string respawnStage;
        Vector2 respawnPos;

        bool bossActive;
        Boss boss;
        float exitMsgCooldown;
        Coroutine bossCo, flowCo;

        // ---------------------------------------------------------------- inicialização

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Progress.Load();
            Sfx.Init(gameObject);

            camRig = CameraRig.Create();
            var sunGo = new GameObject("Sun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            QualitySettings.shadowDistance = 45f;

            world = new GameObject("World").transform;
            Player = PlayerController.Create(world);
            Player.gameObject.SetActive(false);

            // fundo da tela de título: fase 1 sem jogador
            LoadStage(Stages.All[0].id, SpawnAt.Start);
            Player.gameObject.SetActive(false);
            OpenMenu(Menu.Title);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mvtest") >= 0) StartCoroutine(SelfTest());
        }

        void OnDestroy()
        {
            if (I == this) { I = null; Time.timeScale = 1f; }
        }

        public enum SpawnAt { Start, Respawn, Teleport, BossWarp }

        public void LoadStage(string id, SpawnAt at, Vector2? custom = null)
        {
            var info = Stages.Get(id);
            if (info == null) { Debug.LogError("Fase inexistente: " + id); return; }

            if (bossCo != null) StopCoroutine(bossCo);
            if (flowCo != null) StopCoroutine(flowCo);
            bossCo = flowCo = null;
            transitioning = false;
            if (Level != null)
            {
                Level.root.gameObject.SetActive(false);   // dispara OnDisable (limpa Enemy.All) na hora
                Destroy(Level.root.gameObject);
            }
            Projectile.PlayerShots = 0;
            bossActive = false;
            boss = null;
            camRig.lockCenter = null;
            camRig.extraDistance = 0;

            Level = LevelBuilder.Build(info, world);
            Backdrop.ApplyAtmosphere(info.theme, camRig.Cam, sun);

            Vector2 spawn = Level.start;
            switch (at)
            {
                case SpawnAt.Respawn: spawn = custom ?? Level.start; break;
                case SpawnAt.Teleport: spawn = Level.hasTeleport ? Level.teleportPos : Level.start; break;
                case SpawnAt.BossWarp: spawn = Level.bossWarp ?? Level.start; break;
            }
            respawnStage = id;
            respawnPos = spawn;
            foreach (var cp in Level.checkpoints)
                if (Vector2.Distance(cp.pos, spawn) < 0.5f) ActivateCheckpoint(cp, false);

            Player.gameObject.SetActive(true);
            Player.Spawn(spawn);
            Player.SetForm(Progress.CurrentForm);
            camRig.Snap(Player.Pos, Level);
            if (at == SpawnAt.Teleport) Sfx.Play(Sfx.Teleport);
            ShowBanner(info.title, 2.5f);
        }

        // ---------------------------------------------------------------- loop

        void Update()
        {
            bannerTime -= Time.unscaledDeltaTime;
            toastTime -= Time.unscaledDeltaTime;
            exitMsgCooldown -= Time.unscaledDeltaTime;

            HandleMenus();
            if (menu != Menu.None || Level == null) return;

            var p = Player;
            if (p.IsDead || transitioning) return;

            // gatilho da sala do chefe
            if (Level.hasBoss && !bossActive && !Progress.IsBossDefeated(Level.info.id))
                foreach (var t in Level.triggers)
                    if (TileCollision.AabbOverlap(p.Pos, p.Half, LevelGrid.CellCenter(t.x, t.y), new Vector2(0.5f, 0.5f)))
                    {
                        bossCo = StartCoroutine(BossIntro());
                        break;
                    }

            // checkpoints
            foreach (var cp in Level.checkpoints)
                if (!cp.active && Mathf.Abs(p.Pos.x - cp.pos.x) < 0.9f && Mathf.Abs(p.Pos.y - p.Half.y - cp.pos.y) < 2f)
                    ActivateCheckpoint(cp, true);

            // teleporte (vira ponto de retorno)
            if (Level.hasTeleport && Level.teleportPad.IsActive && Near(p, Level.teleportPos, 1.2f) && respawnPos != Level.teleportPos)
            {
                respawnPos = Level.teleportPos;
                respawnStage = Level.info.id;
                Toast("Ponto de retorno salvo. Aperte M para teleportar.");
            }

            // portal para a próxima fase
            if (Level.hasExit && Level.exitPortal.IsActive && Near(p, Level.exitPos, 1.0f))
            {
                if (!string.IsNullOrEmpty(Level.info.next) && Stages.Get(Level.info.next) != null)
                    flowCo = StartCoroutine(GoToStage(Level.info.next));
                else if (exitMsgCooldown <= 0)
                {
                    exitMsgCooldown = 3f;
                    Toast("Novas fases em breve! Use M para voltar aos teleportes.");
                }
            }
        }

        void LateUpdate()
        {
            if (Level != null && Player != null)
                camRig.Follow(Player, Level, Time.unscaledDeltaTime);
        }

        static bool Near(PlayerController p, Vector2 feet, float r) =>
            Mathf.Abs(p.Pos.x - feet.x) < r && Mathf.Abs(p.Pos.y - p.Half.y - feet.y) < 1.5f;

        void ActivateCheckpoint(Checkpoint cp, bool announce)
        {
            foreach (var other in Level.checkpoints) other.active = false;
            cp.active = true;
            foreach (var other in Level.checkpoints)
                if (other.flag != null) other.flag.sharedMaterial.SetColor(Mat.BaseColor, other.active ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.85f, 0.15f, 0.15f));
            respawnPos = cp.pos;
            respawnStage = Level.info.id;
            if (announce) { Toast("Checkpoint!"); Sfx.Play(Sfx.Menu); }
        }

        public void Shake(float a) => camRig.Shake(a);

        // ---------------------------------------------------------------- chefes

        IEnumerator BossIntro()
        {
            bossActive = true;
            for (int i = 0; i < Level.entryShutters.Count; i++)
            {
                var c = Level.entryShutters[i];
                var vis = Level.entryShutterVisuals[i];
                vis.SetActive(true);
                Level.grid.Set(c.x, c.y, Cell.Shutter, vis);
            }
            Sfx.Play(Sfx.Pound);
            camRig.lockCenter = Level.cameraAnchor ?? Level.bossSpawn;
            if (Level.info.boss == BossKind.Minotaur) camRig.extraDistance = 5f;   // sala larga + chefe gigante
            yield return new WaitForSeconds(1.0f);
            switch (Level.info.boss)
            {
                case BossKind.WaterJug: boss = Enemy.Spawn<BossWaterJug>(Level.root, Level.bossSpawn); break;
                case BossKind.WineBarrel: boss = Enemy.Spawn<BossWineBarrel>(Level.root, Level.bossSpawn); break;
                case BossKind.SodaBottle: boss = Enemy.Spawn<BossSodaBottle>(Level.root, Level.bossSpawn); break;
                case BossKind.Shaker: boss = Enemy.Spawn<BossShaker>(Level.root, Level.bossSpawn); break;
                case BossKind.Champagne: boss = Enemy.Spawn<BossChampagne>(Level.root, Level.bossSpawn); break;
                case BossKind.Teapot: boss = Enemy.Spawn<BossTeapot>(Level.root, Level.bossSpawn); break;
                case BossKind.JuiceBox: boss = Enemy.Spawn<BossJuiceBox>(Level.root, Level.bossSpawn); break;
                case BossKind.GlassJar: boss = Enemy.Spawn<BossGlassJar>(Level.root, Level.bossSpawn); break;
                case BossKind.Minotaur: boss = Enemy.Spawn<BossMinotaur>(Level.root, Level.bossSpawn); break;
            }
            if (boss != null) ShowBanner("!! " + boss.displayName + " !!", 2f);
        }

        public void OnBossDefeated(Boss b)
        {
            if (b != boss) return;
            var info = Level.info;
            bossActive = false;
            boss = null;
            camRig.lockCenter = null;
            Progress.SetBossDefeated(info.id);

            foreach (var c in Level.entryShutters) Level.grid.Set(c.x, c.y, Cell.Empty);
            foreach (var c in Level.exitShutters) Level.grid.Set(c.x, c.y, Cell.Empty);
            Level.teleportPad?.SetActive(true);
            Level.exitPortal?.SetActive(true);
            Player.Heal();
            Sfx.Play(Sfx.Unlock);

            if (info.rewardForm > 0)
            {
                bool isNew = !Progress.IsFormUnlocked(info.rewardForm);
                Progress.UnlockForm(info.rewardForm);
                if (isNew)
                {
                    Progress.CurrentForm = info.rewardForm;
                    Progress.Save();
                    Player.SetForm(info.rewardForm);
                }
                var f = Forms.Get(info.rewardForm);
                ShowBanner($"FORMA {info.rewardForm} LIBERADA: {f.ability.ToUpper()} ({f.flavor})", 4f);
                Toast(f.howTo + "   |   ENTER: trocar de forma");
            }
            else if (info.final)
            {
                ShowBanner("O MINOTAURO FOI DERROTADO!", 3f);
                flowCo = StartCoroutine(EndingSequence());
            }
            else
            {
                ShowBanner("CHEFE DERROTADO!", 3f);
                var next = Stages.Get(info.next);
                if (next != null) Toast($"Acesso liberado: {next.title}! Siga pelo portal.", 4f);
            }
        }

        // ---------------------------------------------------------------- final do jogo

        public float fadeAlpha;
        public float endingTime;
        readonly System.Collections.Generic.List<CanModel> parade = new System.Collections.Generic.List<CanModel>();
        CanModel hero;
        float nextFirework;

        IEnumerator EndingSequence()
        {
            transitioning = true;
            for (float t = 0; t < 3f; t += Time.deltaTime)     // explosões finais na sala
            {
                if (Random.value < 0.1f) Fx.Explode(Player.Pos + new Vector2(Random.Range(-8f, 8f), Random.Range(0f, 8f)), new Color(1f, 0.4f, 0.2f), 8);
                yield return null;
            }
            for (fadeAlpha = 0; fadeAlpha < 1f; fadeAlpha += Time.unscaledDeltaTime / 1.2f) yield return null;
            fadeAlpha = 1f;

            // cena final: céu borbulhante, desfile das latas e fogos
            Level.root.gameObject.SetActive(false);
            Destroy(Level.root.gameObject);
            var info = new StageInfo { id = "ending", title = "", theme = Theme.Sky };
            Level = LevelBuilder.Build(info, world);
            Backdrop.ApplyAtmosphere(Theme.Sky, camRig.Cam, sun);
            Player.gameObject.SetActive(false);
            parade.Clear();
            float cx = Level.grid.W * 0.5f;
            for (int i = 0; i < Forms.Count; i++)
            {
                var c = CanModel.Create(Level.root);
                c.SetSkin(SkinLibrary.Get(i));
                c.transform.position = new Vector3(cx - 8.4f + i * 2.4f, 2.8f, 1.2f);
                parade.Add(c);
            }
            hero = CanModel.Create(Level.root);
            hero.SetSkin(SkinLibrary.Get(Progress.CurrentForm));
            hero.transform.localScale = Vector3.one * 1.6f;
            hero.transform.position = new Vector3(cx, 3.3f, -1.2f);
            camRig.lockCenter = new Vector2(cx, 7.5f);
            camRig.Snap(new Vector2(cx, 7.5f), Level);
            menu = Menu.Ending;
            endingTime = 0;
            nextFirework = 0.5f;
            Sfx.Play(Sfx.Unlock);
            for (; fadeAlpha > 0f; fadeAlpha -= Time.unscaledDeltaTime / 1.5f) yield return null;
            fadeAlpha = 0;
            transitioning = false;
        }

        void UpdateEnding(float dt)
        {
            endingTime += dt;
            for (int i = 0; i < parade.Count; i++)
            {
                var c = parade[i];
                if (c == null) continue;
                float ph = endingTime * 4f + i * 0.7f;
                float hop = Mathf.Abs(Mathf.Sin(ph)) * 1.3f;
                var p = c.transform.position;
                c.transform.position = new Vector3(p.x, 2.8f + hop, p.z);
                c.Animate(new CanModel.Pose { facing = (i % 2 == 0) ? 1 : -1, grounded = hop < 0.1f, vy = Mathf.Cos(ph), flipping = i % 3 == 0 && hop > 0.6f }, dt);
            }
            if (hero != null)
            {
                hero.transform.Rotate(0, 0, 0);
                hero.Animate(new CanModel.Pose { facing = Mathf.Sin(endingTime * 0.8f) > 0 ? 1 : -1, grounded = true, shooting = Mathf.Repeat(endingTime, 2f) < 0.4f }, dt);
            }
            nextFirework -= dt;
            if (nextFirework <= 0)
            {
                nextFirework = Random.Range(0.25f, 0.6f);
                var colors = new[] { new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.5f), new Color(0.4f, 0.6f, 1f), new Color(1f, 0.9f, 0.3f), new Color(1f, 0.4f, 1f) };
                Fx.Firework(new Vector3(Level.grid.W * 0.5f + Random.Range(-11f, 11f), Random.Range(8f, 13f), Random.Range(2f, 8f)), colors[Random.Range(0, colors.Length)]);
                if (Random.value < 0.35f) Sfx.Play(Sfx.Explode, 0.25f);
            }
        }

        void BackToTitle()
        {
            camRig.lockCenter = null;
            LoadStage(Stages.All[0].id, SpawnAt.Start);
            Player.gameObject.SetActive(false);
            OpenMenu(Menu.Title);
        }

        public void OnEnemyKilled(Enemy e) { }

        public void OnPlayerDied()
        {
            flowCo = StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            transitioning = true;
            yield return new WaitForSeconds(1.6f);
            LoadStage(respawnStage, SpawnAt.Respawn, respawnPos);
        }

        IEnumerator GoToStage(string id)
        {
            transitioning = true;
            Sfx.Play(Sfx.Teleport);
            ShowBanner("...", 1f);
            yield return new WaitForSeconds(0.8f);
            LoadStage(id, SpawnAt.Start);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

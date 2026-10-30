using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>HUD e menus (IMGUI, resolução de referência 1280x720).</summary>
    public partial class GameManager
    {
        const float RefW = 1280f, RefH = 720f;

        string bannerText, toastText;
        float bannerTime, toastTime;
        int cursor;

        GUIStyle sTitle, sBig, sText, sSmall, sCenter, sItem;

        public void ShowBanner(string text, float time) { bannerText = text; bannerTime = time; }
        public void Toast(string text, float time = 3.5f) { toastText = text; toastTime = time; }

        // ---------------------------------------------------------------- menus

        class CheatItem
        {
            public Func<string> label;
            public Action act;
            public Action<int> side;     // ←/→ no item (opcional)
        }

        int cheatStage;

        List<CheatItem> cheatItems;

        List<CheatItem> CheatItems()
        {
            if (cheatItems != null) return cheatItems;
            string OnOff(bool b) => b ? "[LIGADO]" : "[desligado]";
            cheatItems = new List<CheatItem>
            {
                new CheatItem { label = () => "Invencível " + OnOff(Cheats.invincible), act = () => Cheats.invincible = !Cheats.invincible },
                new CheatItem { label = () => "Vida infinita (leva golpe, não perde vida) " + OnOff(Cheats.infiniteLife), act = () => Cheats.infiniteLife = !Cheats.infiniteLife },
                new CheatItem { label = () => "Sem recarga na ação especial " + OnOff(Cheats.noCooldown), act = () => Cheats.noCooldown = !Cheats.noCooldown },
                new CheatItem { label = () => "Tiros matam com 1 acerto " + OnOff(Cheats.oneHitKill), act = () => Cheats.oneHitKill = !Cheats.oneHitKill },
                new CheatItem { label = () => "Voo livre / atravessa paredes " + OnOff(Cheats.noclip), act = () => Cheats.noclip = !Cheats.noclip },
                new CheatItem { label = () => "Liberar todas as formas", act = () => { Progress.UnlockAllForms(); Toast("Todas as formas liberadas (ENTER para trocar)"); } },
                new CheatItem { label = () => "Derrotar todos os chefes (libera teleportes)", act = () =>
                    {
                        foreach (var s in Stages.All)
                        {
                            Progress.SetBossDefeated(s.id);
                            if (s.rewardForm > 0) Progress.UnlockForm(s.rewardForm);
                        }
                        Toast("Chefes marcados como derrotados. Use M para teleportar.");
                    } },
                new CheatItem { label = () => "Recuperar vida", act = () => Player.Heal() },
                new CheatItem { label = () => "Ir para a sala do chefe desta fase", act = () => { CloseMenu(); LoadStage(Level.info.id, SpawnAt.BossWarp); } },
                new CheatItem { label = () => "Derrotar o chefe atual", act = () => { if (boss != null) boss.Die(); else Toast("Nenhum chefe ativo."); } },
            };
            cheatItems.Add(new CheatItem
            {
                label = () => $"Ir para a fase:  < {Stages.All[cheatStage].title} >",
                act = () => { CloseMenu(); LoadStage(Stages.All[cheatStage].id, SpawnAt.Start); },
                side = d => cheatStage = (cheatStage + d + Stages.All.Length) % Stages.All.Length,
            });
            cheatItems.Add(new CheatItem { label = () => "Recarregar skins da pasta", act = () => { SkinLibrary.ReloadAll(); Player.RefreshSkin(); Toast("Skins recarregadas de " + SkinLibrary.Folder); } });
            cheatItems.Add(new CheatItem { label = () => "Resetar progresso salvo", act = () => { Progress.Reset(); Player.SetForm(0); Toast("Progresso apagado."); } });
            cheatItems.Add(new CheatItem { label = () => "Fechar", act = CloseMenu });
            return cheatItems;
        }

        List<StageInfo> TeleportTargets()
        {
            var list = new List<StageInfo>();
            foreach (var s in Stages.All) if (Progress.IsBossDefeated(s.id)) list.Add(s);
            return list;
        }

        int MenuCount()
        {
            switch (menu)
            {
                case Menu.Title: return Progress.HasAnyProgress ? 3 : 2;
                case Menu.Forms: return Forms.Count;
                case Menu.Teleport: return Mathf.Max(1, TeleportTargets().Count);
                case Menu.Cheats: return CheatItems().Count;
            }
            return 0;
        }

        void OpenMenu(Menu m)
        {
            menu = m;
            cursor = m == Menu.Forms ? Progress.CurrentForm : 0;
            Time.timeScale = 0f;
            Sfx.Play(Sfx.Menu);
        }

        void CloseMenu()
        {
            menu = Menu.None;
            Time.timeScale = 1f;
            menuClosedFrame = Time.frameCount;
        }

        void HandleMenus()
        {
            if (menu == Menu.Ending)
            {
                UpdateEnding(Time.deltaTime);
                if (endingTime > 6f && (GameInput.MenuDown || GameInput.QuitDown || GameInput.JumpDown)) BackToTitle();
                return;
            }
            if (menu == Menu.None)
            {
                if (GameInput.QuitDown) { Quit(); return; }
                if (Player.IsDead || transitioning) return;
                if (GameInput.MenuDown) OpenMenu(Menu.Forms);
                else if (GameInput.TeleportDown)
                {
                    if (bossActive) Toast("Não dá para teleportar durante a luta com o chefe!");
                    else OpenMenu(Menu.Teleport);
                }
                else if (GameInput.CheatsDown) OpenMenu(Menu.Cheats);
                return;
            }

            if (GameInput.QuitDown)
            {
                if (menu == Menu.Title) Quit();
                else CloseMenu();
                return;
            }
            // mesma tecla fecha o menu
            if ((menu == Menu.Teleport && GameInput.TeleportDown) || (menu == Menu.Cheats && GameInput.CheatsDown)) { CloseMenu(); return; }

            int n = MenuCount();
            if (GameInput.UpDown) { cursor = (cursor - 1 + n) % n; Sfx.Play(Sfx.Menu); }
            if (GameInput.DownDown) { cursor = (cursor + 1) % n; Sfx.Play(Sfx.Menu); }
            if (menu == Menu.Cheats && CheatItems()[cursor].side != null && (GameInput.LeftDown || GameInput.RightDown))
            {
                CheatItems()[cursor].side(GameInput.RightDown ? 1 : -1);
                Sfx.Play(Sfx.Menu);
            }
            if (!GameInput.ConfirmDown) return;

            switch (menu)
            {
                case Menu.Title:
                    if (cursor == 0) // continuar ou novo jogo
                    {
                        if (Progress.HasAnyProgress) { CloseMenu(); Continue(); }
                        else { CloseMenu(); LoadStage(Stages.All[0].id, SpawnAt.Start); }
                    }
                    else if (cursor == 1 && Progress.HasAnyProgress) { Progress.Reset(); CloseMenu(); LoadStage(Stages.All[0].id, SpawnAt.Start); }
                    else Quit();
                    break;
                case Menu.Forms:
                    if (Progress.IsFormUnlocked(cursor))
                    {
                        Progress.CurrentForm = cursor;
                        Progress.Save();
                        Player.SetForm(cursor);
                        Sfx.Play(Sfx.Unlock, 0.5f);
                        CloseMenu();
                    }
                    break;
                case Menu.Teleport:
                    var t = TeleportTargets();
                    if (t.Count == 0) { CloseMenu(); break; }
                    CloseMenu();
                    LoadStage(t[cursor].id, SpawnAt.Teleport);
                    break;
                case Menu.Cheats:
                    Sfx.Play(Sfx.Menu);
                    CheatItems()[cursor].act();
                    break;
            }
        }

        void Continue()
        {
            // continua no teleporte do último chefe derrotado
            var t = TeleportTargets();
            if (t.Count > 0) LoadStage(t[t.Count - 1].id, SpawnAt.Teleport);
            else LoadStage(Stages.All[0].id, SpawnAt.Start);
        }

        // ---------------------------------------------------------------- desenho

        void Styles()
        {
            if (sText != null) return;
            sTitle = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sBig = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            sText = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            sCenter = new GUIStyle(sText) { alignment = TextAnchor.MiddleCenter };
            sSmall = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            sItem = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
        }

        static void Box(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Label(Rect r, string text, GUIStyle s, Color c, bool shadow = true)
        {
            var old = GUI.color;
            if (shadow)
            {
                GUI.color = new Color(0, 0, 0, 0.8f * c.a);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, s);
            }
            GUI.color = c;
            GUI.Label(r, text, s);
            GUI.color = old;
        }

        void OnGUI()
        {
            Styles();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / RefW, Screen.height / RefH, 1));

            if (menu == Menu.Title) { DrawTitle(); DrawFade(); return; }
            if (menu == Menu.Ending) { DrawEnding(); DrawFade(); return; }
            if (Level == null) return;

            DrawHud();

            if (bannerTime > 0)
            {
                float a = Mathf.Clamp01(bannerTime * 2f);
                Box(new Rect(0, 110, RefW, 70), new Color(0, 0, 0, 0.55f * a));
                Label(new Rect(0, 110, RefW, 70), bannerText, sBig, new Color(1f, 0.92f, 0.3f, a));
            }
            if (toastTime > 0)
            {
                float a = Mathf.Clamp01(toastTime * 2f);
                Box(new Rect(240, 190, 800, 44), new Color(0, 0, 0, 0.55f * a));
                Label(new Rect(240, 190, 800, 44), toastText, sCenter, new Color(1, 1, 1, a));
            }

            // dica próxima (placas "?")
            if (menu == Menu.None && Player != null && !Player.IsDead)
            {
                Hint best = null;
                float bd = 3.2f;
                foreach (var h in Level.hints)
                {
                    float d = Mathf.Abs(h.pos.x - Player.Pos.x) + Mathf.Abs(h.pos.y - Player.Pos.y) * 0.5f;
                    if (d < bd) { bd = d; best = h; }
                }
                if (best != null && !string.IsNullOrEmpty(best.text))
                {
                    var r = new Rect(190, 600, 900, 70);
                    Box(r, new Color(0.05f, 0.05f, 0.1f, 0.75f));
                    Box(new Rect(r.x, r.y, 6, r.height), new Color(1f, 0.85f, 0.2f));
                    Label(new Rect(r.x + 20, r.y, r.width - 30, r.height), best.text, sText, Color.white);
                }
            }

            Label(new Rect(870, 690, 400, 26), "ENTER formas   M teleporte   C cheats   ESC sair", sSmall, new Color(1, 1, 1, 0.75f));

            switch (menu)
            {
                case Menu.Forms: DrawForms(); break;
                case Menu.Teleport: DrawTeleport(); break;
                case Menu.Cheats: DrawCheats(); break;
            }
            DrawFade();
        }

        void DrawHud()
        {
            var p = Player;
            // barra de vida estilo clássico (vertical, em segmentos)
            DrawBar(new Rect(28, 30, 26, 230), p.Hp / (float)PlayerController.MaxHp, PlayerController.MaxHp, new Color(1f, 0.9f, 0.45f));
            // barra da ação especial (recarga)
            var f = Forms.Get(p.Form);
            float ready = p.HookFrac > 0 ? p.HookFrac : 1f - p.CooldownFrac;
            DrawBar(new Rect(62, 30, 18, 230), ready, 14, f.color);
            Label(new Rect(28, 262, 400, 28), $"F{p.Form} {f.flavor} - {f.ability}", sSmall, Color.white);
            if (p.CooldownFrac > 0 && !Cheats.noCooldown)
                Label(new Rect(28, 284, 400, 24), $"recarga {(p.CooldownFrac * Forms.SpecialCooldown):0.0}s", sSmall, new Color(1, 1, 1, 0.8f));

            var cheatsOn = new List<string>();
            if (Cheats.invincible) cheatsOn.Add("INVENCÍVEL");
            if (Cheats.infiniteLife) cheatsOn.Add("VIDA INFINITA");
            if (Cheats.noCooldown) cheatsOn.Add("SEM RECARGA");
            if (Cheats.oneHitKill) cheatsOn.Add("1 HIT");
            if (Cheats.noclip) cheatsOn.Add("VOO LIVRE");
            if (cheatsOn.Count > 0) Label(new Rect(100, 30, 500, 24), "CHEATS: " + string.Join(", ", cheatsOn), sSmall, new Color(1f, 0.4f, 0.4f));

            if (boss != null && boss.Alive)
            {
                DrawBar(new Rect(RefW - 54, 30, 26, 230), boss.hp / (float)boss.maxHp, Mathf.Min(boss.maxHp, 40), new Color(1f, 0.35f, 0.3f));
                var nameStyle = new GUIStyle(sSmall) { alignment = TextAnchor.MiddleRight };
                Label(new Rect(RefW - 360, 262, 330, 24), boss.displayName, nameStyle, Color.white);
            }
        }

        void DrawFade()
        {
            if (fadeAlpha > 0) Box(new Rect(0, 0, RefW, RefH), new Color(1, 1, 1, Mathf.Clamp01(fadeAlpha)));
        }

        static readonly string[] Credits =
        {
            "MOSTERVANOID",
            "",
            "A lata de energético venceu o Minotauro Vermelho",
            "e devolveu o gás a todas as bebidas do mundo!",
            "",
            "- FORMAS -",
            "0 ORIGINAL  -  Tiro Especial",
            "1 CITRUS  -  Slide",
            "2 BERRY  -  Super Pulo",
            "3 MANGO  -  Dash",
            "4 ICE  -  Pulo Duplo",
            "5 COLA  -  Impacto no Chão",
            "6 KIWI  -  Gancho",
            "7 CHILI  -  Impacto na Parede",
            "",
            "- CHEFES -",
            "Galão d'Água  .  Barril de Vinho  .  Garrafa de Refri",
            "Coqueteleira  .  Garrafa de Champanhe  .  Bule de Chá",
            "Caixinha de Suco  .  Jarra de Limonada",
            "e o Minotauro Vermelho",
            "",
            "Feito com Unity 6.6 + URP",
            "",
            "OBRIGADO POR JOGAR!",
        };

        void DrawEnding()
        {
            float a = Mathf.Clamp01(endingTime / 2f);
            Label(new Rect(0, 30, RefW, 90), "FIM", sTitle, new Color(1f, 0.9f, 0.3f, a));
            // créditos sobem na metade de cima (as latas desfilam embaixo), com uma faixa escura para leitura
            const float top = 120f, bottom = 430f;
            Box(new Rect(300, top - 10, RefW - 600, bottom - top + 20), new Color(0, 0, 0, 0.35f * a));
            float y0 = bottom - endingTime * 40f + 20f;
            for (int i = 0; i < Credits.Length; i++)
            {
                float y = y0 + i * 34f;
                if (y < top || y > bottom) continue;
                var style = i == 0 || Credits[i].StartsWith("-") || Credits[i].StartsWith("OBRIGADO") ? sBig : sCenter;
                float fade = Mathf.Clamp01((y - top) / 50f) * Mathf.Clamp01((bottom - y) / 50f);
                Label(new Rect(0, y, RefW, 34), Credits[i], style, new Color(1, 1, 1, fade));
            }
            if (endingTime > 6f)
                Label(new Rect(0, RefH - 40, RefW, 30), "ENTER: voltar ao título", sCenter, new Color(1, 1, 1, 0.6f + Mathf.Sin(endingTime * 4f) * 0.4f));
        }

        static void DrawBar(Rect r, float frac, int segments, Color c)
        {
            Box(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0, 0, 0, 0.85f));
            int filled = Mathf.CeilToInt(Mathf.Clamp01(frac) * segments);
            float sh = r.height / segments;
            for (int i = 0; i < segments; i++)
            {
                var sr = new Rect(r.x, r.yMax - (i + 1) * sh + 1, r.width, sh - 2);
                Box(sr, i < filled ? c : new Color(0.2f, 0.2f, 0.2f, 0.9f));
                if (i < filled) Box(new Rect(sr.x + 3, sr.y, r.width * 0.3f, sr.height), new Color(1, 1, 1, 0.45f));
            }
        }

        Rect Panel(float w, float h, string title)
        {
            Box(new Rect(0, 0, RefW, RefH), new Color(0, 0, 0, 0.45f));
            var r = new Rect((RefW - w) / 2, (RefH - h) / 2, w, h);
            Box(r, new Color(0.06f, 0.07f, 0.12f, 0.95f));
            Box(new Rect(r.x, r.y, r.width, 4), new Color(1f, 0.85f, 0.2f));
            Label(new Rect(r.x, r.y + 10, r.width, 46), title, sBig, Color.white);
            return r;
        }

        void Item(Rect r, string text, bool selected, bool enabled = true)
        {
            if (selected) Box(new Rect(r.x - 10, r.y, r.width + 20, r.height), new Color(1f, 0.85f, 0.2f, 0.18f));
            var c = !enabled ? new Color(0.5f, 0.5f, 0.5f) : selected ? new Color(1f, 0.9f, 0.3f) : Color.white;
            Label(r, (selected ? "> " : "   ") + text, sItem, c);
        }

        void DrawTitle()
        {
            Box(new Rect(0, 0, RefW, RefH), new Color(0, 0, 0, 0.35f));
            Label(new Rect(0, 90, RefW, 100), "MOSTERVANOID", sTitle, new Color(0.4f, 1f, 0.5f));
            Label(new Rect(0, 175, RefW, 40), "A lata de energético contra-ataca!", sCenter, Color.white);
            Label(new Rect(RefW - 220, RefH - 34, 200, 28), "versão " + Application.version, sSmall, new Color(1, 1, 1, 0.6f));

            var opts = Progress.HasAnyProgress ? new[] { "Continuar", "Novo jogo (apaga progresso)", "Sair" } : new[] { "Novo jogo", "Sair" };
            for (int i = 0; i < opts.Length; i++)
                Item(new Rect(520, 260 + i * 44, 400, 40), opts[i], cursor == i);

            var help = "Mover: WASD / setas     Pular: ESPAÇO / 0     Atirar: K / 2     Especial: L / 3\n" +
                       "Formas: ENTER     Teleporte: M     Cheats: C     Sair: ESC";
            Box(new Rect(140, 560, 1000, 90), new Color(0, 0, 0, 0.6f));
            Label(new Rect(160, 560, 960, 90), help, sCenter, Color.white);
        }

        void DrawForms()
        {
            var r = Panel(980, 560, "FORMAS  (ENTER escolhe, ESC fecha)");
            for (int i = 0; i < Forms.Count; i++)
            {
                bool unlocked = Progress.IsFormUnlocked(i);
                var f = Forms.Get(i);
                float y = r.y + 70 + i * 58;
                var row = new Rect(r.x + 30, y, r.width - 60, 52);
                if (i == cursor) Box(row, new Color(1f, 0.85f, 0.2f, 0.15f));
                if (i == Progress.CurrentForm) Box(new Rect(row.x, row.y, 5, row.height), f.color);
                // miniatura da skin
                var thumb = new Rect(row.x + 14, row.y + 4, 88, 44);
                if (unlocked) GUI.DrawTexture(thumb, SkinLibrary.Get(i), ScaleMode.StretchToFill);
                else Box(thumb, new Color(0.15f, 0.15f, 0.15f));
                string name = unlocked ? $"{i}  {f.flavor} — {f.ability}" : $"{i}  ???";
                Label(new Rect(row.x + 120, row.y, 380, 52), name, sItem, unlocked ? (i == cursor ? new Color(1f, 0.9f, 0.3f) : Color.white) : Color.gray);
                if (unlocked) Label(new Rect(row.x + 500, row.y, row.width - 500, 52), f.howTo, sSmall, new Color(0.85f, 0.9f, 1f));
            }
        }

        void DrawTeleport()
        {
            var t = TeleportTargets();
            var r = Panel(760, 140 + Mathf.Max(1, t.Count) * 50, "TELEPORTES");
            if (t.Count == 0)
            {
                Label(new Rect(r.x + 30, r.y + 70, r.width - 60, 50), "Nenhum teleporte ainda. Derrote um chefe para criar um.", sCenter, Color.gray);
                return;
            }
            for (int i = 0; i < t.Count; i++)
                Item(new Rect(r.x + 40, r.y + 70 + i * 50, r.width - 80, 44), t[i].title + "  (após o chefe)", cursor == i);
        }

        void DrawCheats()
        {
            var items = CheatItems();
            var r = Panel(820, 110 + items.Count * 36, "CHEATS (testes)");
            for (int i = 0; i < items.Count; i++)
                Item(new Rect(r.x + 40, r.y + 66 + i * 36, r.width - 80, 34), items[i].label(), cursor == i);
        }
    }
}

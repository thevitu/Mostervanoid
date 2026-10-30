using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Constrói a fase a partir do texto: grade de colisão, terreno 3D, objetos e cenário de fundo.</summary>
    public static class LevelBuilder
    {
        // Profundidade do terreno (estilo plataforma 3D "DKC"): frente em Z0, fundo em Z1. O jogador anda em z=0.
        public const float Z0 = -1.1f, Z1 = 3.2f;

        public static Level Build(StageInfo info, Transform parent)
        {
            var asset = Resources.Load<TextAsset>("Levels/" + info.id);
            if (asset == null)
            {
                Debug.LogError($"[Mostervanoid] Fase não encontrada: Resources/Levels/{info.id}.txt");
                return null;
            }
            var data = LevelData.Parse(asset.text);

            var lv = new Level { info = info, grid = new LevelGrid(data.Width, data.Height) };
            lv.root = new GameObject("Level_" + info.id).transform;
            lv.root.SetParent(parent, false);

            bool bossDone = Progress.IsBossDefeated(info.id);
            var th = ThemeKit.For(info.theme);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                {
                    char c = data.At(x, y);
                    var cc = GridCenter(x, y);
                    switch (c)
                    {
                        case '#': lv.grid.Set(x, y, Cell.Solid); break;
                        case '=': lv.grid.Set(x, y, Cell.OneWay, OneWayVisual(lv.root, x, y, th)); break;
                        case '^': lv.grid.Set(x, y, Cell.Spike, SpikeVisual(lv.root, x, y, th)); break;
                        case 'X': lv.grid.Set(x, y, Cell.Breakable, BlockVisual(lv.root, x, y, ProcTex.Reinforced, "Reinforced")); break;
                        case 'G': lv.grid.Set(x, y, Cell.Cracked, BlockVisual(lv.root, x, y, ProcTex.Cracked, "Cracked")); break;
                        case '<': lv.grid.Set(x, y, Cell.WindL, WindFx.Create(lv.root, x, y, -1)); break;
                        case '>': lv.grid.Set(x, y, Cell.WindR, WindFx.Create(lv.root, x, y, 1)); break;
                        case 'P': lv.start = Feet(x, y); break;
                        case 'b': lv.bossWarp = Feet(x, y); break;
                        case 'K':
                            lv.checkpoints.Add(new Checkpoint { pos = Feet(x, y), flag = CheckpointVisual(lv.root, x, y) });
                            break;
                        case 'H':
                            lv.hooks.Add(cc);
                            HookVisual(lv.root, cc);
                            break;
                        case 'w': Enemy.Spawn<EnemyWalker>(lv.root, Feet(x, y)); break;
                        case 'f': Enemy.Spawn<EnemyFlyer>(lv.root, cc); break;
                        case 'a': Enemy.Spawn<EnemyArmored>(lv.root, Feet(x, y)); break;
                        case 't': Enemy.Spawn<EnemyTurret>(lv.root, Feet(x, y)); break;
                        case 'O': lv.hasBoss = true; lv.bossSpawn = cc; break;
                        case 'c': lv.cameraAnchor = cc; break;
                        case '[': lv.triggers.Add(new Vector2Int(x, y)); break;
                        case 'd':
                            lv.entryShutters.Add(new Vector2Int(x, y));
                            var sv = ShutterVisual(lv.root, x, y);
                            sv.SetActive(false);
                            lv.entryShutterVisuals.Add(sv);
                            break;
                        case 'D':
                            lv.exitShutters.Add(new Vector2Int(x, y));
                            if (!bossDone) lv.grid.Set(x, y, Cell.Shutter, ShutterVisual(lv.root, x, y));
                            break;
                        case 'T':
                            lv.hasTeleport = true;
                            lv.teleportPos = Feet(x, y);
                            lv.teleportPad = TeleportPad.Create(lv.root, Feet(x, y));
                            break;
                        case 'E':
                            lv.hasExit = true;
                            lv.exitPos = Feet(x, y);
                            lv.exitPortal = ExitPortal.Create(lv.root, Feet(x, y));
                            break;
                        default:
                            if (char.IsDigit(c))
                            {
                                data.hints.TryGetValue(c, out var text);
                                lv.hints.Add(new Hint { pos = cc, text = text ?? "" });
                                SignVisual(lv.root, x, y, th);
                            }
                            break;
                    }
                }

            if (!lv.hasBoss || bossDone)
            {
                lv.teleportPad?.SetActive(true);
                lv.exitPortal?.SetActive(true);
            }

            BuildTerrain(lv, th);
            Backdrop.Build(info.theme, lv, th);
            return lv;
        }

        static Vector2 GridCenter(int x, int y) => LevelGrid.CellCenter(x, y);
        // posição "no chão" da célula: base da célula
        static Vector2 Feet(int x, int y) => new Vector2(x + 0.5f, y);

        // ----------------------------------------------------------------- terreno

        class MeshAcc
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int>[] tris = { new List<int>(), new List<int>(), new List<int>() };

            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(normal); n.Add(normal); n.Add(normal); n.Add(normal);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                var t = tris[sub];
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
        }

        static void BuildTerrain(Level lv, ThemeKit th)
        {
            var g = lv.grid;
            var m = new MeshAcc();
            const int TOP = 0, EDGE = 1, FILL = 2;
            bool S(int x, int y) => g.InBounds(x, y) && g.Get(x, y) == Cell.Solid;

            for (int x = 0; x < g.W; x++)
                for (int y = 0; y < g.H; y++)
                {
                    if (!S(x, y)) continue;
                    bool exposedTop = !S(x, y + 1) && y + 1 < g.H;
                    // frente
                    if (exposedTop)
                        m.Quad(EDGE, new Vector3(x, y, Z0), new Vector3(x, y + 1, Z0), new Vector3(x + 1, y + 1, Z0), new Vector3(x + 1, y, Z0),
                            Vector3.back, new Vector2(x * .5f, 0), new Vector2(x * .5f, 1), new Vector2((x + 1) * .5f, 1), new Vector2((x + 1) * .5f, 0));
                    else
                        m.Quad(FILL, new Vector3(x, y, Z0), new Vector3(x, y + 1, Z0), new Vector3(x + 1, y + 1, Z0), new Vector3(x + 1, y, Z0),
                            Vector3.back, new Vector2(x * .5f, y * .5f), new Vector2(x * .5f, (y + 1) * .5f), new Vector2((x + 1) * .5f, (y + 1) * .5f), new Vector2((x + 1) * .5f, y * .5f));
                    // topo
                    if (exposedTop)
                        m.Quad(TOP, new Vector3(x, y + 1, Z0), new Vector3(x, y + 1, Z1), new Vector3(x + 1, y + 1, Z1), new Vector3(x + 1, y + 1, Z0),
                            Vector3.up, new Vector2(x * .5f, Z0 * .5f), new Vector2(x * .5f, Z1 * .5f), new Vector2((x + 1) * .5f, Z1 * .5f), new Vector2((x + 1) * .5f, Z0 * .5f));
                    // fundo (embaixo)
                    if (!S(x, y - 1) && y > 0)
                        m.Quad(FILL, new Vector3(x + 1, y, Z0), new Vector3(x + 1, y, Z1), new Vector3(x, y, Z1), new Vector3(x, y, Z0),
                            Vector3.down, new Vector2((x + 1) * .5f, Z0 * .5f), new Vector2((x + 1) * .5f, Z1 * .5f), new Vector2(x * .5f, Z1 * .5f), new Vector2(x * .5f, Z0 * .5f));
                    // laterais
                    if (!S(x - 1, y) && x > 0)
                        m.Quad(FILL, new Vector3(x, y, Z1), new Vector3(x, y + 1, Z1), new Vector3(x, y + 1, Z0), new Vector3(x, y, Z0),
                            Vector3.left, new Vector2(Z1 * .5f, y * .5f), new Vector2(Z1 * .5f, (y + 1) * .5f), new Vector2(Z0 * .5f, (y + 1) * .5f), new Vector2(Z0 * .5f, y * .5f));
                    if (!S(x + 1, y) && x < g.W - 1)
                        m.Quad(FILL, new Vector3(x + 1, y, Z0), new Vector3(x + 1, y + 1, Z0), new Vector3(x + 1, y + 1, Z1), new Vector3(x + 1, y, Z1),
                            Vector3.right, new Vector2(Z0 * .5f, y * .5f), new Vector2(Z0 * .5f, (y + 1) * .5f), new Vector2(Z1 * .5f, (y + 1) * .5f), new Vector2(Z1 * .5f, y * .5f));
                }

            // "rocha" abaixo do mapa para dar volume nas colunas com chão
            for (int x = 0; x < g.W; x++)
            {
                if (!S(x, 0)) continue;
                m.Quad(FILL, new Vector3(x, -8, Z0), new Vector3(x, 0, Z0), new Vector3(x + 1, 0, Z0), new Vector3(x + 1, -8, Z0),
                    Vector3.back, new Vector2(x * .5f, -4), new Vector2(x * .5f, 0), new Vector2((x + 1) * .5f, 0), new Vector2((x + 1) * .5f, -4));
                if (!S(x - 1, 0) && x > 0)
                    m.Quad(FILL, new Vector3(x, -8, Z1), new Vector3(x, 0, Z1), new Vector3(x, 0, Z0), new Vector3(x, -8, Z0),
                        Vector3.left, new Vector2(Z1 * .5f, -4), new Vector2(Z1 * .5f, 0), new Vector2(Z0 * .5f, 0), new Vector2(Z0 * .5f, -4));
                if (!S(x + 1, 0) && x < g.W - 1)
                    m.Quad(FILL, new Vector3(x + 1, -8, Z0), new Vector3(x + 1, 0, Z0), new Vector3(x + 1, 0, Z1), new Vector3(x + 1, -8, Z1),
                        Vector3.right, new Vector2(Z0 * .5f, -4), new Vector2(Z0 * .5f, 0), new Vector2(Z1 * .5f, 0), new Vector2(Z1 * .5f, -4));
            }

            var mesh = new Mesh { name = "Terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(m.v);
            mesh.SetNormals(m.n);
            mesh.SetUVs(0, m.uv);
            mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) mesh.SetTriangles(m.tris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var go = new GameObject("Terrain");
            go.transform.SetParent(lv.root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = new[] { th.top, th.edge, th.fill };
        }

        // ----------------------------------------------------------------- visuais de células

        static GameObject OneWayVisual(Transform root, int x, int y, ThemeKit th)
        {
            var go = Prim.Make(PrimitiveType.Cube, root, new Vector3(x + 0.5f, y + 0.85f, 0.8f), new Vector3(1.02f, 0.3f, 3.2f), th.plank, default, "OneWay");
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, -1.2f, -0.3f), new Vector3(0.12f, 2.0f, 0.06f), th.plankSupport);
            return go;
        }

        /// <summary>Estacas pontiagudas: madeira afiada na selva, pontas de metal na refinaria (várias fileiras em profundidade).</summary>
        static GameObject SpikeVisual(Transform root, int x, int y, ThemeKit th)
        {
            var go = new GameObject("Stakes");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y, 0);
            var rng = new System.Random(x * 7919 + y * 104729);
            float R(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);
            bool wood = th.woodStakes;
            var shaft = wood ? Mat.Color(new Color(0.45f, 0.30f, 0.16f), 0.15f) : Mat.Color(new Color(0.35f, 0.36f, 0.40f), 0.6f, 0.8f);
            var tip = wood ? Mat.Color(new Color(0.85f, 0.72f, 0.50f), 0.2f)
                : th.icyStakes ? Mat.Color(new Color(0.75f, 0.92f, 1f), 0.95f, 0.1f)
                : Mat.Color(new Color(0.85f, 0.87f, 0.92f), 0.9f, 0.95f);
            // base de terra/metal
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 0.05f, 1.05f), new Vector3(1f, 0.1f, Z1 - Z0), wood || th.icyStakes ? th.fill : Mat.Color(new Color(0.2f, 0.2f, 0.22f)));
            for (int row = 0; row < 4; row++)
            {
                float z = Z0 + 0.45f + row * 1.0f;
                for (int i = 0; i < 2; i++)
                {
                    float sx = -0.25f + i * 0.5f + R(-0.08f, 0.08f) + (row % 2) * 0.12f;
                    float h = R(0.45f, 0.62f);
                    var stake = Prim.Pivot(go.transform, new Vector3(sx, 0.05f, z), "Stake");
                    stake.localEulerAngles = new Vector3(R(-10, 10), 0, R(-10, 10));
                    float r = wood ? 0.16f : 0.2f;
                    if (wood)
                    {
                        Prim.Make(PrimitiveType.Cylinder, stake, new Vector3(0, h * 0.35f, 0), new Vector3(r, h * 0.35f, r), shaft);
                        Prim.MakeMesh(Prim.Cone, stake, new Vector3(0, h * 0.7f, 0), new Vector3(r, h * 0.45f, r), tip, default, "Tip");
                    }
                    else
                        Prim.MakeMesh(Prim.Cone, stake, Vector3.zero, new Vector3(r * 1.6f, h * 1.1f, r * 1.6f), tip, default, "Tip");
                }
            }
            return go;
        }

        public static GameObject BlockVisual(Transform root, int x, int y, Texture2D tex, string name)
        {
            return Prim.Make(PrimitiveType.Cube, root, new Vector3(x + 0.5f, y + 0.5f, (Z0 + Z1) * 0.5f),
                new Vector3(1f, 1f, Z1 - Z0), Mat.Textured(tex, Color.white), default, name);
        }

        public static GameObject ShutterVisual(Transform root, int x, int y)
        {
            var go = Prim.Make(PrimitiveType.Cube, root, new Vector3(x + 0.5f, y + 0.5f, (Z0 + Z1) * 0.5f),
                new Vector3(0.98f, 0.98f, Z1 - Z0 - 0.1f), Mat.Color(new Color(0.22f, 0.24f, 0.30f), 0.6f, 0.7f), default, "Shutter");
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 0, -0.51f), new Vector3(0.8f, 0.15f, 0.02f), Mat.Glow(new Color(1f, 0.35f, 0.1f), 2.5f));
            return go;
        }

        static Renderer CheckpointVisual(Transform root, int x, int y)
        {
            var go = new GameObject("Checkpoint");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y, 0.6f);
            Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.1f, 0), new Vector3(0.1f, 1.1f, 0.1f), Mat.Color(new Color(0.8f, 0.8f, 0.85f), 0.7f, 0.8f));
            Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(0, 2.25f, 0), Vector3.one * 0.2f, Mat.Color(new Color(0.9f, 0.8f, 0.2f), 0.7f, 0.6f));
            var flag = Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0.4f, 1.85f, 0), new Vector3(0.7f, 0.45f, 0.05f), Mat.New(new Color(0.85f, 0.15f, 0.15f)));
            return flag.GetComponent<Renderer>();
        }

        static void HookVisual(Transform root, Vector2 c)
        {
            var go = new GameObject("Hook");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(c.x, c.y, 0);
            var metal = Mat.Color(new Color(0.6f, 0.62f, 0.68f), 0.8f, 0.9f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(Mathf.Cos(a) * 0.32f, Mathf.Sin(a) * 0.32f, 0),
                    new Vector3(0.12f, 0.26f, 0.12f), metal, new Vector3(0, 0, a * Mathf.Rad2Deg));
            }
            Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.25f, Mat.Glow(new Color(0.4f, 1f, 0.4f), 2f));
            go.AddComponent<Spin>().speed = new Vector3(0, 0, 45);
        }

        static void SignVisual(Transform root, int x, int y, ThemeKit th)
        {
            var go = new GameObject("Sign");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y, 1.6f);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f), th.plankSupport);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 1.25f, 0), new Vector3(0.9f, 0.6f, 0.1f), th.plank);
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 1.32f, -0.07f), new Vector3(0.1f, 0.28f, 0.04f), Mat.Glow(new Color(1f, 0.85f, 0.2f), 1.5f));
            Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0, 1.1f, -0.07f), new Vector3(0.1f, 0.08f, 0.04f), Mat.Glow(new Color(1f, 0.85f, 0.2f), 1.5f));
        }
    }

    /// <summary>Materiais do terreno por tema.</summary>
    public class ThemeKit
    {
        public Material top, edge, fill, plank, plankSupport;
        public bool woodStakes, icyStakes;

        static readonly Dictionary<Theme, ThemeKit> kits = new Dictionary<Theme, ThemeKit>();

        public static ThemeKit For(Theme t)
        {
            if (kits.TryGetValue(t, out var k) && k.top != null) return k;
            k = new ThemeKit();
            switch (t)
            {
                case Theme.Jungle:
                    k.top = Mat.Textured(ProcTex.GrassTop, Color.white, 0.1f);
                    k.edge = Mat.Textured(ProcTex.GrassEdge, Color.white, 0.1f);
                    k.fill = Mat.Textured(ProcTex.Dirt, Color.white, 0.1f);
                    k.plank = Mat.Textured(ProcTex.Wood, Color.white, 0.2f);
                    k.plankSupport = Mat.Color(new Color(0.35f, 0.22f, 0.10f));
                    k.woodStakes = true;
                    break;
                case Theme.Vineyard:   // terra avermelhada, grama seca dourada
                    k.top = Mat.Textured(ProcTex.GrassTop, new Color(1.0f, 0.85f, 0.55f), 0.1f);
                    k.edge = Mat.Textured(ProcTex.GrassEdge, new Color(1.0f, 0.85f, 0.6f), 0.1f);
                    k.fill = Mat.Textured(ProcTex.Dirt, new Color(1.0f, 0.75f, 0.65f), 0.1f);
                    k.plank = Mat.Textured(ProcTex.Wood, new Color(0.8f, 0.55f, 0.45f), 0.25f);
                    k.plankSupport = Mat.Color(new Color(0.30f, 0.16f, 0.10f));
                    k.woodStakes = true;
                    break;
                case Theme.Glacier:    // neve sobre gelo
                    k.top = Mat.Textured(ProcTex.Snow, new Color(0.88f, 0.92f, 0.98f), 0.4f);
                    k.edge = Mat.Textured(ProcTex.SnowEdge, Color.white, 0.75f);
                    k.fill = Mat.Textured(ProcTex.Ice, Color.white, 0.85f);
                    k.plank = Mat.Color(new Color(0.65f, 0.85f, 1f), 0.95f, 0.05f);
                    k.plankSupport = Mat.Color(new Color(0.5f, 0.7f, 0.9f), 0.9f);
                    k.icyStakes = true;
                    break;
                case Theme.Sky:        // ilhas: grama clara em cima, rocha embaixo
                    k.top = Mat.Textured(ProcTex.GrassTop, new Color(0.85f, 1f, 0.8f), 0.1f);
                    k.edge = Mat.Textured(ProcTex.GrassEdge, new Color(0.9f, 1f, 0.9f), 0.1f);
                    k.fill = Mat.Textured(ProcTex.Dirt, new Color(0.75f, 0.72f, 0.8f), 0.15f);
                    k.plank = Mat.Color(new Color(0.95f, 0.97f, 1f), 0.6f);
                    k.plankSupport = Mat.Color(new Color(0.8f, 0.85f, 0.95f), 0.4f);
                    k.icyStakes = true;
                    break;
                case Theme.Cave:       // rocha roxa com musgo luminoso
                    k.top = Mat.Textured(ProcTex.Rock, new Color(0.8f, 1f, 0.95f), 0.3f);
                    k.edge = Mat.Textured(ProcTex.GrassEdge, new Color(0.45f, 0.75f, 0.85f), 0.2f);
                    k.fill = Mat.Textured(ProcTex.Rock, Color.white, 0.25f);
                    k.plank = Mat.Textured(ProcTex.Wood, new Color(0.6f, 0.5f, 0.6f), 0.2f);
                    k.plankSupport = Mat.Color(new Color(0.25f, 0.2f, 0.25f));
                    k.icyStakes = true;
                    break;
                case Theme.Coffee:     // rocha vulcânica escura com veios de café quente
                    k.top = Mat.Textured(ProcTex.Rock, new Color(0.75f, 0.55f, 0.4f), 0.2f);
                    k.edge = Mat.Textured(ProcTex.Dirt, new Color(0.55f, 0.35f, 0.25f), 0.15f);
                    k.fill = Mat.Textured(ProcTex.Rock, new Color(0.7f, 0.5f, 0.38f), 0.15f);
                    k.plank = Mat.Textured(ProcTex.Wood, new Color(0.55f, 0.35f, 0.25f), 0.3f);
                    k.plankSupport = Mat.Color(new Color(0.2f, 0.12f, 0.08f));
                    k.woodStakes = true;
                    break;
                case Theme.GlassFactory:
                    k.top = Mat.Textured(ProcTex.Grate, new Color(0.7f, 0.95f, 0.95f), 0.6f, 0.5f);
                    k.edge = Mat.Textured(ProcTex.MetalEdge, new Color(0.75f, 0.95f, 0.95f), 0.5f, 0.5f);
                    k.fill = Mat.Textured(ProcTex.Metal, new Color(0.6f, 0.8f, 0.85f), 0.5f, 0.5f);
                    k.plank = Mat.Color(new Color(0.75f, 0.95f, 1f), 0.95f, 0.1f);
                    k.plankSupport = Mat.Color(new Color(0.3f, 0.35f, 0.38f), 0.5f, 0.6f);
                    break;
                case Theme.Labyrinth:
                    k.top = Mat.Textured(ProcTex.Brick, new Color(0.9f, 0.85f, 0.8f), 0.15f);
                    k.edge = Mat.Textured(ProcTex.Brick, new Color(0.85f, 0.75f, 0.7f), 0.15f);
                    k.fill = Mat.Textured(ProcTex.Brick, new Color(0.75f, 0.65f, 0.6f), 0.15f);
                    k.plank = Mat.Textured(ProcTex.Wood, new Color(0.5f, 0.3f, 0.2f), 0.2f);
                    k.plankSupport = Mat.Color(new Color(0.2f, 0.15f, 0.12f));
                    break;
                default:               // refinaria
                    k.top = Mat.Textured(ProcTex.Grate, Color.white, 0.5f, 0.6f);
                    k.edge = Mat.Textured(ProcTex.MetalEdge, Color.white, 0.45f, 0.5f);
                    k.fill = Mat.Textured(ProcTex.Metal, Color.white, 0.4f, 0.5f);
                    k.plank = Mat.Textured(ProcTex.Grate, new Color(0.9f, 0.75f, 0.3f), 0.5f, 0.6f);
                    k.plankSupport = Mat.Color(new Color(0.3f, 0.3f, 0.33f), 0.5f, 0.6f);
                    break;
            }
            kits[t] = k;
            return k;
        }
    }

    public class Spin : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 90, 0);
        void Update() => transform.Rotate(speed * Time.deltaTime, Space.Self);
    }
}

using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Cenário de fundo em camadas 3D (paralaxe real pela câmera em perspectiva), no estilo
    /// dos jogos de plataforma pré-renderizados do SNES: várias "fatias" de profundidade com névoa.
    /// </summary>
    public static class Backdrop
    {
        public static void Build(Theme theme, Level lv, ThemeKit th)
        {
            var root = new GameObject("Backdrop").transform;
            root.SetParent(lv.root, false);
            var rng = new System.Random(lv.info.id.GetHashCode());
            switch (theme)
            {
                case Theme.Jungle: Jungle(root, lv, rng); break;
                case Theme.Vineyard: Vineyard(root, lv, rng); break;
                case Theme.Glacier: Glacier(root, lv, rng); break;
                case Theme.Sky: Sky(root, lv, rng); break;
                case Theme.Cave: Cave(root, lv, rng); break;
                case Theme.Coffee: CoffeeChimney(root, lv, rng); break;
                case Theme.GlassFactory: GlassFactory(root, lv, rng); break;
                case Theme.Labyrinth: Labyrinth(root, lv, rng); break;
                default: Refinery(root, lv, rng); break;
            }
        }

        public static void ApplyAtmosphere(Theme theme, Camera cam, Light sun)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            if (theme == Theme.Jungle)
            {
                var sky = new Color(0.52f, 0.78f, 0.92f);
                cam.backgroundColor = sky;
                RenderSettings.fogColor = new Color(0.60f, 0.82f, 0.88f);
                RenderSettings.fogStartDistance = 26f;
                RenderSettings.fogEndDistance = 150f;
                RenderSettings.ambientSkyColor = new Color(0.65f, 0.75f, 0.85f);
                RenderSettings.ambientEquatorColor = new Color(0.45f, 0.55f, 0.40f);
                RenderSettings.ambientGroundColor = new Color(0.25f, 0.20f, 0.15f);
                sun.color = new Color(1f, 0.93f, 0.78f);
                sun.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            }
            else if (theme == Theme.Vineyard)
            {
                cam.backgroundColor = new Color(0.98f, 0.62f, 0.42f);
                RenderSettings.fogColor = new Color(0.95f, 0.62f, 0.50f);
                RenderSettings.fogStartDistance = 24f;
                RenderSettings.fogEndDistance = 140f;
                RenderSettings.ambientSkyColor = new Color(0.85f, 0.60f, 0.65f);
                RenderSettings.ambientEquatorColor = new Color(0.70f, 0.45f, 0.35f);
                RenderSettings.ambientGroundColor = new Color(0.25f, 0.15f, 0.15f);
                sun.color = new Color(1f, 0.72f, 0.45f);
                sun.intensity = 1.2f;
                sun.transform.rotation = Quaternion.Euler(22, -60, 0);
            }
            else if (theme == Theme.Cave || theme == Theme.Coffee || theme == Theme.GlassFactory || theme == Theme.Labyrinth)
            {
                Color bg, fog, amb, sunC; float sunI, fogEnd;
                switch (theme)
                {
                    case Theme.Cave: bg = new Color(0.06f, 0.05f, 0.12f); fog = new Color(0.12f, 0.10f, 0.22f); amb = new Color(0.35f, 0.32f, 0.50f); sunC = new Color(0.6f, 0.8f, 1f); sunI = 0.55f; fogEnd = 90f; break;
                    case Theme.Coffee: bg = new Color(0.16f, 0.07f, 0.04f); fog = new Color(0.35f, 0.16f, 0.08f); amb = new Color(0.50f, 0.32f, 0.25f); sunC = new Color(1f, 0.65f, 0.4f); sunI = 0.8f; fogEnd = 110f; break;
                    case Theme.GlassFactory: bg = new Color(0.10f, 0.16f, 0.20f); fog = new Color(0.20f, 0.32f, 0.38f); amb = new Color(0.45f, 0.55f, 0.60f); sunC = new Color(0.9f, 0.95f, 1f); sunI = 0.9f; fogEnd = 120f; break;
                    default: bg = new Color(0.10f, 0.04f, 0.04f); fog = new Color(0.25f, 0.08f, 0.06f); amb = new Color(0.45f, 0.30f, 0.28f); sunC = new Color(1f, 0.6f, 0.45f); sunI = 0.7f; fogEnd = 100f; break;
                }
                cam.backgroundColor = bg;
                RenderSettings.fogColor = fog;
                RenderSettings.fogStartDistance = 20f;
                RenderSettings.fogEndDistance = fogEnd;
                RenderSettings.ambientSkyColor = amb;
                RenderSettings.ambientEquatorColor = amb * 0.8f;
                RenderSettings.ambientGroundColor = amb * 0.5f;
                sun.color = sunC;
                sun.intensity = sunI;
                sun.transform.rotation = Quaternion.Euler(45, -30, 0);
            }
            else if (theme == Theme.Sky)
            {
                cam.backgroundColor = new Color(0.45f, 0.70f, 0.98f);
                RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.98f);
                RenderSettings.fogStartDistance = 30f;
                RenderSettings.fogEndDistance = 180f;
                RenderSettings.ambientSkyColor = new Color(0.70f, 0.80f, 0.95f);
                RenderSettings.ambientEquatorColor = new Color(0.60f, 0.65f, 0.75f);
                RenderSettings.ambientGroundColor = new Color(0.45f, 0.45f, 0.55f);
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            else if (theme == Theme.Glacier)
            {
                cam.backgroundColor = new Color(0.16f, 0.24f, 0.44f);
                RenderSettings.fogColor = new Color(0.42f, 0.54f, 0.74f);
                RenderSettings.fogStartDistance = 25f;
                RenderSettings.fogEndDistance = 170f;
                RenderSettings.ambientSkyColor = new Color(0.42f, 0.52f, 0.75f);
                RenderSettings.ambientEquatorColor = new Color(0.34f, 0.42f, 0.56f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.30f);
                sun.color = new Color(0.85f, 0.92f, 1f);
                sun.intensity = 0.72f;
                sun.transform.rotation = Quaternion.Euler(40, -25, 0);
            }
            else
            {
                var sky = new Color(0.32f, 0.18f, 0.30f);
                cam.backgroundColor = sky;
                RenderSettings.fogColor = new Color(0.55f, 0.30f, 0.32f);
                RenderSettings.fogStartDistance = 22f;
                RenderSettings.fogEndDistance = 130f;
                RenderSettings.ambientSkyColor = new Color(0.55f, 0.45f, 0.60f);
                RenderSettings.ambientEquatorColor = new Color(0.45f, 0.30f, 0.30f);
                RenderSettings.ambientGroundColor = new Color(0.15f, 0.12f, 0.15f);
                sun.color = new Color(1f, 0.72f, 0.50f);
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(35, -50, 0);
            }
        }

        static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        // Cobertura necessária numa profundidade z (câmera em z = CameraRig.Distance negativo)
        static Vector4 Span(Level lv, float z, float margin = 1.2f)
        {
            float d = z + CameraRig.Distance;
            float halfH = d * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad) * margin;
            float halfW = halfH * 1.9f;
            return new Vector4(-halfW, lv.grid.W + halfW, -halfH, lv.grid.H + halfH);
        }

        // ------------------------------------------------------------------ SELVA

        static void Jungle(Transform root, Level lv, System.Random rng)
        {
            var trunk = Mat.Color(new Color(0.38f, 0.25f, 0.14f), 0.1f);
            var trunkFar = Mat.Color(new Color(0.30f, 0.32f, 0.30f), 0.1f);
            var leaf = Mat.Color(new Color(0.16f, 0.48f, 0.16f), 0.2f);
            var leaf2 = Mat.Color(new Color(0.24f, 0.60f, 0.18f), 0.2f);
            var leafFar = Mat.Color(new Color(0.20f, 0.40f, 0.30f), 0.1f);
            var hill = Mat.Color(new Color(0.22f, 0.50f, 0.28f), 0.05f);
            var mount = Mat.Color(new Color(0.40f, 0.55f, 0.65f), 0.05f);
            var rock = Mat.Color(new Color(0.45f, 0.42f, 0.40f), 0.1f);
            var fg = Mat.Color(new Color(0.06f, 0.22f, 0.08f), 0.2f);

            // montanhas bem ao fundo
            var s = Span(lv, 90);
            for (float x = s.x; x < s.y; x += R(rng, 14, 26))
            {
                float h = R(rng, 25, 55);
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, -5 + h * 0.15f, 90), new Vector3(h, h, 10), mount, new Vector3(0, 0, 45), "Mountain");
            }
            // morros
            s = Span(lv, 45);
            for (float x = s.x; x < s.y; x += R(rng, 9, 16))
            {
                float w = R(rng, 18, 32);
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, R(rng, -6, -1), 45), new Vector3(w, w * R(rng, 0.45f, 0.7f), 8), hill, default, "Hill");
            }
            // chão da floresta: continua o terreno para trás, até o fundo (as árvores ficam apoiadas nele)
            float gy = GroundLevel(lv);
            var floorMat = Mat.Textured(ProcTex.GrassTop, new Color(0.75f, 0.85f, 0.7f), 0.05f, 0f, new Vector2((lv.grid.W + 240) / 3f, 30));
            Prim.Make(PrimitiveType.Cube, root, new Vector3(lv.grid.W * 0.5f, gy - 0.5f, 49.5f), new Vector3(lv.grid.W + 240, 1f, 92f), floorMat, default, "ForestFloor");
            var shrub = Mat.Color(new Color(0.14f, 0.42f, 0.14f), 0.15f);
            var shrubFar = Mat.Color(new Color(0.18f, 0.38f, 0.26f), 0.1f);

            // floresta distante
            s = Span(lv, 22);
            for (float x = s.x; x < s.y; x += R(rng, 2.5f, 4.5f))
            {
                var p = new Vector3(x, gy - 0.1f, R(rng, 18, 26));
                Tree(root, p, R(rng, 1.4f, 2.2f), trunkFar, leafFar, rng);
                Undergrowth(root, p, 2.2f, shrubFar, rng);
            }
            // árvores próximas (atrás do terreno), com raízes e arbustos na base
            s = Span(lv, 8);
            for (float x = s.x; x < s.y; x += R(rng, 4, 8))
            {
                var p = new Vector3(x, gy - 0.05f, R(rng, 6, 9));
                Tree(root, p, R(rng, 0.9f, 1.3f), trunk, rng.Next(2) == 0 ? leaf : leaf2, rng);
                for (int i = 0; i < 4; i++)   // raízes
                    Prim.Make(PrimitiveType.Cylinder, root, p + Quaternion.Euler(0, i * 90 + R(rng, -20, 20), 0) * new Vector3(0.5f, 0.15f, 0),
                        new Vector3(0.22f, 0.5f, 0.22f), trunk, new Vector3(0, i * 90, 60), "Root");
                Undergrowth(root, p, 1.4f, rng.Next(2) == 0 ? leaf2 : shrub, rng);
            }
            // sebe contínua logo atrás do terreno: esconde a junção terreno/chão da floresta
            s = Span(lv, 5);
            for (float x = s.x; x < s.y; x += R(rng, 0.9f, 1.6f))
            {
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + R(rng, -0.1f, 0.3f), R(rng, 3.8f, 5.2f)),
                    new Vector3(R(rng, 1.6f, 2.8f), R(rng, 0.9f, 1.7f), R(rng, 1.2f, 2f)), rng.Next(3) == 0 ? leaf2 : shrub, default, "Hedge");
                if (rng.NextDouble() < 0.35)   // samambaias
                    for (int k = 0; k < 4; k++)
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + R(rng, -0.3f, 0.3f), gy + 0.5f, R(rng, 3.5f, 4.2f)),
                            new Vector3(1.6f, 0.12f, 0.45f), leaf, new Vector3(R(rng, -20, 20), k * 45 + R(rng, -10, 10), R(rng, 15, 35)), "Fern");
            }

            // pedras e arbustos sobre o terreno, atrás do jogador
            var g = lv.grid;
            for (int x = 1; x < g.W - 1; x++)
                for (int y = 0; y < g.H - 1; y++)
                {
                    if (g.Get(x, y) != Cell.Solid || g.Get(x, y + 1) != Cell.Empty) continue;
                    double roll = rng.NextDouble();
                    if (roll < 0.10)
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + 0.5f, y + 1.1f, R(rng, 2.0f, 2.8f)),
                            new Vector3(R(rng, 1, 1.8f), R(rng, 0.7f, 1.2f), 1), leaf2, default, "Bush");
                    else if (roll < 0.15)
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + 0.5f, y + 1.0f, R(rng, 2.0f, 2.8f)),
                            new Vector3(R(rng, 0.6f, 1.1f), R(rng, 0.4f, 0.7f), 0.8f), rock, default, "Rock");
                    else if (roll < 0.19 && y < 4)
                        // folhagem em primeiro plano (bem perto da câmera, cobre um pouco o chão)
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + 0.5f, y + 0.4f, R(rng, -3.2f, -2.4f)),
                            new Vector3(R(rng, 1.4f, 2.2f), R(rng, 0.8f, 1.2f), 0.6f), fg, default, "Foreground");
                }
        }

        /// <summary>Altura mais comum do chão (topo das colunas sólidas que começam em y=0).</summary>
        static float GroundLevel(Level lv)
        {
            var g = lv.grid;
            var count = new System.Collections.Generic.Dictionary<int, int>();
            for (int x = 0; x < g.W; x++)
            {
                int h = 0;
                while (h < g.H && g.Get(x, h) == Cell.Solid) h++;
                if (h == 0 || h == g.H) continue;
                count.TryGetValue(h, out int c);
                count[h] = c + 1;
            }
            int best = 2, bc = -1;
            foreach (var kv in count) if (kv.Value > bc) { bc = kv.Value; best = kv.Key; }
            return best;
        }

        static void Undergrowth(Transform root, Vector3 basePos, float size, Material m, System.Random rng)
        {
            int n = 3 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                float a = R(rng, 0, Mathf.PI * 2);
                var off = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a) * 0.5f) * R(rng, 0.3f, 1.0f) * size;
                Prim.Make(PrimitiveType.Sphere, root, basePos + off + Vector3.up * R(rng, 0.1f, 0.35f) * size,
                    new Vector3(R(rng, 0.8f, 1.4f), R(rng, 0.5f, 0.9f), R(rng, 0.8f, 1.2f)) * size, m, default, "Bush");
            }
        }

        static void Tree(Transform root, Vector3 basePos, float scale, Material trunk, Material leaf, System.Random rng)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(root, false);
            t.localPosition = basePos;
            t.localScale = Vector3.one * scale;
            float h = R(rng, 6, 10);
            float lean = R(rng, -8, 8);
            Prim.Make(PrimitiveType.Cylinder, t, new Vector3(0, h * 0.5f, 0), new Vector3(0.5f, h * 0.5f, 0.5f), trunk, new Vector3(0, 0, lean));
            var top = new Vector3(-Mathf.Sin(lean * Mathf.Deg2Rad) * h, h, 0);
            // copa estilo palmeira: folhas inclinadas + bola central
            Prim.Make(PrimitiveType.Sphere, t, top, Vector3.one * 1.6f, leaf);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60 + R(rng, -10, 10);
                Prim.Make(PrimitiveType.Sphere, t, top + Quaternion.Euler(0, a, 0) * new Vector3(1.6f, -0.4f, 0),
                    new Vector3(3.2f, 0.45f, 1.0f), leaf, new Vector3(0, a, -20));
            }
        }

        /// <summary>Chão contínuo atrás do terreno, até o fundo.</summary>
        static float BackFloor(Transform root, Level lv, Material m)
        {
            float gy = GroundLevel(lv);
            Prim.Make(PrimitiveType.Cube, root, new Vector3(lv.grid.W * 0.5f, gy - 0.5f, 49.5f), new Vector3(lv.grid.W + 240, 1f, 92f), m, default, "BackFloor");
            return gy;
        }

        // ------------------------------------------------------------------ VINHEDO

        static void Vineyard(Transform root, Level lv, System.Random rng)
        {
            var hillFar = Mat.Color(new Color(0.55f, 0.35f, 0.50f), 0.05f);
            var hillMid = Mat.Color(new Color(0.55f, 0.50f, 0.25f), 0.05f);
            var vine = Mat.Color(new Color(0.25f, 0.42f, 0.12f), 0.2f);
            var grape = Mat.Color(new Color(0.40f, 0.10f, 0.35f), 0.7f);
            var post = Mat.Color(new Color(0.35f, 0.22f, 0.12f), 0.1f);
            var cypress = Mat.Color(new Color(0.12f, 0.25f, 0.12f), 0.1f);
            var barrelWood = Mat.Textured(ProcTex.Wood, new Color(0.75f, 0.5f, 0.35f), 0.3f);
            var iron = Mat.Color(new Color(0.2f, 0.2f, 0.22f), 0.6f, 0.8f);
            var wall = Mat.Color(new Color(0.92f, 0.82f, 0.65f), 0.1f);
            var roof = Mat.Color(new Color(0.70f, 0.30f, 0.18f), 0.2f);

            // sol poente
            Prim.Make(PrimitiveType.Sphere, root, new Vector3(lv.grid.W * 0.5f, 22, 105), Vector3.one * 26f, Mat.Glow(new Color(1f, 0.75f, 0.35f), 3f), default, "Sun");
            var s = Span(lv, 70);
            for (float x = s.x; x < s.y; x += R(rng, 14, 24))
            {
                float w = R(rng, 30, 50);
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, R(rng, -10, -4), 70), new Vector3(w, w * 0.5f, 10), hillFar, default, "Hill");
            }
            s = Span(lv, 42);
            for (float x = s.x; x < s.y; x += R(rng, 10, 18))
            {
                float w = R(rng, 22, 36);
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, R(rng, -7, -3), 42), new Vector3(w, w * 0.45f, 8), hillMid, default, "Hill");
            }
            float gy = BackFloor(root, lv, Mat.Textured(ProcTex.GrassTop, new Color(0.9f, 0.75f, 0.45f), 0.05f, 0f, new Vector2((lv.grid.W + 240) / 3f, 30)));

            // fileiras de videiras em profundidade
            foreach (float z in new[] { 7f, 11f, 15f, 20f, 26f })
            {
                s = Span(lv, z);
                for (float x = s.x; x < s.y; x += 1.7f)
                {
                    Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + 1.0f, z), new Vector3(1.9f, 1.3f, 1.1f), vine, default, "Vine");
                    if (rng.NextDouble() < 0.45)
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + R(rng, -0.5f, 0.5f), gy + 0.7f, z - 0.5f), new Vector3(0.35f, 0.5f, 0.35f), grape, default, "Grapes");
                    if (Mathf.Repeat(x, 6.8f) < 1.7f)
                        Prim.Make(PrimitiveType.Cube, root, new Vector3(x, gy + 0.8f, z - 0.2f), new Vector3(0.12f, 1.6f, 0.12f), post, default, "Post");
                }
            }
            // ciprestes
            s = Span(lv, 18);
            for (float x = s.x; x < s.y; x += R(rng, 6, 14))
            {
                float h = R(rng, 5, 9);
                float z = R(rng, 13, 30);
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + h * 0.5f, z), new Vector3(1.4f, h, 1.4f), cypress, default, "Cypress");
            }
            // casa da vinícola
            for (float x = 20; x < lv.grid.W; x += R(rng, 45, 70))
            {
                float hx = x + R(rng, -5, 5);
                Prim.Make(PrimitiveType.Cube, root, new Vector3(hx, gy + 2f, 32), new Vector3(8, 4, 5), wall, default, "House");
                Prim.Make(PrimitiveType.Cube, root, new Vector3(hx, gy + 4.6f, 32), new Vector3(6.2f, 6.2f, 5.4f), roof, new Vector3(0, 0, 45), "Roof");
                Prim.Make(PrimitiveType.Cube, root, new Vector3(hx - 1.5f, gy + 2.2f, 29.45f), new Vector3(1.2f, 1.2f, 0.1f), Mat.Glow(new Color(1f, 0.8f, 0.4f), 1.5f), default, "Window");
            }
            // barris logo atrás do terreno
            for (int x = 3; x < lv.grid.W - 3; x += 9 + rng.Next(14))
            {
                int n = 1 + rng.Next(3);
                for (int i = 0; i < n; i++)
                {
                    var b = new Vector3(x + i * 1.3f, gy + 0.6f, 4.6f);
                    Prim.Make(PrimitiveType.Cylinder, root, b, new Vector3(1.2f, 0.6f, 1.2f), barrelWood, default, "Barrel");
                    Prim.Make(PrimitiveType.Cylinder, root, b + Vector3.up * 0.35f, new Vector3(1.25f, 0.04f, 1.25f), iron, default, "Hoop");
                    Prim.Make(PrimitiveType.Cylinder, root, b - Vector3.up * 0.35f, new Vector3(1.25f, 0.04f, 1.25f), iron, default, "Hoop");
                }
            }
            // sebe com cachos de uva
            s = Span(lv, 4.5f);
            for (float x = s.x; x < s.y; x += R(rng, 1.0f, 1.7f))
            {
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + R(rng, -0.1f, 0.3f), R(rng, 3.8f, 5f)),
                    new Vector3(R(rng, 1.6f, 2.6f), R(rng, 0.9f, 1.5f), R(rng, 1.2f, 1.8f)), vine, default, "Hedge");
                if (rng.NextDouble() < 0.3)
                    Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + 0.3f, 3.6f), new Vector3(0.4f, 0.55f, 0.4f), grape, default, "Grapes");
            }
        }

        // ------------------------------------------------------------------ GELEIRA

        static void Glacier(Transform root, Level lv, System.Random rng)
        {
            var mountain = Mat.Color(new Color(0.70f, 0.80f, 0.95f), 0.5f);
            var cliff = Mat.Textured(ProcTex.Ice, new Color(0.8f, 0.9f, 1f), 0.9f, 0f, new Vector2(3, 3));
            var crystal = Mat.Glow(new Color(0.35f, 0.75f, 1f), 1.1f);
            var crystal2 = Mat.Glow(new Color(0.6f, 0.45f, 1f), 1.0f);
            var bottleIce = Mat.Color(new Color(0.55f, 0.80f, 0.95f), 0.95f, 0.1f);
            var snowMat = Mat.Color(new Color(0.82f, 0.88f, 0.96f), 0.3f);

            // aurora boreal
            for (int i = 0; i < 5; i++)
                Prim.Make(PrimitiveType.Cube, root, new Vector3(lv.grid.W * 0.5f + R(rng, -40, 40), 30 + i * 3, 115),
                    new Vector3(lv.grid.W + 200, R(rng, 1.5f, 3f), 1), Mat.Glow(i % 2 == 0 ? new Color(0.3f, 1f, 0.6f) : new Color(0.4f, 0.7f, 1f), 1.8f), new Vector3(0, 0, R(rng, -4, 4)), "Aurora");
            var s = Span(lv, 90);
            for (float x = s.x; x < s.y; x += R(rng, 12, 22))
            {
                float h = R(rng, 28, 60);
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, -6 + h * 0.15f, 90), new Vector3(h, h, 10), mountain, new Vector3(0, 0, 45), "IcePeak");
            }
            float gy = BackFloor(root, lv, Mat.Textured(ProcTex.Snow, new Color(0.8f, 0.86f, 0.95f), 0.3f, 0f, new Vector2((lv.grid.W + 240) / 4f, 25)));
            // penhascos de gelo com neve no topo
            s = Span(lv, 38);
            for (float x = s.x; x < s.y; x += R(rng, 8, 16))
            {
                float h = R(rng, 8, 22);
                float w = R(rng, 5, 10);
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, gy + h * 0.5f, 38), new Vector3(w, h, 6), cliff, default, "Cliff");
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, gy + h + 0.2f, 38), new Vector3(w + 0.4f, 0.6f, 6.5f), snowMat, default, "SnowCap");
            }
            // garrafas gigantes congeladas
            s = Span(lv, 20);
            for (float x = s.x; x < s.y; x += R(rng, 18, 30))
            {
                var bt = Prim.Pivot(root, new Vector3(x, gy, R(rng, 16, 24)), "FrozenBottle");
                bt.localEulerAngles = new Vector3(0, 0, R(rng, -25, 25));
                Prim.Make(PrimitiveType.Cylinder, bt, new Vector3(0, 3f, 0), new Vector3(3f, 3f, 3f), bottleIce);
                Prim.Make(PrimitiveType.Sphere, bt, new Vector3(0, 6.2f, 0), new Vector3(3f, 2f, 3f), bottleIce);
                Prim.Make(PrimitiveType.Cylinder, bt, new Vector3(0, 7.5f, 0), new Vector3(1.1f, 1f, 1.1f), bottleIce);
                Prim.Make(PrimitiveType.Sphere, bt, new Vector3(0, 3f, 0), Vector3.one * 1.6f, crystal2);
            }
            // cristais brilhantes (camada próxima)
            s = Span(lv, 8);
            for (float x = s.x; x < s.y; x += R(rng, 3, 7))
            {
                var c = new Vector3(x, gy, R(rng, 5, 11));
                int n = 2 + rng.Next(3);
                for (int i = 0; i < n; i++)
                {
                    float h = R(rng, 1.2f, 3.5f);
                    Prim.Make(PrimitiveType.Cube, root, c + new Vector3(R(rng, -0.6f, 0.6f), h * 0.4f, R(rng, -0.4f, 0.4f)),
                        new Vector3(0.45f, h, 0.45f), rng.Next(3) == 0 ? crystal2 : crystal, new Vector3(R(rng, -10, 10), 45, R(rng, -25, 25)), "Crystal");
                }
            }
            // montes de neve atrás do terreno
            s = Span(lv, 4.5f);
            for (float x = s.x; x < s.y; x += R(rng, 1.2f, 2.2f))
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, gy + R(rng, -0.3f, 0.1f), R(rng, 3.8f, 5f)),
                    new Vector3(R(rng, 2f, 3.2f), R(rng, 0.8f, 1.4f), R(rng, 1.4f, 2f)), snowMat, default, "SnowDrift");
            root.gameObject.AddComponent<Snowfall>();
        }

        // ------------------------------------------------------------------ CÉU

        static void Sky(Transform root, Level lv, System.Random rng)
        {
            var cloud = Mat.Color(new Color(0.97f, 0.98f, 1f), 0.2f);
            var cloudFar = Mat.Color(new Color(0.85f, 0.90f, 0.98f), 0.1f);
            var rock = Mat.Textured(ProcTex.Dirt, new Color(0.7f, 0.68f, 0.78f), 0.15f);
            var grass = Mat.Textured(ProcTex.GrassTop, new Color(0.85f, 1f, 0.8f), 0.1f);
            var bubble = Mat.Color(new Color(0.75f, 0.92f, 1f), 0.98f, 0.2f);
            var gold = Mat.Glow(new Color(1f, 0.85f, 0.4f), 1.4f);
            var glass = Mat.Color(new Color(0.8f, 0.9f, 0.95f), 0.95f, 0.3f);

            // mar de nuvens embaixo (onde se cai)
            foreach (float z in new[] { -2f, 6f, 16f, 34f, 60f })
            {
                var s = Span(lv, z);
                for (float x = s.x; x < s.y; x += R(rng, 3, 6))
                    Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, R(rng, -5f, -2.5f) - z * 0.05f, z + R(rng, -1, 1)),
                        new Vector3(R(rng, 5, 9), R(rng, 2.5f, 4f), R(rng, 3, 6)), z > 20 ? cloudFar : cloud, default, "CloudSea");
            }
            // nuvens altas
            var span = Span(lv, 45);
            for (int i = 0; i < 40; i++)
            {
                var c = new Vector3(R(rng, span.x, span.y), R(rng, 8, span.w), R(rng, 30, 70));
                for (int k = 0; k < 4; k++)
                    Prim.Make(PrimitiveType.Sphere, root, c + new Vector3(R(rng, -3, 3), R(rng, -0.5f, 1), 0), new Vector3(R(rng, 3, 6), R(rng, 2, 3.5f), 3), cloudFar, default, "Cloud");
            }
            // ilhas flutuantes distantes (cone de rocha + topo de grama)
            span = Span(lv, 28);
            for (float x = span.x; x < span.y; x += R(rng, 10, 20))
            {
                var p = new Vector3(x, R(rng, 2, 18), R(rng, 22, 40));
                float sc = R(rng, 2, 5);
                Prim.MakeMesh(Prim.Cone, root, p, new Vector3(sc * 2f, -sc * 1.8f, sc * 2f), rock, default, "IslandRock");
                Prim.Make(PrimitiveType.Cylinder, root, p + Vector3.up * 0.15f, new Vector3(sc * 2.05f, 0.3f, sc * 2.05f), grass, default, "IslandTop");
                if (rng.NextDouble() < 0.5)   // taça de champanhe gigante
                {
                    Prim.Make(PrimitiveType.Cylinder, root, p + Vector3.up * 1.2f, new Vector3(0.2f, 1f, 0.2f), glass, default, "GlassStem");
                    Prim.MakeMesh(Prim.Cone, root, p + Vector3.up * 3.6f, new Vector3(1.8f, -1.4f, 1.8f), glass, default, "GlassBowl");
                }
            }
            // bolhas gigantes subindo
            span = Span(lv, 12);
            for (int i = 0; i < 45; i++)
            {
                var b = Prim.Make(PrimitiveType.Sphere, root, new Vector3(R(rng, span.x, span.y), R(rng, span.z, span.w), R(rng, 6, 18)),
                    Vector3.one * R(rng, 0.6f, 2.2f), bubble, default, "Bubble");
                var bb = b.AddComponent<Bob>();
                bb.speed = R(rng, 0.3f, 1.0f);
                bb.amplitude = R(rng, 0.3f, 1.2f);
            }
            // brilhos dourados (espumante)
            for (int i = 0; i < 60; i++)
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(R(rng, span.x, span.y), R(rng, 0, span.w), R(rng, 4, 14)), Vector3.one * 0.12f, gold, default, "Sparkle");
            // nuvens sob as ilhas jogáveis (esconde o "corte" de baixo)
            var g = lv.grid;
            for (int x = 1; x < g.W - 1; x += 3)
                for (int y = 1; y < g.H - 1; y++)
                    if (g.Get(x, y) == Cell.Solid && g.Get(x, y - 1) == Cell.Empty && g.Get(x, y + 3) != Cell.Solid)
                    {
                        Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + 0.5f, y - 0.3f, R(rng, 0.5f, 2.5f)),
                            new Vector3(R(rng, 2.5f, 4f), R(rng, 1f, 1.6f), 2.5f), cloud, default, "UnderCloud");
                        break;
                    }
        }

        // ------------------------------------------------------------------ CAVERNA

        static void Cave(Transform root, Level lv, System.Random rng)
        {
            var rock = Mat.Textured(ProcTex.Rock, new Color(0.7f, 0.65f, 0.85f), 0.3f, 0f, new Vector2(4, 4));
            var rockDark = Mat.Color(new Color(0.16f, 0.14f, 0.22f), 0.2f);
            var crystal = Mat.Glow(new Color(0.3f, 0.95f, 0.85f), 1.8f);
            var crystal2 = Mat.Glow(new Color(0.85f, 0.4f, 1f), 1.6f);
            var mushroom = Mat.Glow(new Color(0.4f, 1f, 0.5f), 1.4f);
            var stem = Mat.Color(new Color(0.85f, 0.85f, 0.75f), 0.3f);
            var g = lv.grid;
            // paredão do fundo
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, g.H * 0.5f, 40), new Vector3(g.W + 150, g.H + 120, 2), rock, default, "CaveWall");
            var s = Span(lv, 25);
            for (int i = 0; i < (int)((s.y - s.x) * (s.w - s.z) / 60f); i++)
            {
                var p = new Vector3(R(rng, s.x, s.y), R(rng, s.z, s.w), R(rng, 15, 35));
                float h = R(rng, 3, 9);
                bool down = rng.Next(2) == 0;   // estalactite ou estalagmite
                Prim.MakeMesh(Prim.Cone, root, p, new Vector3(R(rng, 1.5f, 3f), down ? -h : h, R(rng, 1.5f, 3f)), rockDark, default, "Stalactite");
            }
            // cristais brilhantes e cogumelos perto do terreno
            s = Span(lv, 8);
            for (int i = 0; i < (int)((s.y - s.x) * (s.w - s.z) / 25f); i++)
            {
                var p = new Vector3(R(rng, s.x, s.y), R(rng, s.z, s.w), R(rng, 5, 12));
                if (rng.Next(3) == 0)
                {
                    Prim.Make(PrimitiveType.Cylinder, root, p, new Vector3(0.25f, 0.6f, 0.25f), stem, default, "MushStem");
                    Prim.Make(PrimitiveType.Sphere, root, p + Vector3.up * 0.6f, new Vector3(1.4f, 0.6f, 1.4f), mushroom, default, "MushCap");
                }
                else
                    for (int k = 0; k < 3; k++)
                        Prim.Make(PrimitiveType.Cube, root, p + new Vector3(R(rng, -0.5f, 0.5f), 0, 0), new Vector3(0.4f, R(rng, 1, 2.6f), 0.4f),
                            rng.Next(2) == 0 ? crystal : crystal2, new Vector3(R(rng, -15, 15), 45, R(rng, -25, 25)), "Crystal");
            }
            // bolhas de refrigerante subindo pela caverna
            for (int i = 0; i < 40; i++)
            {
                var b = Prim.Make(PrimitiveType.Sphere, root, new Vector3(R(rng, s.x, s.y), R(rng, s.z, s.w), R(rng, 4, 10)), Vector3.one * R(rng, 0.2f, 0.6f), Mat.Glow(new Color(0.6f, 0.9f, 1f), 1.2f), default, "Bubble");
                var bb = b.AddComponent<Bob>(); bb.speed = R(rng, 0.5f, 1.5f); bb.amplitude = R(rng, 0.5f, 2f);
            }
        }

        // ------------------------------------------------------------------ CHAMINÉ DO CAFÉ

        static void CoffeeChimney(Transform root, Level lv, System.Random rng)
        {
            var g = lv.grid;
            var brick = Mat.Textured(ProcTex.Brick, new Color(0.55f, 0.32f, 0.22f), 0.2f, 0f, new Vector2(6, 30));
            var cup = Mat.Color(new Color(0.95f, 0.93f, 0.88f), 0.85f);
            var coffee = Mat.Glow(new Color(0.55f, 0.25f, 0.08f), 1.2f);
            var steam = Mat.Color(new Color(0.9f, 0.85f, 0.8f), 0.2f);
            var ember = Mat.Glow(new Color(1f, 0.45f, 0.1f), 2.5f);
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, g.H * 0.5f, 9f), new Vector3(g.W + 6, g.H + 60, 1), brick, default, "ChimneyWall");
            // xícaras gigantes empilhadas e vapor
            for (float y = 4; y < g.H + 10; y += R(rng, 9, 15))
            {
                float x = rng.Next(2) == 0 ? R(rng, -14, -6) : g.W + R(rng, 6, 14);
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, y, 20), new Vector3(7, 3, 7), cup, default, "Cup");
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, y + 2.95f, 20), new Vector3(6.4f, 0.1f, 6.4f), coffee, default, "CupCoffee");
                for (int k = 0; k < 4; k++)
                {
                    var st = Prim.Make(PrimitiveType.Sphere, root, new Vector3(x + R(rng, -2, 2), y + 4 + k * 1.8f, 20), Vector3.one * R(rng, 2, 3.5f), steam, default, "Steam");
                    var bb = st.AddComponent<Bob>(); bb.speed = R(rng, 0.5f, 1f); bb.amplitude = 0.6f;
                }
            }
            // brasas subindo
            for (int i = 0; i < 70; i++)
            {
                var e = Prim.Make(PrimitiveType.Cube, root, new Vector3(R(rng, -4, g.W + 4), R(rng, 0, g.H), R(rng, 3, 8)), Vector3.one * 0.12f, ember, default, "Ember");
                var bb = e.AddComponent<Bob>(); bb.speed = R(rng, 1, 3); bb.amplitude = R(rng, 0.3f, 1f);
            }
            // café quente no fundo da chaminé
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, -3f, 3f), new Vector3(g.W + 60, 2, 30), coffee, default, "CoffeePool");
        }

        // ------------------------------------------------------------------ FÁBRICA DE VIDRO

        static void GlassFactory(Transform root, Level lv, System.Random rng)
        {
            var g = lv.grid;
            var wall = Mat.Textured(ProcTex.Metal, new Color(0.45f, 0.55f, 0.6f), 0.4f, 0.4f, new Vector2(30, 4));
            var glass = Mat.Color(new Color(0.65f, 0.9f, 0.95f), 0.97f, 0.1f);
            var glassG = Mat.Color(new Color(0.35f, 0.75f, 0.45f), 0.97f, 0.1f);
            var belt = Mat.Color(new Color(0.12f, 0.12f, 0.14f), 0.4f, 0.3f);
            var furnace = Mat.Glow(new Color(1f, 0.5f, 0.15f), 2.2f);
            var steel = Mat.Color(new Color(0.5f, 0.55f, 0.6f), 0.6f, 0.6f);
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, g.H * 0.5f + 4, 30), new Vector3(g.W + 120, g.H + 40, 1), wall, default, "FactoryWall");
            var s = Span(lv, 14);
            // esteiras com garrafas
            foreach (float y in new[] { 3f, 9f, 15f })
            {
                Prim.Make(PrimitiveType.Cube, root, new Vector3((s.x + s.y) * 0.5f, y, 14), new Vector3(s.y - s.x, 0.4f, 1.6f), belt, default, "Belt");
                for (float x = s.x; x < s.y; x += R(rng, 1.4f, 2.2f))
                {
                    var b = Prim.Pivot(root, new Vector3(x, y + 0.2f, 14), "Bottle");
                    var m = rng.Next(2) == 0 ? glass : glassG;
                    Prim.Make(PrimitiveType.Cylinder, b, new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0.5f, 0.6f), m);
                    Prim.Make(PrimitiveType.Cylinder, b, new Vector3(0, 1.2f, 0), new Vector3(0.22f, 0.25f, 0.22f), m);
                }
            }
            // fornos acesos
            for (float x = s.x; x < s.y; x += R(rng, 16, 26))
            {
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, 6, 24), new Vector3(7, 12, 5), steel, default, "Furnace");
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, 4, 21.4f), new Vector3(3.5f, 2.5f, 0.2f), furnace, default, "FurnaceMouth");
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, 16, 24), new Vector3(2, 4, 2), steel, default, "Chimney");
            }
            // garrafões gigantes em prateleiras próximas
            s = Span(lv, 6);
            for (float x = s.x; x < s.y; x += R(rng, 6, 11))
            {
                var p = new Vector3(x, R(rng, 1, Mathf.Max(2, g.H - 4)), 6);
                Prim.Make(PrimitiveType.Sphere, root, p, new Vector3(1.8f, 2.2f, 1.8f), rng.Next(2) == 0 ? glass : glassG, default, "Jug");
                Prim.Make(PrimitiveType.Cylinder, root, p + Vector3.up * 1.3f, new Vector3(0.5f, 0.4f, 0.5f), glass, default, "JugNeck");
            }
        }

        // ------------------------------------------------------------------ LABIRINTO

        static void Labyrinth(Transform root, Level lv, System.Random rng)
        {
            var g = lv.grid;
            var stone = Mat.Textured(ProcTex.Brick, new Color(0.7f, 0.6f, 0.55f), 0.15f, 0f, new Vector2(20, 8));
            var stoneDark = Mat.Textured(ProcTex.Brick, new Color(0.45f, 0.38f, 0.35f), 0.15f, 0f, new Vector2(2, 6));
            var banner = Mat.Color(new Color(0.65f, 0.08f, 0.06f), 0.3f);
            var fire = Mat.Glow(new Color(1f, 0.45f, 0.1f), 3f);
            var gold = Mat.Color(new Color(0.85f, 0.65f, 0.2f), 0.7f, 0.8f);
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, g.H * 0.5f + 6, 24), new Vector3(g.W + 120, g.H + 50, 1), stone, default, "LabyrinthWall");
            var s = Span(lv, 12);
            // colunas, arcos, estandartes e tochas
            for (float x = s.x; x < s.y; x += R(rng, 7, 11))
            {
                Prim.Make(PrimitiveType.Cube, root, new Vector3(x, g.H * 0.5f, 12), new Vector3(1.4f, g.H + 20, 1.4f), stoneDark, default, "Column");
                if (rng.Next(2) == 0)
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(x + 1.2f, R(rng, 6, Mathf.Max(7, g.H - 3)), 11.6f), new Vector3(1.4f, 3.5f, 0.1f), banner, default, "Banner");
            }
            int lights = 0;
            for (float x = 4; x < g.W; x += R(rng, 9, 14))
            {
                float y = R(rng, 3.5f, 6f);
                var t = Prim.Pivot(root, new Vector3(x, y, 3.6f), "Torch");
                Prim.Make(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.15f, 0.5f, 0.15f), gold);
                Prim.Make(PrimitiveType.Sphere, t, new Vector3(0, 0.7f, 0), new Vector3(0.35f, 0.55f, 0.35f), fire);
                if (lights++ % 2 == 0)
                {
                    var l = t.gameObject.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.25f); l.range = 9f; l.intensity = 2.5f;
                }
                t.gameObject.AddComponent<Bob>().amplitude = 0.03f;
            }
            // cabeças de touro em relevo
            for (float x = s.x + 10; x < s.y; x += R(rng, 30, 45))
            {
                var h = Prim.Pivot(root, new Vector3(x, R(rng, 8, 14), 22), "BullRelief");
                Prim.Make(PrimitiveType.Sphere, h, Vector3.zero, new Vector3(3, 3.5f, 1.5f), stoneDark);
                Prim.MakeMesh(Prim.Cone, h, new Vector3(-1.6f, 1.2f, 0), new Vector3(0.6f, 2.2f, 0.6f), gold, new Vector3(0, 0, 50), "Horn");
                Prim.MakeMesh(Prim.Cone, h, new Vector3(1.6f, 1.2f, 0), new Vector3(0.6f, 2.2f, 0.6f), gold, new Vector3(0, 0, -50), "Horn");
            }
        }

        // ------------------------------------------------------------------ REFINARIA

        static void Refinery(Transform root, Level lv, System.Random rng)
        {
            var steel = Mat.Color(new Color(0.42f, 0.44f, 0.50f), 0.6f, 0.6f);
            var steelDark = Mat.Color(new Color(0.25f, 0.24f, 0.30f), 0.5f, 0.5f);
            var rust = Mat.Color(new Color(0.55f, 0.30f, 0.16f), 0.3f, 0.3f);
            var pipeRed = Mat.Color(new Color(0.70f, 0.18f, 0.12f), 0.5f, 0.4f);
            var pipeYel = Mat.Color(new Color(0.85f, 0.65f, 0.12f), 0.5f, 0.4f);
            var tank = Mat.Color(new Color(0.58f, 0.58f, 0.62f), 0.7f, 0.7f);
            var glowG = Mat.Glow(new Color(0.3f, 1f, 0.35f), 2.2f);
            var glowO = Mat.Glow(new Color(1f, 0.55f, 0.15f), 2.5f);
            var wall = Mat.Textured(ProcTex.Metal, new Color(0.45f, 0.40f, 0.48f), 0.4f, 0.4f, new Vector2(4, 4));

            // chaminés e tanques ao fundo
            var s = Span(lv, 80);
            for (float x = s.x; x < s.y; x += R(rng, 8, 16))
            {
                float h = R(rng, 30, 70);
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, s.z + h * 0.5f, 80), new Vector3(R(rng, 3, 6), h * 0.5f, 3), steelDark, default, "Stack");
                Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, s.z + h + 0.5f, 79), Vector3.one * 1.2f, glowO, default, "StackLight");
            }
            // tanques grandes de energético
            s = Span(lv, 40);
            for (float y = s.z; y < s.w; y += R(rng, 18, 26))
                for (float x = s.x; x < s.y; x += R(rng, 12, 20))
                {
                    float r = R(rng, 4, 7);
                    Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, y, 40), new Vector3(r * 2, r * 1.4f, r * 2), tank, default, "Tank");
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(x, y + r * 0.2f, 40 - r - 0.05f), new Vector3(r * 0.5f, r * 1.6f, 0.2f), glowG, default, "TankGauge");
                    Prim.Make(PrimitiveType.Sphere, root, new Vector3(x, y + r * 1.4f, 40), new Vector3(r * 2, r * 0.8f, r * 2), tank, default, "TankTop");
                }
            // tubulações horizontais e verticais (camada média)
            s = Span(lv, 16);
            for (float y = s.z; y < s.w; y += R(rng, 4, 8))
            {
                var m = rng.Next(3) == 0 ? pipeRed : (rng.Next(2) == 0 ? pipeYel : steel);
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3((s.x + s.y) * 0.5f, y, R(rng, 12, 18)),
                    new Vector3(R(rng, 0.6f, 1.4f), (s.y - s.x) * 0.5f, R(rng, 0.6f, 1.4f)), m, new Vector3(0, 0, 90), "PipeH");
            }
            for (float x = s.x; x < s.y; x += R(rng, 5, 10))
            {
                var m = rng.Next(2) == 0 ? rust : steel;
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(x, (s.z + s.w) * 0.5f, R(rng, 10, 15)),
                    new Vector3(R(rng, 0.5f, 1.0f), (s.w - s.z) * 0.5f, R(rng, 0.5f, 1.0f)), m, default, "PipeV");
            }
            // engrenagens girando
            for (int i = 0; i < Mathf.Max(4, lv.grid.H / 10); i++)
            {
                var gear = new GameObject("Gear").transform;
                gear.SetParent(root, false);
                gear.localPosition = new Vector3(R(rng, s.x, s.y), R(rng, s.z, s.w), 9);
                float r = R(rng, 2, 4);
                for (int k = 0; k < 10; k++)
                    Prim.Make(PrimitiveType.Cube, gear, Quaternion.Euler(0, 0, k * 36) * new Vector3(r, 0, 0), new Vector3(1, 0.8f, 0.6f), steelDark, new Vector3(0, 0, k * 36));
                Prim.Make(PrimitiveType.Cylinder, gear, Vector3.zero, new Vector3(r * 2, 0.3f, r * 2), steel, new Vector3(90, 0, 0));
                gear.gameObject.AddComponent<Spin>().speed = new Vector3(0, 0, R(rng, -30, 30));
            }
            // estrutura da torre logo atrás do terreno: painéis, vigas e treliças (deixa ver o fundo entre elas)
            var g = lv.grid;
            for (float y = 0; y < g.H + 8; y += 10)
            {
                Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, y, 6.5f), new Vector3(g.W + 4, 3.5f, 0.5f), wall, default, "Panel");
                Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W * 0.5f, y + 1.9f, 6.2f), new Vector3(g.W + 4, 0.3f, 0.3f), pipeYel, default, "Rail");
                for (float x = 3; x < g.W - 2; x += 8)
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(x + R(rng, -1, 1), y, 6.2f), new Vector3(2.2f, 1.2f, 0.1f), Mat.Glow(new Color(1f, 0.75f, 0.4f), 1.2f), default, "Window");
                for (float x = 0; x <= g.W; x += 6.5f)
                {
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(x, y + 5, 6.6f), new Vector3(0.35f, 7.5f, 0.35f), steelDark, new Vector3(0, 0, 40), "Brace");
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(x, y + 5, 6.6f), new Vector3(0.35f, 7.5f, 0.35f), steelDark, new Vector3(0, 0, -40), "Brace");
                }
            }
            Prim.Make(PrimitiveType.Cube, root, new Vector3(-0.5f, g.H * 0.5f, 6.5f), new Vector3(1.5f, g.H + 20, 1.5f), steelDark, default, "Column");
            Prim.Make(PrimitiveType.Cube, root, new Vector3(g.W + 0.5f, g.H * 0.5f, 6.5f), new Vector3(1.5f, g.H + 20, 1.5f), steelDark, default, "Column");
            // canos em primeiro plano nas laterais
            for (float y = 3; y < g.H; y += R(rng, 7, 13))
                Prim.Make(PrimitiveType.Cylinder, root, new Vector3(rng.Next(2) == 0 ? -1.5f : g.W + 1.5f, y, -2.8f),
                    new Vector3(0.9f, 5f, 0.9f), pipeRed, new Vector3(0, 0, 90), "FgPipe");
        }
    }

    /// <summary>Flocos de neve que caem em volta da câmera (reciclados).</summary>
    public class Snowfall : MonoBehaviour
    {
        Transform[] flakes;
        Vector3[] vel;

        void Start()
        {
            var mat = Mat.Glow(new Color(0.9f, 0.95f, 1f), 1.1f);
            flakes = new Transform[140];
            vel = new Vector3[flakes.Length];
            for (int i = 0; i < flakes.Length; i++)
            {
                flakes[i] = Prim.Make(PrimitiveType.Cube, transform, Vector3.zero, Vector3.one * Random.Range(0.05f, 0.12f), mat, Random.insideUnitSphere * 180, "Flake").transform;
                Respawn(i, true);
            }
        }

        void Respawn(int i, bool anywhere)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var c = cam.transform.position;
            float z = Random.Range(-4f, 10f);
            float halfH = (z - c.z) * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;
            flakes[i].position = new Vector3(c.x + Random.Range(-halfW, halfW), anywhere ? c.y + Random.Range(-halfH, halfH) : c.y + halfH + 1f, z);
            vel[i] = new Vector3(Random.Range(-0.6f, 0.2f), Random.Range(-2.2f, -1f), 0);
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null || flakes == null) return;
            float dt = Time.deltaTime;
            var c = cam.transform.position;
            for (int i = 0; i < flakes.Length; i++)
            {
                var f = flakes[i];
                f.position += (vel[i] + new Vector3(Mathf.Sin(Time.time + i) * 0.4f, 0, 0)) * dt;
                f.Rotate(60 * dt, 90 * dt, 0);
                float halfH = (f.position.z - c.z) * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad);
                float halfW = halfH * cam.aspect;
                bool below = f.position.y < c.y - halfH - 1f;
                if (below || Mathf.Abs(f.position.x - c.x) > halfW + 2f) Respawn(i, !below);
            }
        }
    }

    /// <summary>Sobe e desce suavemente (bolhas do céu).</summary>
    public class Bob : MonoBehaviour
    {
        public float speed = 1f, amplitude = 0.5f;
        Vector3 basePos;
        float phase;

        void Start() { basePos = transform.localPosition; phase = Random.value * 10f; }

        void Update() => transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}

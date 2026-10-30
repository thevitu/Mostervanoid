using System.IO;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Skins do corpo da lata. Ficam em Assets/StreamingAssets/Skins/form0.png ... form7.png
    /// (no build: &lt;Jogo&gt;_Data/StreamingAssets/Skins). A imagem é enrolada em volta da lata:
    /// o CENTRO horizontal da imagem fica virado para a câmera. Proporção recomendada 2:1 (ex. 512x256).
    /// Se o arquivo não existir, uma skin padrão é gerada.
    /// </summary>
    public static class SkinLibrary
    {
        public const int Width = 512, Height = 256;
        static readonly Texture2D[] skins = new Texture2D[Forms.Count];

        public static string Folder => Path.Combine(Application.streamingAssetsPath, "Skins");
        public static string PathFor(int form) => Path.Combine(Folder, $"form{form}.png");

        public static Texture2D Get(int form)
        {
            form = Mathf.Clamp(form, 0, Forms.Count - 1);
            if (skins[form] == null) skins[form] = Load(form);
            return skins[form];
        }

        public static void ReloadAll()
        {
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] != null) Object.Destroy(skins[i]);
                skins[i] = null;
            }
        }

        static Texture2D Load(int form)
        {
            try
            {
                string p = PathFor(form);
                if (File.Exists(p))
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (t.LoadImage(File.ReadAllBytes(p)))
                    {
                        t.name = $"skin{form}";
                        t.wrapMode = TextureWrapMode.Repeat;
                        t.filterMode = FilterMode.Bilinear;
                        t.anisoLevel = 4;
                        return t;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Mostervanoid] Falha ao carregar skin {form}: {e.Message}");
            }
            return Generate(form);
        }

        /// <summary>Gera a skin padrão de uma forma (também usada pelo editor para exportar os PNGs).</summary>
        public static Texture2D Generate(int form)
        {
            var info = Forms.Get(form);
            var px = new Color[Width * Height];
            Color dark = info.color * 0.55f; dark.a = 1;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    float v = y / (float)Height;
                    var c = Color.Lerp(dark, info.color, Mathf.Sin(v * Mathf.PI) * 0.9f + 0.1f);
                    // faixas metálicas em cima e embaixo
                    if (y < 14 || y > Height - 15) c = Color.Lerp(new Color(0.75f, 0.77f, 0.80f), Color.white, (x % 32) / 64f);
                    else if (y < 20 || y > Height - 21) c = info.accent;
                    // faixa diagonal
                    float d = (x - y * 0.6f) % 128f;
                    if (d < 0) d += 128f;
                    if (y >= 20 && y <= Height - 21 && d < 10f) c = Color.Lerp(c, info.accent, 0.55f);
                    c.a = 1f;
                    px[y * Width + x] = c;
                }

            // raios atrás do logo
            DrawBolt(px, 256 - 150, 128, 1.0f, info.accent);
            DrawBolt(px, 256 + 150, 128, 1.0f, info.accent);
            DrawBolt(px, 0, 128, 1.6f, info.accent);
            DrawBolt(px, Width, 128, 1.6f, info.accent);

            var shadow = new Color(0, 0, 0, 1);
            PixelFont.Draw(px, Width, Height, "MOSTER", 256, 150, 6, Color.white, shadow);
            PixelFont.Draw(px, Width, Height, info.flavor, 256, 92, 4, info.accent, shadow);
            PixelFont.Draw(px, Width, Height, "ENERGY", 256, 52, 2, Color.white);

            var t = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                name = $"skin{form}_default", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4
            };
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static void DrawBolt(Color[] px, int cx, int cy, float s, Color col)
        {
            // raio em zig-zag (polígono)
            Vector2[] poly =
            {
                new Vector2(10, 60), new Vector2(-18, 2), new Vector2(0, 2), new Vector2(-12, -60),
                new Vector2(20, -6), new Vector2(2, -6), new Vector2(16, 60),
            };
            for (int i = 0; i < poly.Length; i++) poly[i] = poly[i] * s + new Vector2(cx, cy);
            int minX = Mathf.FloorToInt(cx - 30 * s), maxX = Mathf.CeilToInt(cx + 30 * s);
            int minY = Mathf.FloorToInt(cy - 62 * s), maxY = Mathf.CeilToInt(cy + 62 * s);
            for (int y = Mathf.Max(20, minY); y <= Mathf.Min(Height - 21, maxY); y++)
                for (int xx = minX; xx <= maxX; xx++)
                {
                    if (!Inside(poly, new Vector2(xx, y))) continue;
                    int x = ((xx % Width) + Width) % Width;
                    var c = Color.Lerp(px[y * Width + x], col, 0.8f);
                    c.a = 1;
                    px[y * Width + x] = c;
                }
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }
}

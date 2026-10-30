using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Texturas procedurais para o cenário (sem precisar de arquivos de arte).</summary>
    public static class ProcTex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float Fbm(float x, float y, int seed)
        {
            float v = 0, amp = 0.5f, f = 1;
            for (int i = 0; i < 4; i++)
            {
                v += amp * Mathf.PerlinNoise(x * f + seed * 13.1f, y * f + seed * 7.7f);
                f *= 2; amp *= 0.5f;
            }
            return v;
        }

        static Texture2D Make(string key, int size, System.Func<int, int, Color> px)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = key, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    cols[y * size + x] = px(x, y);
            t.SetPixels(cols);
            t.Apply(true);
            cache[key] = t;
            return t;
        }

        // Pedras/terra (frente do terreno da selva)
        public static Texture2D Dirt => Make("dirt", 128, (x, y) =>
        {
            float n = Fbm(x / 24f, y / 24f, 1);
            var c = Color.Lerp(new Color(0.36f, 0.22f, 0.12f), new Color(0.55f, 0.36f, 0.20f), n);
            // pedrinhas
            float s = Fbm(x / 9f, y / 9f, 5);
            if (s > 0.62f) c = Color.Lerp(c, new Color(0.62f, 0.58f, 0.52f), (s - 0.62f) * 5f);
            if (s > 0.60f && s < 0.62f) c *= 0.7f;
            return c * (0.9f + Hash(x, y, 2) * 0.12f);
        });

        // Frente com borda de grama no topo
        public static Texture2D GrassEdge => Make("grassedge", 128, (x, y) =>
        {
            var d = Dirt.GetPixel(x, y);
            float edge = 128 - 34 + Mathf.Sin(x * 0.45f) * 5f + Mathf.Sin(x * 1.3f) * 3f;
            if (y > edge)
            {
                float n = Fbm(x / 8f, y / 8f, 3);
                return Color.Lerp(new Color(0.18f, 0.55f, 0.12f), new Color(0.40f, 0.80f, 0.20f), n) * (0.9f + Hash(x, y, 9) * 0.15f);
            }
            if (y > edge - 4) return d * 0.6f;
            return d;
        });

        // Topo de grama
        public static Texture2D GrassTop => Make("grasstop", 128, (x, y) =>
        {
            float n = Fbm(x / 14f, y / 14f, 4);
            var c = Color.Lerp(new Color(0.20f, 0.52f, 0.12f), new Color(0.45f, 0.82f, 0.22f), n);
            if (Hash(x, y, 11) > 0.93f) c = new Color(0.95f, 0.85f, 0.30f); // florzinhas
            return c * (0.9f + Hash(x, y, 7) * 0.15f);
        });

        // Painel de metal (frente da refinaria)
        public static Texture2D Metal => Make("metal", 128, (x, y) =>
        {
            float n = Fbm(x / 30f, y / 30f, 8);
            var c = Color.Lerp(new Color(0.32f, 0.35f, 0.40f), new Color(0.48f, 0.52f, 0.58f), n);
            int px = x % 64, py = y % 64;
            if (px < 2 || py < 2) c *= 0.55f;               // juntas
            if (px > 61 || py > 61) c *= 1.25f;             // brilho
            bool rivet = (Mathf.Abs(px - 7) < 2 || Mathf.Abs(px - 56) < 2) && (Mathf.Abs(py - 7) < 2 || Mathf.Abs(py - 56) < 2);
            if (rivet) c = new Color(0.75f, 0.76f, 0.80f);
            // ferrugem
            float r = Fbm(x / 10f, y / 10f, 12);
            if (r > 0.66f) c = Color.Lerp(c, new Color(0.55f, 0.28f, 0.12f), (r - 0.66f) * 4f);
            return c;
        });

        // Frente de metal com faixa de aviso amarela/preta no topo
        public static Texture2D MetalEdge => Make("metaledge", 128, (x, y) =>
        {
            if (y > 108)
            {
                bool stripe = ((x + y) / 12) % 2 == 0;
                return stripe ? new Color(0.95f, 0.75f, 0.10f) : new Color(0.12f, 0.12f, 0.12f);
            }
            if (y > 104) return new Color(0.2f, 0.2f, 0.22f);
            return Metal.GetPixel(x, y);
        });

        // Grade metálica (topo)
        public static Texture2D Grate => Make("grate", 64, (x, y) =>
        {
            bool bar = (x % 16) < 3 || (y % 16) < 3;
            return bar ? new Color(0.55f, 0.58f, 0.62f) : new Color(0.20f, 0.22f, 0.25f);
        });

        public static Texture2D Wood => Make("wood", 64, (x, y) =>
        {
            float n = Mathf.Sin(x * 0.25f + Fbm(x / 10f, y / 30f, 21) * 6f) * 0.5f + 0.5f;
            var c = Color.Lerp(new Color(0.45f, 0.27f, 0.12f), new Color(0.68f, 0.45f, 0.22f), n);
            if (y % 32 < 2) c *= 0.5f;
            return c;
        });

        public static Texture2D Cracked => Make("cracked", 64, (x, y) =>
        {
            var c = Color.Lerp(new Color(0.45f, 0.40f, 0.35f), new Color(0.60f, 0.55f, 0.48f), Fbm(x / 12f, y / 12f, 31));
            float crack = Mathf.Abs(Mathf.Sin(x * 0.3f + Mathf.Sin(y * 0.2f) * 2f) * 20f - (y % 21));
            if (crack < 1.6f) c *= 0.3f;
            if (Mathf.Abs(x - y) < 2 || Mathf.Abs(x + y - 64) < 2) c *= 0.35f;
            return c;
        });

        public static Texture2D Reinforced => Make("reinforced", 64, (x, y) =>
        {
            var c = new Color(0.55f, 0.15f, 0.12f) * (0.85f + Fbm(x / 9f, y / 9f, 41) * 0.3f);
            if (x < 4 || x > 59 || y < 4 || y > 59) c = new Color(0.25f, 0.25f, 0.28f);
            if (Mathf.Abs(x - y) < 4 || Mathf.Abs(x + y - 63) < 4) c = new Color(0.30f, 0.30f, 0.33f);
            return c;
        });

        // Neve (topo)
        public static Texture2D Snow => Make("snow", 128, (x, y) =>
        {
            float n = Fbm(x / 16f, y / 16f, 51);
            var c = Color.Lerp(new Color(0.82f, 0.88f, 0.96f), Color.white, n);
            if (Hash(x, y, 52) > 0.97f) c = new Color(0.75f, 0.9f, 1f) * 1.1f;   // brilhos
            return c;
        });

        // Gelo (frente)
        public static Texture2D Ice => Make("ice", 128, (x, y) =>
        {
            float n = Fbm(x / 22f, y / 22f, 61);
            var c = Color.Lerp(new Color(0.35f, 0.62f, 0.85f), new Color(0.70f, 0.88f, 0.98f), n);
            float crack = Mathf.Abs(Mathf.Sin(x * 0.11f + Fbm(x / 9f, y / 9f, 62) * 7f));
            if (crack < 0.05f) c = Color.Lerp(c, Color.white, 0.6f);
            return c;
        });

        // Frente de gelo com camada de neve no topo
        public static Texture2D SnowEdge => Make("snowedge", 128, (x, y) =>
        {
            float edge = 128 - 30 + Mathf.Sin(x * 0.3f) * 4f + Mathf.Sin(x * 0.9f) * 2f;
            if (y > edge) return Snow.GetPixel(x, y);
            if (y > edge - 3) return new Color(0.6f, 0.75f, 0.9f);
            return Ice.GetPixel(x, y);
        });

        // Tijolos de pedra (labirinto)
        public static Texture2D Brick => Make("brick", 128, (x, y) =>
        {
            int row = y / 16;
            int bx = (x + (row % 2) * 16) % 32;
            bool mortar = (y % 16) < 2 || bx < 2;
            float n = Fbm(x / 12f, y / 12f, 71);
            var c = Color.Lerp(new Color(0.42f, 0.36f, 0.32f), new Color(0.62f, 0.55f, 0.48f), n);
            c *= 0.85f + Hash(x / 32 + row * 7, row, 72) * 0.3f;
            if (mortar) c = new Color(0.22f, 0.19f, 0.17f);
            return c;
        });

        // Rocha de caverna
        public static Texture2D Rock => Make("rock", 128, (x, y) =>
        {
            float n = Fbm(x / 18f, y / 18f, 81);
            float v = Fbm(x / 6f, y / 6f, 82);
            var c = Color.Lerp(new Color(0.22f, 0.20f, 0.28f), new Color(0.42f, 0.38f, 0.50f), n);
            if (v > 0.64f) c = Color.Lerp(c, new Color(0.30f, 0.85f, 0.80f), (v - 0.64f) * 3f);   // veios minerais
            return c;
        });

        /// <summary>Gradiente vertical simples.</summary>
        public static Texture2D Gradient(string key, Color bottom, Color top) => Make(key, 64, (x, y) => Color.Lerp(bottom, top, y / 63f));
    }
}

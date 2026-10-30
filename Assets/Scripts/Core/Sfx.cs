using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Efeitos sonoros 8-bit sintetizados em tempo de execução (sem arquivos de áudio).</summary>
    public static class Sfx
    {
        public static AudioClip Shoot, Special, Jump, Hurt, Explode, Tink, Land, Unlock, Teleport, Slide, Dash, Pound, Menu, BossHit;
        static AudioSource source;
        const int Rate = 22050;

        public static void Init(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.35f;
            Shoot = Tone("shoot", 0.08f, 900, 500, Wave.Square);
            Special = Tone("special", 0.25f, 300, 1200, Wave.Square, 0.5f);
            Jump = Tone("jump", 0.12f, 300, 700, Wave.Square, 0.35f);
            Hurt = Tone("hurt", 0.25f, 600, 120, Wave.Saw);
            Explode = Noise("explode", 0.35f);
            Tink = Tone("tink", 0.06f, 1800, 1600, Wave.Square, 0.25f);
            Land = Tone("land", 0.05f, 180, 90, Wave.Square, 0.25f);
            Unlock = Arp("unlock", new[] { 523f, 659f, 784f, 1046f }, 0.12f);
            Teleport = Tone("teleport", 0.5f, 200, 1800, Wave.Square, 0.4f);
            Slide = Noise("slide", 0.15f, 0.3f);
            Dash = Tone("dash", 0.15f, 200, 900, Wave.Saw, 0.35f);
            Pound = Noise("pound", 0.3f, 0.8f);
            Menu = Tone("menu", 0.04f, 1200, 1200, Wave.Square, 0.25f);
            BossHit = Tone("bosshit", 0.08f, 400, 200, Wave.Square, 0.4f);
        }

        public static void Play(AudioClip c, float vol = 1f)
        {
            if (source != null && c != null) source.PlayOneShot(c, vol);
        }

        enum Wave { Square, Saw }

        static AudioClip Tone(string name, float dur, float f0, float f1, Wave w, float vol = 0.4f)
        {
            int n = (int)(dur * Rate);
            var data = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float frac = phase - Mathf.Floor(phase);
                float s = w == Wave.Square ? (frac < 0.5f ? 1 : -1) : frac * 2 - 1;
                data[i] = s * vol * (1 - t);
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Noise(string name, float dur, float vol = 0.5f)
        {
            int n = (int)(dur * Rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float hold = 0; int step = 1;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                step = 1 + (int)(t * 12);
                if (i % step == 0) hold = (float)rng.NextDouble() * 2 - 1;
                data[i] = hold * vol * (1 - t);
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Arp(string name, float[] notes, float each)
        {
            int per = (int)(each * Rate);
            var data = new float[per * notes.Length];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    float s = (t * notes[k] % 1f) < 0.5f ? 1 : -1;
                    data[k * per + i] = s * 0.3f * (1 - i / (float)per * 0.5f);
                }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}

using UnityEngine;

namespace Mostervanoid
{
    public enum FormId
    {
        Original = 0,   // tiro especial 5x dano
        Slide = 1,
        SuperJump = 2,
        Dash = 3,
        DoubleJump = 4,
        GroundPound = 5,
        Hook = 6,
        WallImpact = 7,
    }

    public struct FormInfo
    {
        public string flavor;      // sabor (texto da lata)
        public string ability;     // nome da habilidade
        public string howTo;       // instrução curta
        public Color color;        // cor principal (HUD, fallback da skin)
        public Color accent;
    }

    public static class Forms
    {
        public const int Count = 8;
        public const float SpecialCooldown = 3f;

        public static readonly FormInfo[] All =
        {
            new FormInfo { flavor = "ORIGINAL", ability = "Tiro Especial", howTo = "L/3: tiro com 5x de dano",
                color = new Color(0.10f, 0.35f, 0.85f), accent = new Color(0.85f, 0.90f, 0.95f) },
            new FormInfo { flavor = "CITRUS", ability = "Slide", howTo = "L/3 (ou baixo + L/3) no chão: desliza por frestas",
                color = new Color(0.98f, 0.82f, 0.10f), accent = new Color(0.20f, 0.55f, 0.10f) },
            new FormInfo { flavor = "BERRY", ability = "Super Pulo", howTo = "Segure L/3 no chão e solte: pulo 3x mais alto",
                color = new Color(0.55f, 0.15f, 0.70f), accent = new Color(0.95f, 0.40f, 0.70f) },
            new FormInfo { flavor = "MANGO", ability = "Dash", howTo = "L/3: arrancada invulnerável (funciona no ar)",
                color = new Color(1.00f, 0.50f, 0.08f), accent = new Color(1.00f, 0.90f, 0.30f) },
            new FormInfo { flavor = "ICE", ability = "Pulo Duplo", howTo = "Pule de novo no ar. L/3: tiro especial",
                color = new Color(0.45f, 0.85f, 0.95f), accent = new Color(1f, 1f, 1f) },
            new FormInfo { flavor = "COLA", ability = "Impacto no Chão", howTo = "L/3 no ar: investe contra o chão (quebra piso rachado)",
                color = new Color(0.45f, 0.10f, 0.08f), accent = new Color(0.95f, 0.80f, 0.55f) },
            new FormInfo { flavor = "KIWI", ability = "Gancho", howTo = "Segure L/3 no ar perto de um gancho: fica suspenso até 3s",
                color = new Color(0.40f, 0.70f, 0.15f), accent = new Color(0.30f, 0.20f, 0.10f) },
            new FormInfo { flavor = "CHILI", ability = "Impacto na Parede", howTo = "L/3: investida que destrói portas reforçadas",
                color = new Color(0.85f, 0.08f, 0.08f), accent = new Color(1.00f, 0.75f, 0.10f) },
        };

        public static FormInfo Get(FormId id) => All[(int)id];
        public static FormInfo Get(int id) => All[Mathf.Clamp(id, 0, Count - 1)];

        /// <summary>Formas cujo botão especial é o tiro especial 5x.</summary>
        public static bool UsesSpecialShot(FormId id) => id == FormId.Original || id == FormId.DoubleJump;
    }
}

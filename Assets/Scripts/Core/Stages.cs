namespace Mostervanoid
{
    public enum Theme { Jungle, Refinery, Vineyard, Glacier, Sky, Cave, Coffee, GlassFactory, Labyrinth }
    public enum BossKind { None, WaterJug, WineBarrel, SodaBottle, Shaker, Champagne, Teapot, JuiceBox, GlassJar, Minotaur }

    public class StageInfo
    {
        public string id;          // nome do arquivo em Resources/Levels/<id>.txt
        public string title;
        public Theme theme;
        public BossKind boss;
        public int rewardForm;     // forma liberada ao derrotar o chefe (-1 = nenhuma)
        public string next;        // fase acessada pelo portal após o chefe (null = em breve)
        public bool final;         // derrotar o chefe encerra o jogo (animação final)
    }

    /// <summary>
    /// Registro das fases (um chefe por fase). Para adicionar uma fase nova: crie Resources/Levels/stageN.txt
    /// e adicione uma entrada aqui (e aponte o 'next' da fase anterior para ela).
    /// </summary>
    public static class Stages
    {
        public static readonly StageInfo[] All =
        {
            new StageInfo { id = "stage1", title = "Selva Gaseificada", theme = Theme.Jungle,
                boss = BossKind.WaterJug, rewardForm = (int)FormId.Slide, next = "stage2" },
            new StageInfo { id = "stage2", title = "Torre da Refinaria", theme = Theme.Refinery,
                boss = BossKind.WineBarrel, rewardForm = (int)FormId.SuperJump, next = "stage3" },
            new StageInfo { id = "stage3", title = "Vinhedo ao Entardecer", theme = Theme.Vineyard,
                boss = BossKind.SodaBottle, rewardForm = (int)FormId.Dash, next = "stage4" },
            new StageInfo { id = "stage4", title = "Geleira Engarrafada", theme = Theme.Glacier,
                boss = BossKind.Shaker, rewardForm = (int)FormId.DoubleJump, next = "stage5" },
            new StageInfo { id = "stage5", title = "Céu Borbulhante", theme = Theme.Sky,
                boss = BossKind.Champagne, rewardForm = (int)FormId.GroundPound, next = "stage6" },
            new StageInfo { id = "stage6", title = "Caverna Efervescente", theme = Theme.Cave,
                boss = BossKind.Teapot, rewardForm = (int)FormId.Hook, next = "stage7" },
            new StageInfo { id = "stage7", title = "Chaminé do Café", theme = Theme.Coffee,
                boss = BossKind.JuiceBox, rewardForm = (int)FormId.WallImpact, next = "stage8" },
            new StageInfo { id = "stage8", title = "Fábrica de Vidro", theme = Theme.GlassFactory,
                boss = BossKind.GlassJar, rewardForm = -1, next = "stage9" },
            new StageInfo { id = "stage9", title = "Labirinto do Minotauro", theme = Theme.Labyrinth,
                boss = BossKind.Minotaur, rewardForm = -1, next = null, final = true },
        };

        public static StageInfo Get(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return null;
        }
    }
}

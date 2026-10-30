using System.Collections.Generic;
using UnityEngine;

namespace Mostervanoid
{
    public class Checkpoint
    {
        public Vector2 pos;
        public Renderer flag;
        public bool active;
    }

    public class Hint
    {
        public Vector2 pos;
        public string text;
    }

    /// <summary>Fase carregada em memória (grade + objetos especiais).</summary>
    public class Level
    {
        public StageInfo info;
        public LevelGrid grid;
        public Transform root;

        public Vector2 start;
        public Vector2? bossWarp;

        public bool hasBoss;
        public Vector2 bossSpawn;
        public Vector2? cameraAnchor;
        public readonly List<Vector2Int> triggers = new List<Vector2Int>();
        public readonly List<Vector2Int> entryShutters = new List<Vector2Int>();
        public readonly List<Vector2Int> exitShutters = new List<Vector2Int>();
        public readonly List<GameObject> entryShutterVisuals = new List<GameObject>();

        public readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
        public readonly List<Hint> hints = new List<Hint>();
        public readonly List<Vector2> hooks = new List<Vector2>();

        public bool hasTeleport;
        public Vector2 teleportPos;
        public TeleportPad teleportPad;

        public bool hasExit;
        public Vector2 exitPos;
        public ExitPortal exitPortal;

        public Vector2 Size => new Vector2(grid.W, grid.H);
    }

    /// <summary>Formato e parser dos arquivos Resources/Levels/*.txt (ver README.txt na mesma pasta).</summary>
    public class LevelData
    {
        public readonly List<string> rows = new List<string>();
        public readonly Dictionary<char, string> hints = new Dictionary<char, string>();
        public int Width, Height;

        public static LevelData Parse(string text)
        {
            var d = new LevelData();
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("@hint"))
                {
                    var rest = line.Substring(5).Trim();
                    if (rest.Length >= 1) d.hints[rest[0]] = rest.Substring(1).Trim();
                    continue;
                }
                if (line.StartsWith("@")) continue;
                if (line.Length == 0) continue;
                d.rows.Add(line);
            }
            d.Height = d.rows.Count;
            foreach (var r in d.rows) d.Width = Mathf.Max(d.Width, r.Length);
            return d;
        }

        /// <summary>Caractere na célula (x, y) com y=0 embaixo.</summary>
        public char At(int x, int y)
        {
            var row = rows[Height - 1 - y];
            return x < row.Length ? row[x] : '.';
        }
    }
}

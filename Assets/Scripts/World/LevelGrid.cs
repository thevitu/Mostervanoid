using UnityEngine;

namespace Mostervanoid
{
    public enum Cell : byte
    {
        Empty,
        Solid,
        OneWay,      // plataforma atravessável por baixo
        Spike,       // não sólido, mata ao tocar
        Breakable,   // porta reforçada: só a Forma 7 (impacto na parede) destrói
        Cracked,     // piso rachado: só a Forma 5 (impacto no chão) destrói
        Shutter,     // portão de chefe fechado
        WindL,       // jato de vapor empurrando para a esquerda (só o dash atravessa)
        WindR,       // jato de vapor empurrando para a direita
    }

    /// <summary>Grade de colisão. Célula (x,y) ocupa [x,x+1] x [y,y+1] no mundo; y=0 é a linha de baixo.</summary>
    public class LevelGrid
    {
        public readonly int W, H;
        readonly Cell[,] cells;
        readonly GameObject[,] visuals;

        public LevelGrid(int w, int h)
        {
            W = w; H = h;
            cells = new Cell[w, h];
            visuals = new GameObject[w, h];
        }

        public bool InBounds(int x, int y) => x >= 0 && x < W && y >= 0 && y < H;

        public Cell Get(int x, int y) => InBounds(x, y) ? cells[x, y] : Cell.Empty;

        public void Set(int x, int y, Cell c, GameObject visual = null)
        {
            if (!InBounds(x, y)) return;
            cells[x, y] = c;
            if (visuals[x, y] != null && visuals[x, y] != visual) Object.Destroy(visuals[x, y]);
            visuals[x, y] = visual;
        }

        public GameObject VisualAt(int x, int y) => InBounds(x, y) ? visuals[x, y] : null;

        public bool IsSolid(int x, int y)
        {
            if (x < 0 || x >= W) return true;        // paredes laterais invisíveis
            if (y < 0 || y >= H) return false;       // abaixo = buraco, acima = céu
            var c = cells[x, y];
            return c == Cell.Solid || c == Cell.Breakable || c == Cell.Cracked || c == Cell.Shutter;
        }

        public bool IsOneWay(int x, int y) => Get(x, y) == Cell.OneWay;

        public static Vector2 CellCenter(int x, int y) => new Vector2(x + 0.5f, y + 0.5f);
    }

    /// <summary>Colisão de caixas (AABB) contra a grade. pos = centro, half = meia-extensão.</summary>
    public static class TileCollision
    {
        const float E = 0.001f;

        public static bool MoveX(LevelGrid g, ref Vector2 p, Vector2 h, float dx)
        {
            if (dx == 0f) return false;
            float nx = p.x + dx;
            int y0 = Mathf.FloorToInt(p.y - h.y + E), y1 = Mathf.FloorToInt(p.y + h.y - E);
            if (dx > 0)
            {
                int cx = Mathf.FloorToInt(nx + h.x - E);
                for (int y = y0; y <= y1; y++)
                    if (g.IsSolid(cx, y)) { p.x = cx - h.x; return true; }
            }
            else
            {
                int cx = Mathf.FloorToInt(nx - h.x + E);
                for (int y = y0; y <= y1; y++)
                    if (g.IsSolid(cx, y)) { p.x = cx + 1 + h.x; return true; }
            }
            p.x = nx;
            return false;
        }

        /// <summary>Move no eixo Y. Retorna true se bateu. 'landed' indica pouso em chão/plataforma.</summary>
        public static bool MoveY(LevelGrid g, ref Vector2 p, Vector2 h, float dy, bool ignoreOneWay, out bool landed)
        {
            landed = false;
            if (dy == 0f) return false;
            float ny = p.y + dy;
            int x0 = Mathf.FloorToInt(p.x - h.x + E), x1 = Mathf.FloorToInt(p.x + h.x - E);
            if (dy < 0)
            {
                float oldBottom = p.y - h.y;
                int cy = Mathf.FloorToInt(ny - h.y);
                for (int x = x0; x <= x1; x++)
                {
                    bool hit = g.IsSolid(x, cy) || (!ignoreOneWay && g.IsOneWay(x, cy) && oldBottom >= cy + 1 - 0.05f);
                    if (hit) { p.y = cy + 1 + h.y; landed = true; return true; }
                }
            }
            else
            {
                int cy = Mathf.FloorToInt(ny + h.y - E);
                for (int x = x0; x <= x1; x++)
                    if (g.IsSolid(x, cy)) { p.y = cy - h.y; return true; }
            }
            p.y = ny;
            return false;
        }

        public static bool Grounded(LevelGrid g, Vector2 p, Vector2 h, bool ignoreOneWay = false)
        {
            float bottom = p.y - h.y;
            int cy = Mathf.FloorToInt(bottom - 0.04f);
            int x0 = Mathf.FloorToInt(p.x - h.x + E), x1 = Mathf.FloorToInt(p.x + h.x - E);
            for (int x = x0; x <= x1; x++)
            {
                if (g.IsSolid(x, cy)) return true;
                if (!ignoreOneWay && g.IsOneWay(x, cy) && bottom >= cy + 1 - 0.06f) return true;
            }
            return false;
        }

        public static bool Overlaps(LevelGrid g, Vector2 p, Vector2 h)
        {
            int x0 = Mathf.FloorToInt(p.x - h.x + E), x1 = Mathf.FloorToInt(p.x + h.x - E);
            int y0 = Mathf.FloorToInt(p.y - h.y + E), y1 = Mathf.FloorToInt(p.y + h.y - E);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (g.IsSolid(x, y)) return true;
            return false;
        }

        public static bool TouchesCell(LevelGrid g, Vector2 p, Vector2 h, Cell type, float shrink = 0.1f)
        {
            int x0 = Mathf.FloorToInt(p.x - h.x + shrink), x1 = Mathf.FloorToInt(p.x + h.x - shrink);
            int y0 = Mathf.FloorToInt(p.y - h.y + 0.02f), y1 = Mathf.FloorToInt(p.y + h.y - shrink);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (g.Get(x, y) == type) return true;
            return false;
        }

        public static bool AabbOverlap(Vector2 a, Vector2 ah, Vector2 b, Vector2 bh) =>
            Mathf.Abs(a.x - b.x) < ah.x + bh.x && Mathf.Abs(a.y - b.y) < ah.y + bh.y;

        public static bool CircleAabb(Vector2 c, float r, Vector2 b, Vector2 bh)
        {
            float dx = Mathf.Max(Mathf.Abs(c.x - b.x) - bh.x, 0f);
            float dy = Mathf.Max(Mathf.Abs(c.y - b.y) - bh.y, 0f);
            return dx * dx + dy * dy < r * r;
        }
    }
}

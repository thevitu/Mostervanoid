using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Mapeamento central de teclas. Mudar aqui altera o jogo inteiro.
    /// Movimento: WASD / setas | Tiro: K / 2 | Especial: L / 3 | Pulo: Espaço / 0
    /// Formas: Enter | Teleporte: M | Cheats: C | Sair: Esc
    /// </summary>
    public static class GameInput
    {
        // entradas simuladas (usadas pelo modo de teste automático -mvtest)
        public static float SimH;
        public static bool SimJumpHeld, SimJumpDown, SimSpecialHeld, SimSpecialDown, SimSpecialUp, SimShootDown;

        static bool Any(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        static bool AnyDown(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        static bool AnyUp(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyUp(k)) return true;
            return false;
        }

        public static bool Left => Any(KeyCode.A, KeyCode.LeftArrow);
        public static bool Right => Any(KeyCode.D, KeyCode.RightArrow);
        public static bool Up => Any(KeyCode.W, KeyCode.UpArrow);
        public static bool Down => Any(KeyCode.S, KeyCode.DownArrow);
        public static float Horizontal => SimH != 0 ? SimH : (Right ? 1f : 0f) - (Left ? 1f : 0f);
        public static float Vertical => (Up ? 1f : 0f) - (Down ? 1f : 0f);

        public static bool UpDown => AnyDown(KeyCode.W, KeyCode.UpArrow);
        public static bool DownDown => AnyDown(KeyCode.S, KeyCode.DownArrow);
        public static bool LeftDown => AnyDown(KeyCode.A, KeyCode.LeftArrow);
        public static bool RightDown => AnyDown(KeyCode.D, KeyCode.RightArrow);

        public static bool JumpDown => SimJumpDown || AnyDown(KeyCode.Space, KeyCode.Alpha0, KeyCode.Keypad0);
        public static bool JumpHeld => SimJumpHeld || Any(KeyCode.Space, KeyCode.Alpha0, KeyCode.Keypad0);

        public static bool ShootDown => SimShootDown || AnyDown(KeyCode.K, KeyCode.Alpha2, KeyCode.Keypad2);

        public static bool SpecialDown => SimSpecialDown || AnyDown(KeyCode.L, KeyCode.Alpha3, KeyCode.Keypad3);
        public static bool SpecialHeld => SimSpecialHeld || Any(KeyCode.L, KeyCode.Alpha3, KeyCode.Keypad3);
        public static bool SpecialUp => SimSpecialUp || AnyUp(KeyCode.L, KeyCode.Alpha3, KeyCode.Keypad3);

        public static bool MenuDown => AnyDown(KeyCode.Return, KeyCode.KeypadEnter);
        public static bool ConfirmDown => AnyDown(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.K, KeyCode.Space);
        public static bool TeleportDown => AnyDown(KeyCode.M);
        public static bool CheatsDown => AnyDown(KeyCode.C);
        public static bool QuitDown => AnyDown(KeyCode.Escape);
    }
}

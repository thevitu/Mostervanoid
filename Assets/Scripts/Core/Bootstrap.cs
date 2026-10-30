using UnityEngine;

namespace Mostervanoid
{
    /// <summary>Garante que o jogo inicie em qualquer cena: cria o GameManager se não existir.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            new GameObject("Mostervanoid").AddComponent<GameManager>();
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mostervanoid
{
    /// <summary>Câmera em perspectiva que segue o jogador (dois eixos), presa aos limites da fase.</summary>
    public class CameraRig : MonoBehaviour
    {
        public const float Distance = 17f;
        public const float Fov = 42f;

        public Camera Cam { get; private set; }
        public Vector2? lockCenter;     // sala do chefe
        public float extraDistance;     // afasta a câmera (chefe gigante numa sala larga)
        float zoom;
        Vector2 look;
        float shake;
        float lookAhead;

        public static CameraRig Create()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = Fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.allowHDR = true;
            go.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            CreatePostFx(go.transform);
            return rig;
        }

        /// <summary>Volume global: bloom (brilho dos projéteis de energia), tonemapping e vinheta leve.</summary>
        static void CreatePostFx(Transform parent)
        {
            var go = new GameObject("PostFx");
            go.transform.SetParent(parent, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.22f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(12f);
            color.contrast.Override(8f);
            vol.sharedProfile = profile;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        public void Snap(Vector2 target, Level lv)
        {
            look = Clamp(target, lv);
            Apply();
        }

        public void Follow(PlayerController p, Level lv, float dt)
        {
            Vector2 target;
            if (lockCenter.HasValue) target = lockCenter.Value;
            else
            {
                lookAhead = Mathf.MoveTowards(lookAhead, p.Facing * 1.5f, dt * 4f);
                target = p.Pos + new Vector2(lookAhead, 1.0f);
            }
            target = Clamp(target, lv);
            float k = 1f - Mathf.Exp(-dt * (lockCenter.HasValue ? 4f : 8f));
            look = Vector2.Lerp(look, target, k);
            Apply();
        }

        void Apply()
        {
            Vector3 s = Vector3.zero;
            if (shake > 0)
            {
                s = (Vector3)(Random.insideUnitCircle * shake * 0.35f);
                shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 1.5f);
            }
            // leve inclinação para baixo, dando a sensação de profundidade do cenário
            zoom = Mathf.MoveTowards(zoom, extraDistance, Time.unscaledDeltaTime * 6f);
            transform.position = new Vector3(look.x, look.y + 1.2f, -(Distance + zoom)) + s;
            transform.rotation = Quaternion.Euler(4f, 0, 0);
        }

        Vector2 Clamp(Vector2 t, Level lv)
        {
            if (lv == null || Cam == null) return t;
            float halfH = (Distance + extraDistance) * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * Cam.aspect;
            float minX = halfW, maxX = lv.grid.W - halfW;
            float minY = halfH - 0.6f, maxY = lv.grid.H - halfH + 1.5f;
            t.x = minX > maxX ? lv.grid.W * 0.5f : Mathf.Clamp(t.x, minX, maxX);
            t.y = minY > maxY ? (minY + maxY) * 0.5f : Mathf.Clamp(t.y, minY, maxY);
            return t;
        }
    }
}

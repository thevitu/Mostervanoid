using UnityEngine;

namespace Mostervanoid
{
    /// <summary>
    /// Modelo 3D do protagonista: uma lata de energético com olhos, braços, pernas e canhão.
    /// O corpo (tubo) recebe a skin da forma atual (StreamingAssets/Skins/formN.png).
    /// Origem do modelo = centro da caixa de colisão em pé (altura 1.6).
    /// </summary>
    public class CanModel : MonoBehaviour
    {
        public const float BodyRadius = 0.36f, BodyHeight = 1.2f;

        Transform rig;        // gira (yaw/roll) e escala (squash)
        Transform legL, legR, armL, armR, eyes;
        Material bodyMat;
        Renderer[] renderers;
        Transform hookLine;
        readonly System.Collections.Generic.List<Transform> pupils = new System.Collections.Generic.List<Transform>();

        float yaw;            // -30 (direita) ... +30 (esquerda)
        float runPhase;
        float spin;

        public static CanModel Create(Transform parent)
        {
            var go = new GameObject("CanModel");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<CanModel>();
            m.Build();
            return m;
        }

        void Build()
        {
            rig = Prim.Pivot(transform, Vector3.zero, "Rig");
            var silver = Mat.Color(new Color(0.82f, 0.84f, 0.88f), 0.85f, 0.9f);
            var dark = Mat.Color(new Color(0.12f, 0.12f, 0.14f), 0.4f);
            var white = Mat.Color(Color.white, 0.6f);

            // corpo (tubo com skin)
            bodyMat = Mat.New(Color.white, 0.75f, 0.35f);
            Prim.MakeMesh(Prim.Tube(), rig, new Vector3(0, 0.1f, 0), new Vector3(BodyRadius * 2, BodyHeight, BodyRadius * 2), bodyMat, default, "Body");
            // tampas e bordas
            Prim.Make(PrimitiveType.Cylinder, rig, new Vector3(0, 0.1f + BodyHeight / 2 + 0.02f, 0), new Vector3(0.66f, 0.03f, 0.66f), silver, default, "Lid");
            Prim.Make(PrimitiveType.Cylinder, rig, new Vector3(0, 0.1f + BodyHeight / 2 + 0.05f, 0), new Vector3(0.70f, 0.025f, 0.70f), silver, default, "Rim");
            Prim.Make(PrimitiveType.Cylinder, rig, new Vector3(0, 0.1f - BodyHeight / 2 - 0.02f, 0), new Vector3(0.62f, 0.04f, 0.62f), silver, default, "Bottom");
            // anel de abrir (pull tab)
            var tab = Prim.Pivot(rig, new Vector3(0, 0.1f + BodyHeight / 2 + 0.085f, 0.05f), "Tab");
            Prim.Make(PrimitiveType.Cube, tab, new Vector3(0, 0, 0), new Vector3(0.16f, 0.02f, 0.26f), silver);
            Prim.Make(PrimitiveType.Cube, tab, new Vector3(0, 0.0f, 0.08f), new Vector3(0.08f, 0.025f, 0.08f), dark);

            // olhos (na frente, voltados para a câmera)
            eyes = Prim.Pivot(rig, new Vector3(0, 0.42f, 0), "Eyes");
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = Prim.Make(PrimitiveType.Sphere, eyes, new Vector3(i * 0.12f, 0, -BodyRadius + 0.02f), new Vector3(0.17f, 0.21f, 0.10f), white, default, "Eye");
                pupils.Add(Prim.Make(PrimitiveType.Sphere, eye.transform, new Vector3(0.18f, 0, -0.35f), new Vector3(0.5f, 0.55f, 0.5f), dark, default, "Pupil").transform);
            }
            // sobrancelhas determinadas
            Prim.Make(PrimitiveType.Cube, eyes, new Vector3(-0.12f, 0.14f, -BodyRadius - 0.01f), new Vector3(0.15f, 0.035f, 0.03f), dark, new Vector3(0, 0, -12));
            Prim.Make(PrimitiveType.Cube, eyes, new Vector3(0.12f, 0.14f, -BodyRadius - 0.01f), new Vector3(0.15f, 0.035f, 0.03f), dark, new Vector3(0, 0, 12));

            // pernas
            legL = Prim.Pivot(rig, new Vector3(-0.14f, -0.5f, 0), "LegL");
            legR = Prim.Pivot(rig, new Vector3(0.14f, -0.5f, 0), "LegR");
            foreach (var leg in new[] { legL, legR })
            {
                Prim.Make(PrimitiveType.Cylinder, leg, new Vector3(0, -0.12f, 0), new Vector3(0.1f, 0.12f, 0.1f), dark);
                Prim.Make(PrimitiveType.Cube, leg, new Vector3(0, -0.26f, -0.04f), new Vector3(0.18f, 0.08f, 0.26f), Mat.Color(new Color(0.85f, 0.15f, 0.1f), 0.5f));
            }

            // braços (o do lado para onde olha vira canhão ao atirar)
            armL = Prim.Pivot(rig, new Vector3(-BodyRadius - 0.04f, 0.15f, 0), "ArmL");
            armR = Prim.Pivot(rig, new Vector3(BodyRadius + 0.04f, 0.15f, 0), "ArmR");
            foreach (var arm in new[] { armL, armR })
            {
                Prim.Make(PrimitiveType.Cylinder, arm, new Vector3(0, -0.15f, 0), new Vector3(0.09f, 0.15f, 0.09f), dark);
                Prim.Make(PrimitiveType.Sphere, arm, new Vector3(0, -0.34f, 0), Vector3.one * 0.17f, Mat.Color(Color.white, 0.5f));
            }

            // linha do gancho
            hookLine = Prim.Make(PrimitiveType.Cylinder, transform, Vector3.zero, new Vector3(0.05f, 0.5f, 0.05f), Mat.Color(new Color(0.3f, 0.3f, 0.3f), 0.5f, 0.8f), default, "HookLine").transform;
            hookLine.gameObject.SetActive(false);

            renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void SetSkin(Texture2D tex) => bodyMat.SetTexture(Mat.BaseMap, tex);

        public void SetVisible(bool v)
        {
            foreach (var r in renderers) if (r != null && r.transform != hookLine) r.enabled = v;
        }

        public struct Pose
        {
            public int facing;
            public float speedX;
            public bool grounded, sliding, shooting, charging, dashing, pounding, hooked, flipping, hurt;
            public float chargeAmount;
            public float vy;
            public Vector3 hookWorld;
        }

        public void Animate(Pose p, float dt)
        {
            // virar (yaw) suavemente — a frente da lata continua visível para a câmera
            float targetYaw = p.facing > 0 ? -35f : 35f;
            yaw = Mathf.MoveTowards(yaw, targetYaw, 600f * dt);

            float roll = 0, pitchSpin = 0;
            Vector3 scale = Vector3.one;
            Vector3 offset = Vector3.zero;

            if (p.sliding)
            {
                roll = p.facing > 0 ? -90f : 90f;          // deita a lata
                offset = new Vector3(-p.facing * 0.1f, 0f, 0);
                runPhase += dt * 20f;
            }
            else if (p.dashing)
            {
                roll = p.facing > 0 ? -35f : 35f;
                scale = new Vector3(0.9f, 1.05f, 0.9f);
            }
            else if (p.charging)
            {
                float k = Mathf.Clamp01(p.chargeAmount);
                scale = new Vector3(1 + 0.25f * k, 1 - 0.3f * k, 1 + 0.25f * k);
                offset.y = -0.24f * k;
                offset.x = Mathf.Sin(Time.time * 60f) * 0.02f * k;
            }
            else if (p.pounding)
            {
                spin += dt * 1400f;
                pitchSpin = spin;
                scale = new Vector3(0.9f, 1.1f, 0.9f);
            }
            else if (p.flipping)
            {
                spin += dt * 900f * -p.facing;
                pitchSpin = spin;
            }
            else
            {
                spin = 0;
                if (!p.grounded)
                    scale = p.vy > 0 ? new Vector3(0.92f, 1.08f, 0.92f) : new Vector3(1.03f, 0.97f, 1.03f);
            }

            foreach (var pu in pupils) pu.localPosition = new Vector3(0.2f * p.facing, p.hooked ? 0.2f : 0f, -0.35f);

            rig.localPosition = offset;
            rig.localRotation = Quaternion.Euler(0, 0, roll + pitchSpin) * Quaternion.Euler(0, yaw, 0);
            rig.localScale = scale;

            // pernas
            float run = p.grounded && Mathf.Abs(p.speedX) > 0.5f && !p.sliding ? 1f : 0f;
            if (run > 0) runPhase += dt * 16f;
            float swing = Mathf.Sin(runPhase) * 40f * run;
            if (!p.grounded && !p.sliding) swing = 25f;
            legL.localRotation = Quaternion.Euler(swing, 0, 0);
            legR.localRotation = Quaternion.Euler(p.grounded ? -swing : -15f, 0, 0);
            if (run > 0) rig.localPosition += new Vector3(0, Mathf.Abs(Mathf.Sin(runPhase)) * 0.05f, 0);

            // braços: o braço "da frente" aponta para frente ao atirar
            var front = p.facing > 0 ? armR : armL;
            var back = p.facing > 0 ? armL : armR;
            float frontZ = p.shooting || p.hooked ? (p.facing > 0 ? 90f : -90f) : Mathf.Sin(runPhase) * 30f * run;
            if (p.hooked) frontZ = 180f;
            front.localRotation = Quaternion.Euler(0, 0, frontZ);
            back.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(runPhase) * 30f * run + (p.grounded ? 0 : (p.facing > 0 ? -40 : 40)));

            // gancho
            hookLine.gameObject.SetActive(p.hooked);
            if (p.hooked)
            {
                Vector3 hand = transform.position + new Vector3(0, 0.8f, 0);
                Vector3 d = p.hookWorld - hand;
                hookLine.position = hand + d * 0.5f;
                hookLine.up = d.normalized;
                hookLine.localScale = new Vector3(0.05f, d.magnitude * 0.5f, 0.05f);
            }
        }
    }
}

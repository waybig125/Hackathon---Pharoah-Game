using UnityEngine;
using UnityEngine.Rendering;

namespace TheAlchemistsCrypt.Player
{
    public class PlayerImmersiveBody : MonoBehaviour
    {
        private GameObject bodyInstance;
        private Animator bodyAnim;
        private CharacterController charController;
        private InfimaGames.LowPolyShooterPack.Character playerCharacter;

        private void Start()
        {
            charController = GetComponent<CharacterController>();
            playerCharacter = GetComponent<InfimaGames.LowPolyShooterPack.Character>();

            // 1. Load Alchemist FBX model from Resources
            GameObject alchemistModel = Resources.Load<GameObject>("Player Character (The Alchemist)/base_basic_shaded");
            if (alchemistModel == null)
            {
                Debug.LogWarning("[PlayerImmersiveBody] Alchemist model not found in Resources!");
                return;
            }

            // 2. Instantiate as child
            bodyInstance = Instantiate(alchemistModel, transform);
            bodyInstance.name = "Alchemist_Shadow_Body";
            bodyInstance.transform.localPosition = new Vector3(0, -1.0f, 0); // Position slightly down at feet
            bodyInstance.transform.localRotation = Quaternion.identity;
            bodyInstance.transform.localScale = Vector3.one * 1.8f; // Match player scale

            // 3. Configure Materials using URP Lit with Alchemist textures
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Texture2D diffTex = Resources.Load<Texture2D>("Player Character (The Alchemist)/texture_diffuse");
            Texture2D normTex = Resources.Load<Texture2D>("Player Character (The Alchemist)/texture_normal");
            Texture2D metTex = Resources.Load<Texture2D>("Player Character (The Alchemist)/texture_metallic");

            Material alMat = new Material(urpShader);
            if (diffTex != null) alMat.SetTexture("_BaseMap", diffTex);
            if (normTex != null)
            {
                alMat.SetTexture("_BumpMap", normTex);
                alMat.EnableKeyword("_NORMALMAP");
            }
            if (metTex != null)
            {
                alMat.SetTexture("_MetallicGlossMap", metTex);
                alMat.EnableKeyword("_METALLICGLOSSMAP");
            }

            // 4. Configure renderers to ShadowsOnly so they cast shadows but are invisible to camera
            Renderer[] renderers = bodyInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                r.receiveShadows = true;
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = alMat;
                r.sharedMaterials = mats;
            }

            // 5. Load and assign the animator controller to animate the shadow
            bodyAnim = bodyInstance.GetComponent<Animator>();
            if (bodyAnim != null)
            {
                // We use the MummyTestController or create a simple runtime override to run locomotions
                RuntimeAnimatorController mController = Resources.Load<RuntimeAnimatorController>("Mummy_Assets/MummyTestController");
                if (mController != null)
                {
                    bodyAnim.runtimeAnimatorController = mController;
                }
            }
        }

        private void Update()
        {
            if (bodyInstance == null) return;

            // Sync shadow rotation and movement animations with actual player physics
            Vector3 vel = charController != null ? charController.velocity : Vector3.zero;
            float speed = new Vector3(vel.x, 0, vel.z).magnitude;

            if (bodyAnim != null)
            {
                bodyAnim.SetFloat("Speed", speed);
                bodyAnim.SetBool("Moving", speed > 0.1f);
            }

            // Orient the shadow body with the player's movement direction
            if (speed > 0.2f)
            {
                Vector3 moveDir = new Vector3(vel.x, 0, vel.z).normalized;
                bodyInstance.transform.rotation = Quaternion.LookRotation(moveDir);
            }
            else
            {
                bodyInstance.transform.localRotation = Quaternion.identity;
            }
        }
    }
}

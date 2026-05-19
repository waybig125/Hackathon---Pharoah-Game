using UnityEngine;
using UnityEditor;
using System.IO;

namespace TheAlchemistsCrypt.Editor
{
    public static class BuildPrepEditor
    {
        [MenuItem("Tools/Generate AI Prefabs")]
        public static void GeneratePrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            GenerateMummyPrefab();
            GeneratePharaohPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildPrepEditor] AI Prefabs successfully generated in Resources.");
        }

        private static void GenerateMummyPrefab()
        {
            string fbxPath = "Assets/Mummy_Assets/mummy_base.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"[BuildPrepEditor] Could not find mummy model at {fbxPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            instance.name = "Mummy_Dynamic_Prefab";
            instance.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

            // 1. Materials
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mummy_Assets/texture/texture_diffuse.png");
            Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mummy_Assets/texture/texture_normal.png");
            
            Material mummyMat = new Material(urpShader);
            if (diffTex != null) mummyMat.SetTexture("_BaseMap", diffTex);
            if (normTex != null)
            {
                mummyMat.SetTexture("_BumpMap", normTex);
                mummyMat.EnableKeyword("_NORMALMAP");
            }

            string matPath = "Assets/Resources/MummyMat.mat";
            AssetDatabase.CreateAsset(mummyMat, matPath);
            mummyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Material[] sharedMats = new Material[r.sharedMaterials.Length];
                for (int j = 0; j < sharedMats.Length; j++)
                {
                    sharedMats[j] = mummyMat;
                }
                r.sharedMaterials = sharedMats;
            }

            // 2. Animator
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Mummy_Assets/MummyTestController.controller");
            var anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            
            Avatar avatar = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in subAssets)
            {
                if (asset is Avatar av)
                {
                    avatar = av;
                    break;
                }
            }
            if (avatar != null) anim.avatar = avatar;

            // 3. Components
            var agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null) agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.8f;
            agent.stoppingDistance = 3.2f;
            agent.height = 2.0f;
            agent.radius = 0.4f;

            var col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f, 0);
            col.height = 2.0f;
            col.radius = 0.4f;

            var rb = instance.GetComponent<Rigidbody>();
            if (rb == null) rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var ai = instance.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
            if (ai == null) ai = instance.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();

            // Save Prefab
            string prefabPath = "Assets/Resources/Mummy_Dynamic_Prefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);
            Debug.Log("[BuildPrepEditor] Mummy Prefab Created.");
        }

        private static void GeneratePharaohPrefab()
        {
            string fbxPath = "Assets/Resources/Pharaoh/base_basic_shaded(3).fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[BuildPrepEditor] Could not find Pharaoh model at {fbxPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            instance.name = "Pharaoh_Prefab";
            instance.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f); // Make the boss bigger

            // 1. Materials
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Pharaoh/texture_diffuse.png");
            Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Pharaoh/texture_normal.png");
            Texture2D metTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Pharaoh/texture_metallic.png");
            
            Material pharaohMat = new Material(urpShader);
            if (diffTex != null) pharaohMat.SetTexture("_BaseMap", diffTex);
            if (normTex != null)
            {
                pharaohMat.SetTexture("_BumpMap", normTex);
                pharaohMat.EnableKeyword("_NORMALMAP");
            }
            if (metTex != null)
            {
                pharaohMat.SetTexture("_MetallicGlossMap", metTex);
                pharaohMat.EnableKeyword("_METALLICGLOSSMAP");
                pharaohMat.SetFloat("_Smoothness", 0.7f);
            }

            string matPath = "Assets/Resources/PharaohMat.mat";
            AssetDatabase.CreateAsset(pharaohMat, matPath);
            pharaohMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Material[] sharedMats = new Material[r.sharedMaterials.Length];
                for (int j = 0; j < sharedMats.Length; j++)
                {
                    sharedMats[j] = pharaohMat;
                }
                r.sharedMaterials = sharedMats;
            }

            // 2. Animator
            // NOTE: We will create the PharaohAnimatorController below and assign it.
            var anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            
            Avatar avatar = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in subAssets)
            {
                if (asset is Avatar av)
                {
                    avatar = av;
                    break;
                }
            }
            if (avatar != null) anim.avatar = avatar;

            // 3. Components
            var agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null) agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 4.5f;
            agent.stoppingDistance = 3.5f;
            agent.height = 2.0f;
            agent.radius = 0.5f;

            var col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f, 0);
            col.height = 2.0f;
            col.radius = 0.5f;

            var rb = instance.GetComponent<Rigidbody>();
            if (rb == null) rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var ai = instance.GetComponent<TheAlchemistsCrypt.AI.PharaohAI>();
            if (ai == null) ai = instance.AddComponent<TheAlchemistsCrypt.AI.PharaohAI>();
            // Pharaoh specific settings could be applied here if using a PharaohAI script.

            // Save Prefab
            string prefabPath = "Assets/Resources/Pharaoh_Prefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);
            Debug.Log("[BuildPrepEditor] Pharaoh Prefab Created.");
        }
    }
}

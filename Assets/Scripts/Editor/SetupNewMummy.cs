using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

public static class SetupNewMummy
{
    [MenuItem("Tools/Setup New Mummy")]
    public static void RunSetup()
    {
        Debug.Log("[MummySetup] Starting setup...");
        
        string assetsPath = "Assets/Mummy_Assets";
        if (!Directory.Exists(assetsPath)) Directory.CreateDirectory(assetsPath);
        
        string resourcePath = "Assets/Resources";
        if (!Directory.Exists(resourcePath)) Directory.CreateDirectory(resourcePath);

        string[] fbxs = {
            "Assets/Mummy/Idle.fbx",
            "Assets/Mummy/Zombie Attack.fbx",
            "Assets/Mummy/Zombie Running.fbx",
            "Assets/Mummy/Falling Back Death.fbx"
        };

        string[] names = { "Mummy_Idle", "Mummy_Attack", "Mummy_Run", "Mummy_Die" };
        bool[] loopable = { true, true, true, false };

        for (int i = 0; i < fbxs.Length; i++) {
            string fbxPath = fbxs[i];
            string animName = names[i];
            bool shouldLoop = loopable[i];

            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip sourceClip = null;
            foreach (var a in assets) {
                if (a is AnimationClip clip && !clip.name.Contains("__preview__")) {
                    sourceClip = clip;
                    break;
                }
            }

            if (sourceClip == null) {
                Debug.LogError($"[MummySetup] No clip in {fbxPath}");
                continue;
            }

            string destPath = assetsPath + "/" + animName + ".anim";
            AnimationClip destClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
            if (destClip == null) {
                destClip = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, destClip);
                AssetDatabase.CreateAsset(destClip, destPath);
            } else {
                EditorUtility.CopySerialized(sourceClip, destClip);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(destClip);
            settings.loopTime = shouldLoop;
            AnimationUtility.SetAnimationClipSettings(destClip, settings);
            EditorUtility.SetDirty(destClip);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[MummySetup] Animations extracted.");

        // 2. Create Animator Controller
        string controllerPath = "Assets/Mummy_Assets/Mummy_Dynamic_Controller.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var rootStateMachine = controller.layers[0].stateMachine;

        string[] stateNames = { "Idle", "Walk", "Attack", "Die" };
        string[] clipPaths = { "Assets/Mummy_Assets/Mummy_Idle.anim", "Assets/Mummy_Assets/Mummy_Run.anim", "Assets/Mummy_Assets/Mummy_Attack.anim", "Assets/Mummy_Assets/Mummy_Die.anim" };

        for (int i = 0; i < 4; i++) {
            var state = rootStateMachine.AddState(stateNames[i]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);
            if (clip != null) state.motion = clip;
        }

        // 3. Build Prefab
        string modelFbxPath = "Assets/Mummy/new_base_basic_shaded.fbx";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelFbxPath);
        if (model == null) {
            Debug.LogError("[MummySetup] Model not found at " + modelFbxPath);
            return;
        }

        GameObject mummyObj = (GameObject)PrefabUtility.InstantiatePrefab(model);
        mummyObj.name = "Mummy_Dynamic_Prefab";
        mummyObj.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

        var agent = mummyObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null) agent = mummyObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 2.0f;
        agent.speed = 3.2f;
        agent.acceleration = 12f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 2.5f;

        var rb = mummyObj.GetComponent<Rigidbody>();
        if (rb == null) rb = mummyObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var col = mummyObj.GetComponent<CapsuleCollider>();
        if (col == null) col = mummyObj.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1, 0);
        col.radius = 0.4f;
        col.height = 2.0f;

        var ai = mummyObj.GetComponent<TheAlchemistsCrypt.AI.ZombieAI>();
        if (ai == null) ai = mummyObj.AddComponent<TheAlchemistsCrypt.AI.ZombieAI>();

        var animator = mummyObj.GetComponent<Animator>();
        if (animator == null) animator = mummyObj.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Apply correct texture/material
        Material mummyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/MummyMat.mat");
        if (mummyMat != null)
        {
            var renderers = mummyObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = mummyMat;
            }
            Debug.Log("[MummySetup] Assigned MummyMat material successfully.");
        }
        else
        {
            Debug.LogWarning("[MummySetup] Could not load Assets/Resources/MummyMat.mat!");
        }

        string prefabPath = resourcePath + "/Mummy_Dynamic_Prefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(mummyObj, prefabPath);

        Object.DestroyImmediate(mummyObj);
        AssetDatabase.SaveAssets();

        Debug.Log("[MummySetup] Prefab successfully created at: " + prefabPath);
    }
}

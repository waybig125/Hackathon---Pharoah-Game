using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace WeirdBrothers 
{
    public class AICreator : EditorWindow
    {
        private GameObject character;
        private bool isHumanoid, isValidAvatar, charExist, isHumanBtn = true, isValidPath = false, IsGenerated;
        private Animator charAnimator;
        private Editor characterPreview;
        private string savePath;
        private RuntimeAnimatorController aiAnimator;

        [MenuItem("WeirdBrothers/AI/AI Creator", false, 0)]
        public static void ShowAIGenerator()
        {
            GetWindow<AICreator>("AI Creator");
        }

        private void OnGUI()
        {
            #region Title
            this.titleContent = new GUIContent("AICreator", null, "AI Creator");
            #endregion

            GUILayout.BeginVertical("AI Creator", "window");

            GUILayout.Space(5);

            GUILayout.BeginVertical("box");

            #region Character FBX Field

            if (!character)
                EditorGUILayout.HelpBox("Drag and drop FBX model here!", MessageType.Info);
            else if (!charExist)
                EditorGUILayout.HelpBox("Missing a Animator Component", MessageType.Error);
            else if (isHumanBtn) 
            {
                if (!isHumanoid)
                    EditorGUILayout.HelpBox(character.name + " is a invalid Humanoid", MessageType.Error);
            }            
            else if (!isValidAvatar)
                EditorGUILayout.HelpBox(character.name + " has invalid Avatar", MessageType.Info);

            character = EditorGUILayout.ObjectField("AI Model", character, typeof(GameObject), true, GUILayout.ExpandWidth(true)) as GameObject;
            
            if (GUI.changed)
            {                
                IsGenerated = false;
                characterPreview = null;
                DisplayModel();
            }

            if (character)
            {
                charAnimator = character.GetComponent<Animator>();                
                charExist = charAnimator != null;
                if (charExist)
                {
                    isHumanoid = charExist ? charAnimator.isHuman : false;
                    if (charAnimator.avatar)
                    {
                        isValidAvatar = charExist ? charAnimator.avatar.isValid : false;
                    }
                    else isValidAvatar = false;
                }
            }

            #endregion

            #region AI Animator

            aiAnimator = EditorGUILayout.ObjectField("AI Animator", aiAnimator, typeof(RuntimeAnimatorController), true, GUILayout.ExpandWidth(true)) as RuntimeAnimatorController;

            #endregion

            #region Is Humanoid CheckBox

            //isHumanBtn = EditorGUILayout.Toggle("Is Humanoid", isHumanBtn);

            //if (GUI.changed)
            //{
            //    IsGenerated = false;
            //}

            #endregion

            #region Save Path

            GUILayout.Space(5);
            GUILayout.BeginHorizontal("box");            
            GUILayout.Label(savePath,EditorStyles.textField);
            ShowSavePathButton();            
            GUILayout.EndHorizontal();

            #endregion

            GUILayout.EndVertical();

            if (!isValidPath)
            {                
                EditorGUILayout.HelpBox("Invalid save path", MessageType.Error);
            }
            if (IsGenerated)
            {
                EditorGUILayout.HelpBox("AI successfully generated", MessageType.Info);
            }

            #region Generate AI Button

            if (character != null && charExist && isValidAvatar && isValidPath)
            {
                if (isHumanBtn)
                {
                    if (isHumanoid)
                    {
                        ShowGenerateButton();
                    }
                }
                else 
                {
                    ShowGenerateButton();
                }                
            }

            //if (IsGenerated)
            //{
            //    EditorGUILayout.HelpBox("AI successfully generated", MessageType.Info);
            //}

            #endregion

            #region Character Displayer

            GUILayout.Space(10);

            DisplayModel();

            #endregion

            GUILayout.EndVertical();
        }

        private void DisplayModel()
        {
            if (character != null && charExist && isValidAvatar)
            {
                if (isHumanBtn)
                {
                    if (isHumanoid)
                    {
                        ShowCharacter();
                    }
                }
            }
            else
            {
                DestroyImmediate(characterPreview);
            }
        }

        private void ShowCharacter() 
        {
            if (characterPreview == null)
                characterPreview = Editor.CreateEditor(character);
            characterPreview.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200, 200), GUIStyle.none);
        }

        private void ShowGenerateButton() 
        {            
            if (GUILayout.Button("Generate AI"))
            {
                GameObject characterPrefab = Instantiate(character);

                //adding collider to character
                if (characterPrefab.GetComponent<CapsuleCollider>() != null)
                {
                    Destroy(characterPrefab.GetComponent<CapsuleCollider>());
                }

                var collider = characterPrefab.AddComponent<CapsuleCollider>();
                var height = ColliderHeight(characterPrefab.GetComponent<Animator>());
                var radius = (float)System.Math.Round(collider.height * 0.15f, 2);
                collider.height = height;
                collider.center = new Vector3(0, (float)System.Math.Round(collider.height * 0.5f, 2), 0);
                collider.radius = radius;

                //adding rigidbody
                if (characterPrefab.GetComponent<Rigidbody>() != null)
                {
                    Destroy(characterPrefab.GetComponent<Rigidbody>());
                }

                var rb = characterPrefab.AddComponent<Rigidbody>();
                rb.freezeRotation = true;

                //adding navmesh agent 
                if (characterPrefab.GetComponent<NavMeshAgent>() != null)
                {
                    Destroy(characterPrefab.GetComponent<NavMeshAgent>());
                }

                var agent = characterPrefab.AddComponent<NavMeshAgent>();
                agent.height = height - 0.1f;
                agent.radius = radius;

                //adding script to character
                if (characterPrefab.GetComponent<BaseStateMachine>() != null)
                {
                    Destroy(characterPrefab.GetComponent<BaseStateMachine>());
                }
                if (characterPrefab.GetComponent<Health>() != null)
                {
                    Destroy(characterPrefab.GetComponent<Health>());
                }

                characterPrefab.AddComponent<BaseStateMachine>();
                characterPrefab.AddComponent<Health>();
                characterPrefab.GetComponent<Animator>().applyRootMotion = false;
                characterPrefab.tag = "Enemy";
                characterPrefab.layer = LayerMask.NameToLayer("Enemy");
                foreach (Transform transform in characterPrefab.transform)
                {
                    transform.gameObject.layer = characterPrefab.layer;
                }

                //add runtime animation controller
                if (aiAnimator) 
                {
                    characterPrefab.GetComponent<Animator>().runtimeAnimatorController = aiAnimator;
                }              

                //saving character in desire path
                string localPath = savePath + "/" + character.name + ".prefab";                
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
                PrefabUtility.SaveAsPrefabAssetAndConnect(characterPrefab, localPath, InteractionMode.UserAction);              

                IsGenerated = true;
                DestroyImmediate(characterPrefab);                            
            }            
        }

        private void ShowSavePathButton() 
        {
            if (GUILayout.Button("Path")) 
            {
                IsGenerated = false;

                string AssetsPath = Application.dataPath;
                savePath = EditorUtility.OpenFolderPanel("save path", AssetsPath ,"");


                if (string.IsNullOrEmpty(savePath))
                {
                    isValidPath = false;
                }
                else
                {
                    isValidPath = true;
                }
            }
        }

        private float ColliderHeight(Animator anim)
        {
            if (anim) 
            {
                var foot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                return (float)System.Math.Round(Vector3.Distance(foot.position, hips.position) * 2f, 2);
            }
            return 0;
        }
    }
}


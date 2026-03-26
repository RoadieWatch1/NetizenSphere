// AvatarSetupHelper.cs
// NetizenSphere — Wires DefaultMale into Player.prefab automatically on import.
// Also available manually: NetizenSphere > Setup Avatar (for any humanoid model).

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NetizenSphere.Editor
{
    // ──────────────────────────────────────────────────────────────────────────
    // Auto-setup: fires when Ch20_nonPBR.fbx or DefaultMale.fbx is first imported
    // ──────────────────────────────────────────────────────────────────────────
    public class AvatarAutoSetup : AssetPostprocessor
    {
        private const string ModelPath  = "Assets/Characters/Ch20_nonPBR.fbx";
        private const string PrefabPath = "Assets/Resources/Player.prefab";

        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets,    string[] movedFromPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path == ModelPath)
                {
                    // Delay one frame so the Avatar is fully registered
                    EditorApplication.delayCall += AutoWirePlayer;
                    break;
                }
            }
        }

        static void AutoWirePlayer()
        {
            AvatarSetupHelper.WirePlayerPrefab(ModelPath, PrefabPath, silent: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Manual setup: NetizenSphere > Setup Avatar
    // ──────────────────────────────────────────────────────────────────────────
    public static class AvatarSetupHelper
    {
        private const string PrefabPath = "Assets/Resources/Player.prefab";

        [MenuItem("NetizenSphere/Setup Avatar")]
        private static void SetupAvatar()
        {
            string modelPath = EditorUtility.OpenFilePanelWithFilters(
                "Select Humanoid Character Prefab or FBX",
                "Assets",
                new[] { "Prefab/FBX", "prefab,fbx", "All files", "*" });

            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.Log("[AvatarSetup] Cancelled.");
                return;
            }

            if (modelPath.StartsWith(Application.dataPath))
                modelPath = "Assets" + modelPath.Substring(Application.dataPath.Length);

            bool ok = WirePlayerPrefab(modelPath, PrefabPath, silent: false);
            if (ok)
                EditorUtility.DisplayDialog("Setup Avatar",
                    "Done!\n\nCharacter placed under AvatarVisual.\n" +
                    "Animator Avatar + Controller wired.\n\n" +
                    "Press Play to verify.", "OK");
        }

        [MenuItem("NetizenSphere/Setup Avatar", validate = true)]
        private static bool SetupAvatarValidate() =>
            System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath, "../" + PrefabPath));

        // ── Core wiring logic ─────────────────────────────────────────────────
        public static bool WirePlayerPrefab(string modelPath, string prefabPath, bool silent)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Setup Avatar",
                        $"Model not found at:\n{modelPath}", "OK");
                else
                    Debug.LogWarning($"[AvatarSetup] Model not found at {modelPath}");
                return false;
            }

            // Verify humanoid avatar
            Animator srcAnimator = modelAsset.GetComponent<Animator>()
                                ?? modelAsset.GetComponentInChildren<Animator>(true);
            if (srcAnimator == null || srcAnimator.avatar == null || !srcAnimator.avatar.isHuman)
            {
                string msg = "Model does not have a Humanoid avatar.\n\n" +
                             "Inspector → Rig → Animation Type: Humanoid → Apply";
                if (!silent) EditorUtility.DisplayDialog("Setup Avatar", msg, "OK");
                else Debug.LogWarning("[AvatarSetup] " + msg);
                return false;
            }

            using var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var prefabRoot = editScope.prefabContentsRoot;

            Transform avatarVisual = prefabRoot.transform.Find("AvatarVisual");
            if (avatarVisual == null)
            {
                Debug.LogError("[AvatarSetup] AvatarVisual not found in Player.prefab.");
                return false;
            }

            // Remove any previous model child
            for (int i = avatarVisual.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(avatarVisual.GetChild(i).gameObject);

            // Place model under AvatarVisual
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, avatarVisual);
            modelInstance.name = modelAsset.name;
            modelInstance.transform.localPosition = new Vector3(0f, -1f, 0f);
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale    = Vector3.one;

            // Wire Animator on AvatarVisual
            var animator = avatarVisual.GetComponent<Animator>();
            if (animator == null)
                animator = avatarVisual.gameObject.AddComponent<Animator>();

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/PlayerAnimatorController.controller");
            if (controller != null)
                animator.runtimeAnimatorController = controller;
            else
                Debug.LogWarning("[AvatarSetup] PlayerAnimatorController not found — assign manually.");

            animator.avatar        = srcAnimator.avatar;
            animator.applyRootMotion = false;

            // Remove redundant Animator from the model instance itself
            if (modelInstance.TryGetComponent<Animator>(out var modelAnim))
                Object.DestroyImmediate(modelAnim);

            // Wire PlayerAvatar serialized fields so Awake() never has to auto-discover.
            var playerAvatar = avatarVisual.GetComponent<NetizenSphere.Player.PlayerAvatar>();
            if (playerAvatar != null)
            {
                var so = new SerializedObject(playerAvatar);
                // _animator → the Animator on AvatarVisual (same GO)
                so.FindProperty("_animator").objectReferenceValue = animator;
                // _animatorController → assigned only when the asset was found
                if (controller != null)
                    so.FindProperty("_animatorController").objectReferenceValue = controller;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"[AvatarSetup] Done — model: {modelAsset.name} | " +
                $"controller: {animator.runtimeAnimatorController?.name ?? "NOT FOUND"} | " +
                $"avatar: {animator.avatar?.name ?? "NOT FOUND"}");
            return true;
        }
    }
}
#endif

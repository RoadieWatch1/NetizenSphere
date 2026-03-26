using UnityEditor;
using UnityEditor.SceneManagement;

namespace NetizenSphere.Editor
{
    /// <summary>
    /// Automatically opens Boot.unity in the Scene view when the Unity Editor loads,
    /// so the hub world is always visible and ready to edit.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneAutoOpen
    {
        private const string BootScenePath = "Assets/Boot.unity";
        private const string SessionKey = "NetizenSphere.SceneAutoOpen.Opened";

        static SceneAutoOpen()
        {
            // Only open once per editor session, not every domain reload
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);

            // Delay until the editor is fully initialized
            EditorApplication.delayCall += OpenHubScene;
        }

        private static void OpenHubScene()
        {
            var currentScene = EditorSceneManager.GetActiveScene();

            // Already on Boot — nothing to do
            if (currentScene.path == BootScenePath)
                return;

            // Only switch if the current scene has no unsaved changes
            if (currentScene.isDirty)
            {
                UnityEngine.Debug.Log("[NetizenSphere] Current scene has unsaved changes — skipping auto-open of Boot.unity. Use NetizenSphere > Open Hub Scene to switch manually.");
                return;
            }

            EditorSceneManager.OpenScene(BootScenePath);
            UnityEngine.Debug.Log("[NetizenSphere] Opened Boot.unity (hub world) in Scene view.");
        }

        [MenuItem("NetizenSphere/Open Hub Scene")]
        private static void OpenHubSceneMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(BootScenePath);
        }
    }
}

using Architecture;
using UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Template.Editor.UI
{
    public sealed class UIRootBuildValidator : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
            {
                return;
            }

            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                foreach (var scope in sceneRoot.GetComponentsInChildren<ProjectLifetimeScope>(true))
                {
                    var serializedScope = new SerializedObject(scope);
                    var root = serializedScope.FindProperty("uiRoot").objectReferenceValue as UIRoot;
                    if (root == null)
                    {
                        throw new BuildFailedException($"[UIRoot] No UI root assigned in '{scene.path}'.");
                    }

                    var document = root.GetComponent<UIDocument>();
                    if (document == null || document.panelSettings == null)
                    {
                        throw new BuildFailedException($"[UIRoot] Missing UIDocument or Panel Settings in '{scene.path}'.");
                    }

                    RequireTemplate<LanguagePage>(root, scene.path);
                    RequireTemplate<MainScenePage>(root, scene.path);
                    RequireTemplate<SettingsPage>(root, scene.path);
                }
            }
        }

        private static void RequireTemplate<TPage>(UIRoot root, string scenePath) where TPage : BasePage
        {
            if (root.GetTemplate(typeof(TPage)) == null)
            {
                throw new BuildFailedException($"[UIRoot] Missing {typeof(TPage).Name} UXML in '{scenePath}'.");
            }
        }
    }
}

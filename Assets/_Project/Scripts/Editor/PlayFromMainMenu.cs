using UnityEditor;
using UnityEditor.SceneManagement;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Faz o botão Play do Editor sempre começar pela tela inicial (MainMenu),
    /// mesmo que a cena aberta seja a Game. Desligue pelo menu
    /// "Guardiao do Farol/Play Começa no Menu" para testar a cena aberta direto.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string MenuItemPath = "Guardiao do Farol/Play Começa no Menu";
        private const string PrefKey = "GuardiaoDoFarol.PlayFromMainMenu";

        static PlayFromMainMenu()
        {
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuItemPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuItemPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuItemPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath)
                : null;
        }
    }
}

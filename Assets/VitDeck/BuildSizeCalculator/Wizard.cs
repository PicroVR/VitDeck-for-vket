using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using VitDeck.Utilities;
using VitDeck.Language;
using VitDeck.Main;

namespace VitDeck.BuildSizeCalculator
{
    /// <summary>
    /// ビルドサイズ計算のGUI
    /// </summary>
    public class Wizard : ScriptableWizard
    {
        private const string prefix = "VitDeck/";

        [SerializeField]
        private DefaultAsset baseFolder;

#if !VITDECK_HIDE_MENUITEM
        [MenuItem(prefix + "Calculate Build Size", priority = 102)]
#endif
        public static void Open()
        {
            DisplayWizard<Wizard>("VitDeck", "Build and Calculate").LoadSettings();
        }

        protected override bool DrawWizardGUI()
        {
            base.DrawWizardGUI();
            this.isValid = baseFolder;
            return true;
        }

        /// <summary>
        /// VitDeckのユーザー設定を読み込む。
        /// </summary>
        private void LoadSettings()
        {
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            this.baseFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(userSettings.validatorFolderPath);
        }

        /// <summary>
        /// VitDeckのユーザー設定を保存する。
        /// </summary>
        private void SaveSettings()
        {
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            userSettings.validatorFolderPath = AssetDatabase.GetAssetPath(this.baseFolder);
            SettingUtility.SaveSettings(userSettings);
        }

        private void OnWizardCreate()
        {
            this.SaveSettings();
            GUIUtilities.OpenPackageScene(AssetUtility.GetId(this.baseFolder));
            UnityEditorUtility.StartCoroutine(this.Calculate());
        }

        private IEnumerator Calculate()
        {
            var bakeCheck = GUIUtilities.BakeCheckAndRun();
            while (bakeCheck.MoveNext())
            {
                yield return null;
            }

            if (!(bool)bakeCheck.Current)
            {
                yield break;
            }

            float? byteCount = null;
            AssetUtility.TemporaryDestroyObjectsOutsideOfRootObjectAndRunCallback(AssetUtility.GetId(this.baseFolder), () => {
                byteCount = Calculator.ForceRebuild();
            });

            if (byteCount == null)
            {
                EditorUtility.DisplayDialog("VitDeck", LocalizedMessage.Get("BuildSizeCalculator.BuildFailed"), "OK");
                yield break;
            }

            EditorUtility.DisplayDialog(
                "VitDeck",
                LocalizedMessage.Get("BuildSizeCalculator.BuildSize", AssetUtility.GetScenePath(this.baseFolder), MathUtility.FormatByteCount((int)byteCount)),
                "OK"
            );
        }
    }
}

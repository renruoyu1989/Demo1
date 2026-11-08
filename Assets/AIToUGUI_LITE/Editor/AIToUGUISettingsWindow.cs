#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIToUGUI.Lite
{
    public class AIToUGUISettingsWindow : EditorWindow
    {
        private const string PrefKey_UseAutoWorkspaceFolder = "AIToUGUI_UseAutoWorkspaceFolder";
        private const string PrefKey_CustomPrefabExportPath = "AIToUGUI_CustomPrefabExportPath";
        private const string DefaultRelativePath = "Assets/Logic";

        public static bool UseAutoWorkspaceFolder
        {
            get => EditorPrefs.GetBool(PrefKey_UseAutoWorkspaceFolder, true);
            set => EditorPrefs.SetBool(PrefKey_UseAutoWorkspaceFolder, value);
        }

        public static string CustomPrefabExportPath
        {
            get => EditorPrefs.GetString(PrefKey_CustomPrefabExportPath, DefaultRelativePath);
            set => EditorPrefs.SetString(PrefKey_CustomPrefabExportPath, FormatRelativePath(value));
        }

        public static string GetEffectivePrefabOutputDir(string workspaceName)
        {
            if (UseAutoWorkspaceFolder)
            {
                if (!string.IsNullOrEmpty(workspaceName))
                {
                    return $"Assets/Logic/{workspaceName}";
                }
                return DefaultRelativePath;
            }

            string custom = CustomPrefabExportPath;
            if (string.IsNullOrWhiteSpace(custom))
            {
                return DefaultRelativePath;
            }
            return FormatRelativePath(custom);
        }

        private static string FormatRelativePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return DefaultRelativePath;

            string normalized = rawPath.Replace('\\', '/').Trim();
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Assets/" + normalized.TrimStart('/');
            }
            return normalized.TrimEnd('/');
        }

        [MenuItem("Tools/AIToUGUI/Settings...", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<AIToUGUISettingsWindow>("AIToUGUI Settings");
            window.minSize = new Vector2(480, 220);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AIToUGUI Prefab Export Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            bool useAuto = EditorGUILayout.ToggleLeft("Auto-infer output folder from workspace name (Assets/Logic/{workspace})", UseAutoWorkspaceFolder);
            if (useAuto != UseAutoWorkspaceFolder)
            {
                UseAutoWorkspaceFolder = useAuto;
            }

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(UseAutoWorkspaceFolder))
            {
                EditorGUILayout.LabelField("Custom Prefab Export Path (Relative to Assets):", EditorStyles.label);

                EditorGUILayout.BeginHorizontal();
                string customPath = EditorGUILayout.TextField(CustomPrefabExportPath);
                if (customPath != CustomPrefabExportPath)
                {
                    CustomPrefabExportPath = customPath;
                }

                if (GUILayout.Button("Browse...", GUILayout.Width(80)))
                {
                    string absPath = EditorUtility.OpenFolderPanel("Select Prefab Export Folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(absPath))
                    {
                        if (absPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string rel = "Assets" + absPath.Substring(Application.dataPath.Length);
                            CustomPrefabExportPath = rel;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Folder", "Selected folder must be inside the Unity project Assets directory.", "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.HelpBox(
                $"Effective Output Directory: {GetEffectivePrefabOutputDir("example_workspace")}",
                MessageType.Info);

            EditorGUILayout.Space(15);
            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                Close();
            }
        }
    }
}

#endif

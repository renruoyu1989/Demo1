#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AIToUGUI.Lite
{
    public static class LiteBatchImporter
    {
        private const string WorkspaceRoot = "workspaces";
        private const string AlternativeWorkspaceRoot = ".codebuddy/workspaces";
        private const string DefaultPrefabOutputDir = "Assets/Prefabs";

        /// <summary>
        /// Non-interactive entry point — returns result string, no dialogs.
        /// Prefabs are saved to <c>Assets/Prefabs/</c>.
        /// Use the overload <see cref="ImportFromPath(string,string)"/> to specify a custom output directory.
        /// </summary>
        public static string ImportFromPath(string jsonAbsolutePath)
        {
            return ImportFromPath(jsonAbsolutePath, DefaultPrefabOutputDir);
        }

        /// <summary>
        /// Non-interactive entry point with custom output directory.
        /// </summary>
        /// <param name="jsonAbsolutePath">Absolute path to compiled_site_bundle.json</param>
        /// <param name="prefabOutputDir">Unity project-relative prefab output directory, e.g. "Assets/Logic/M525"</param>
        public static string ImportFromPath(string jsonAbsolutePath, string prefabOutputDir)
        {
            if (string.IsNullOrEmpty(jsonAbsolutePath))
                return "ERROR: jsonAbsolutePath is null or empty";

            if (!File.Exists(jsonAbsolutePath))
                return $"ERROR: File not found: {jsonAbsolutePath}";

            if (string.IsNullOrEmpty(prefabOutputDir))
                prefabOutputDir = DefaultPrefabOutputDir;

            try
            {
                string jsonContent = File.ReadAllText(jsonAbsolutePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                    return $"ERROR: JSON file is empty: {jsonAbsolutePath}";

                var textAsset = new TextAsset(jsonContent);
                textAsset.name = Path.GetFileNameWithoutExtension(jsonAbsolutePath);

                var parsed = AIToUGUILiteBundleParser.Parse(textAsset);
                if (!parsed.IsValid)
                    return $"ERROR: Parse failed — {string.Join(" | ", parsed.Warnings)}";

                // Ensure scene saved
                var scene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path))
                {
                    string sceneDir = Path.Combine(Application.dataPath, "Scenes");
                    if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);
                    string scenePath = $"Assets/Scenes/BatchImport_Scene.unity";
                    EditorSceneManager.SaveScene(scene, scenePath);
                }

                // Create Canvas + Mount
                var mountGo = FindOrCreateMount();
                if (mountGo == null) return "ERROR: Failed to create preview mount";

                // Build
                var options = new AIToUGUILitePreviewBuildOptions();
                var result = AIToUGUILitePreviewBuilder.BuildAll(parsed, mountGo, options);

                // Resolve prefab names from site.json pages (prefabName)
                var pagePrefabNames = new System.Collections.Generic.Dictionary<string, string>();
                if (parsed.Bundle != null && parsed.Bundle.pages != null)
                {
                    foreach (var p in parsed.Bundle.pages)
                    {
                        if (!string.IsNullOrEmpty(p.pageId) && !string.IsNullOrEmpty(p.prefabName))
                            pagePrefabNames[p.pageId] = p.prefabName;
                    }
                }

                // Save prefabs
                EnsureDirectory(prefabOutputDir);
                int prefabCount = 0;
                var prefabPaths = new System.Collections.Generic.List<string>();

                foreach (var root in result.builtRoots)
                {
                    if (root == null) continue;
                    // Use prefabName from site.json if available, otherwise fall back to GameObject name
                    string prefabName;
                    if (pagePrefabNames.TryGetValue(root.name, out string configuredName))
                        prefabName = SanitizePrefabName(configuredName);
                    else
                        prefabName = SanitizePrefabName(root.name);

                    string prefabPath = $"{prefabOutputDir}/{prefabName}.prefab";
                    if (SaveAsPrefab(root, prefabPath))
                    {
                        prefabCount++;
                        prefabPaths.Add(prefabPath);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.Refresh();

                return $"OK: {prefabCount} prefabs saved. Paths: {string.Join(", ", prefabPaths)} | " +
                       $"Built: {result.builtPageCount} | Cleared: {result.clearedCount} | Warnings: {result.warnings.Count}";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.GetType().Name}: {ex.Message}";
            }
        }

        /// <summary>
        /// Import the most recently modified compiled_site_bundle.json found under workspaces/ or .codebuddy/workspaces/.
        /// Output directory is inferred from the workspace directory name (e.g. "m525_ui" → "Assets/Logic/m525_ui").
        /// Returns result string. MCP-friendly — no parameters, no dialogs.
        /// </summary>
        public static string ImportLatest()
        {
            string bundlePath = FindLatestBundleJson();
            if (string.IsNullOrEmpty(bundlePath))
                return "ERROR: No compiled_site_bundle.json found in workspace directories";

            string wsName = null;
            try
            {
                string bundleDir = Path.GetDirectoryName(bundlePath);
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string wsParent = Path.Combine(projectRoot, WorkspaceRoot);
                string current = bundleDir;
                while (current != null && current.Length > wsParent.Length)
                {
                    string parent = Path.GetDirectoryName(current);
                    if (parent != null && (parent.Equals(wsParent, StringComparison.OrdinalIgnoreCase) ||
                        parent.Equals(Path.Combine(projectRoot, AlternativeWorkspaceRoot), StringComparison.OrdinalIgnoreCase)))
                    {
                        string foundName = Path.GetFileName(current).TrimEnd(Path.DirectorySeparatorChar);
                        if (!string.IsNullOrEmpty(foundName) && foundName != ".")
                            wsName = foundName;
                        break;
                    }
                    current = parent;
                }
            }
            catch { }

            string outputDir = AIToUGUISettingsWindow.GetEffectivePrefabOutputDir(wsName);
            return ImportFromPath(bundlePath, outputDir);
        }

        // ── Menu Items ───────────────────────────────────────

        [MenuItem("Tools/AIToUGUI/Import from File...", false, 10)]
        public static void Menu_ImportFromFile()
        {
            string jsonPath = EditorUtility.OpenFilePanel(
                "Select compiled_site_bundle.json",
                Path.Combine(Application.dataPath, "..", WorkspaceRoot), "json");
            if (string.IsNullOrEmpty(jsonPath)) return;
            string result = ImportFromPath(jsonPath);
            EditorUtility.DisplayDialog("Import Result", result, "OK");
        }

        // ── Helpers ────────────────────────────────────────────

        private static string FindLatestBundleJson()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            var candidates = new System.Collections.Generic.List<string>();

            foreach (string searchDir in new[] {
                Path.Combine(projectRoot, WorkspaceRoot),
                Path.Combine(projectRoot, AlternativeWorkspaceRoot)
            })
            {
                if (!Directory.Exists(searchDir)) continue;
                candidates.AddRange(Directory.GetFiles(searchDir, "compiled_site_bundle.json", SearchOption.AllDirectories));
            }

            if (candidates.Count == 0) return null;
            return candidates.OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc).First();
        }

        private static GameObject FindOrCreateMount()
        {
            const string mountName = "AIToUGUI_Mount";
            var existing = GameObject.Find(mountName);
            if (existing != null)
                return existing;

            var canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }

            var mountGo = new GameObject(mountName, typeof(RectTransform));
            mountGo.transform.SetParent(canvas.transform, false);
            var mountRt = mountGo.GetComponent<RectTransform>();
            mountRt.anchorMin = Vector2.zero;
            mountRt.anchorMax = Vector2.one;
            mountRt.sizeDelta = Vector2.zero;
            return mountGo;
        }

        private static bool SaveAsPrefab(GameObject root, string prefabPath)
        {
            try
            {
                var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existingPrefab != null)
                    PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
                else
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out _);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LiteImport] Prefab save error: {ex.Message}");
                return false;
            }
        }

        private static string SanitizePrefabName(string name)
        {
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", "_");
            return string.IsNullOrWhiteSpace(sanitized) ? "GeneratedPage" : sanitized;
        }

        private static void EnsureDirectory(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
        }
    }
}

#endif

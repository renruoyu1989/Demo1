#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AIToUGUI.Lite
{
    public sealed class AIToUGUILiteBuildResult
    {
        public int clearedCount;
        public int builtPageCount;
        public readonly List<GameObject> builtRoots = new List<GameObject>();
        public readonly List<string> warnings = new List<string>();
    }

    public static class AIToUGUILitePreviewBuilder
    {
        private const int UnlimitedVisibleLines = 9999;
        private const string InternalLabelName = "__lite_Label";

        private static readonly Regex NumberRegex = new Regex(@"-?\d+(?:\.\d+)?", RegexOptions.Compiled);
        private static readonly Regex RgbaRegex = new Regex(@"rgba?\((?<r>[\d\.]+)\s*,\s*(?<g>[\d\.]+)\s*,\s*(?<b>[\d\.]+)(?:\s*,\s*(?<a>[\d\.]+))?\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VarRegex = new Regex(@"var\((?<token>--[\w\-]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ColorRegex = new Regex(@"#(?:[0-9a-fA-F]{3,8})|rgba?\([^)]+\)|transparent", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Vector2 DefaultElementSize = new Vector2(160f, 48f);
        private static readonly Vector2 DefaultToggleSize = new Vector2(180f, 24f);
        private static readonly Vector2 DefaultPanelSize = new Vector2(420f, 240f);

        private sealed class BuiltLiteNode
        {
            public LiteCompiledNodeDto node;
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Transform childHost;
            public readonly List<BuiltLiteNode> children = new List<BuiltLiteNode>();
        }

        private enum LiteLengthUnit
        {
            None,
            Pixel,
            Percent,
            Auto
        }

        private readonly struct LiteCssLength
        {
            public LiteCssLength(float value, LiteLengthUnit unit)
            {
                Value = value;
                Unit = unit;
            }

            public float Value { get; }
            public LiteLengthUnit Unit { get; }
            public bool IsValid => Unit != LiteLengthUnit.None;
            public bool IsAuto => Unit == LiteLengthUnit.Auto;
        }

        private readonly struct LiteCssEdges
        {
            public LiteCssEdges(LiteCssLength left, LiteCssLength right, LiteCssLength top, LiteCssLength bottom)
            {
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }

            public LiteCssLength Left { get; }
            public LiteCssLength Right { get; }
            public LiteCssLength Top { get; }
            public LiteCssLength Bottom { get; }
        }

        public static AIToUGUILiteBuildResult BuildAll(
            AIToUGUILiteParsedBundle parsedBundle,
            GameObject mountRoot,
            AIToUGUILitePreviewBuildOptions options = null)
        {
            var result = new AIToUGUILiteBuildResult();
            if (mountRoot == null)
            {
                result.warnings.Add("Preview mount is missing.");
                return result;
            }

            if (parsedBundle == null || !parsedBundle.IsValid)
            {
                result.warnings.Add("Parsed bundle is invalid.");
                return result;
            }

            result.warnings.AddRange(parsedBundle.Warnings);
            ResetSpatialRecords();
            result.clearedCount = Clear(mountRoot.transform);

            var pages = parsedBundle.Bundle.pages ?? Array.Empty<LiteCompiledPageDto>();
            for (var i = 0; i < pages.Length; i++)
            {
                var root = BuildPage(parsedBundle, pages[i], mountRoot.transform, options);
                if (root == null)
                {
                    continue;
                }

                result.builtRoots.Add(root);
                result.builtPageCount++;
            }

            MarkSceneDirty(mountRoot);
            return result;
        }

        public static GameObject BuildPage(
            AIToUGUILiteParsedBundle parsedBundle,
            LiteCompiledPageDto page,
            Transform mountRoot,
            AIToUGUILitePreviewBuildOptions options = null)
        {
            if (parsedBundle == null || !parsedBundle.IsValid || page == null || page.root == null || mountRoot == null)
            {
                return null;
            }

            var parent = mountRoot as RectTransform;
            if (parent == null)
            {
                return null;
            }

            var tokenMap = BuildTokenMap(parsedBundle.Bundle.theme);
            var designSize = new Vector2(
                Mathf.Max(1f, parsedBundle.Bundle.site.designWidth),
                Mathf.Max(1f, parsedBundle.Bundle.site.designHeight));

            var root = BuildPageGameObject(
                page,
                parent,
                designSize,
                parsedBundle.Bundle.theme,
                tokenMap,
                parsedBundle.Bundle.site.siteId ?? string.Empty,
                options);

            if (root == null)
            {
                return null;
            }

            root.name = ResolvePageName(page);
            return root;
        }

        public static int Clear(Transform mountRoot)
        {
            if (mountRoot == null)
                return 0;

            int count = mountRoot.childCount;
            for (var i = count - 1; i >= 0; i--)
            {
                var child = mountRoot.GetChild(i);
                if (child != null)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            if (mountRoot.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(mountRoot.gameObject.scene);

            return count;
        }

        private static GameObject BuildPageGameObject(
            LiteCompiledPageDto page,
            RectTransform parent,
            Vector2 designSize,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            string siteId,
            AIToUGUILitePreviewBuildOptions options)
        {
            if (page == null || page.root == null || parent == null)
            {
                return null;
            }

            var root = new GameObject(
                SanitizeName(page.root.name, ResolvePageName(page)),
                typeof(RectTransform),
                typeof(CanvasRenderer));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            StretchToParent(rootRect);
            rootRect.localScale = Vector3.one;

            // 创建 Controller 作为 UI 内容根节点
            var controllerGo = new GameObject(
                "Controller",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasRenderer),
                typeof(GraphicRaycaster),
                typeof(Animator));
            var controllerRect = controllerGo.GetComponent<RectTransform>();
            controllerRect.SetParent(rootRect, false);
            StretchToParent(controllerRect);
            controllerRect.localScale = Vector3.one;

            var builtRoot = BuildNodeTree(controllerGo, page.root, theme, tokenMap, options, designSize, true, false, "ui_dark", Vector2.zero);
            LayoutBuiltNode(builtRoot, designSize, true, false);
            RefreshTextComponents(builtRoot);

            // 清理 Controller 上的 Image（背景由子节点 Background 承担）
            var controllerImage = controllerGo.GetComponent<Image>();
            if (controllerImage != null)
                UnityEngine.Object.DestroyImmediate(controllerImage);

            return root;
        }

        private static BuiltLiteNode BuildNodeTree(
            GameObject gameObject,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            Vector2 parentSize,
            bool isRoot,
            bool isInTemplate,
            string parentSpriteName,
            Vector2 parentGlobalPos)
        {
            float relX = node != null && node.layout != null ? ParseFloat(node.layout.left, 0f) : 0f;
            float relY = node != null && node.layout != null ? ParseFloat(node.layout.top, 0f) : 0f;
            var myGlobalPos = parentGlobalPos + new Vector2(relX, relY);

            var childHost = ApplyNodeToGameObject(gameObject, node, theme, tokenMap, options, parentSize, isRoot, isInTemplate, parentSpriteName, myGlobalPos, out var mySpriteName);
            var built = new BuiltLiteNode
            {
                node = node,
                gameObject = gameObject,
                rectTransform = gameObject.GetComponent<RectTransform>(),
                childHost = childHost
            };

            var children = node.children ?? Array.Empty<LiteCompiledNodeDto>();
            var childInTemplate = isInTemplate || (!string.IsNullOrEmpty(node.role) && node.role.StartsWith("templates/", StringComparison.OrdinalIgnoreCase));
            var nextParentSprite = !string.IsNullOrEmpty(mySpriteName) ? mySpriteName : parentSpriteName;

            for (var i = 0; i < children.Length; i++)
            {
                var childNode = children[i];
                if (childNode == null)
                {
                    continue;
                }

                var childObject = new GameObject(
                    SanitizeName(childNode.name, "Node"),
                    typeof(RectTransform),
                    typeof(CanvasRenderer));
                childObject.transform.SetParent(childHost, false);
                built.children.Add(BuildNodeTree(childObject, childNode, theme, tokenMap, options, parentSize, false, childInTemplate, nextParentSprite, myGlobalPos));
            }

            return built;
        }

        private static Transform ApplyNodeToGameObject(
            GameObject gameObject,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            Vector2 parentSize,
            bool isRoot,
            bool isInTemplate,
            string parentSpriteName,
            Vector2 myGlobalPos,
            out string mySpriteName)
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();
            InitializeRectTransform(rectTransform, isRoot);

            var controlType = ResolveControlType(node);
            var graphic = ConfigureGraphic(gameObject, node, controlType, tokenMap, theme, isRoot, isInTemplate, parentSpriteName, myGlobalPos, parentSize, out mySpriteName);
            ConfigureBorder(rectTransform, node, tokenMap, theme);
            if (ShouldMask(node, controlType, isInTemplate))
            {
                EnsureMask(gameObject, graphic);
            }

            Transform childHost = rectTransform;
            switch (controlType)
            {
                case AIToUGUILiteControlType.Button:
                    var btnSpriteName = BuildButton(gameObject, rectTransform, node, theme, tokenMap, options, parentSpriteName, myGlobalPos);
                    if (!string.IsNullOrEmpty(btnSpriteName))
                    {
                        mySpriteName = btnSpriteName;
                    }
                    break;
                case AIToUGUILiteControlType.Input:
                    BuildInput(gameObject, rectTransform, node, theme, tokenMap, options);
                    break;
                case AIToUGUILiteControlType.Toggle:
                    BuildToggle(gameObject, rectTransform, node, theme, tokenMap, options);
                    break;
                case AIToUGUILiteControlType.Slider:
                    BuildSlider(gameObject, rectTransform, theme, tokenMap);
                    break;
                case AIToUGUILiteControlType.Scrollbar:
                    BuildScrollbar(gameObject, rectTransform, theme, tokenMap);
                    break;
                case AIToUGUILiteControlType.Scroll:
                    childHost = BuildScroll(gameObject, rectTransform, node, tokenMap);
                    break;
                case AIToUGUILiteControlType.Dropdown:
                    BuildDropdown(gameObject, rectTransform, node, theme, tokenMap, options);
                    break;
                case AIToUGUILiteControlType.Progress:
                    BuildProgress(gameObject, rectTransform, node, theme, tokenMap);
                    break;
                case AIToUGUILiteControlType.Text:
                    {
                        var effectiveSprite = !string.IsNullOrEmpty(mySpriteName) ? mySpriteName : parentSpriteName;
                        BuildStandaloneText(gameObject, rectTransform, node, theme, tokenMap, options, effectiveSprite);
                    }
                    break;
                case AIToUGUILiteControlType.Image:
                case AIToUGUILiteControlType.Div:
                    if (!string.IsNullOrWhiteSpace(ResolveDisplayText(node)) &&
                        (node.children == null || node.children.Length == 0))
                    {
                        var effectiveSprite = !string.IsNullOrEmpty(mySpriteName) ? mySpriteName : parentSpriteName;
                        BuildStandaloneText(gameObject, rectTransform, node, theme, tokenMap, options, effectiveSprite);
                    }
                    break;
            }

            // ── Template card clickability ──
            // Template nodes (data-ui-role="templates/...") are <div> in HTML,
            // but often the whole card should be clickable. When a template has a
            // direct child named "Background", attach a Button component on that
            // Background so the card responds to clicks without polluting the
            // template root node.
            if (!string.IsNullOrEmpty(node.role) && node.role.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
            {
                var background = rectTransform.Find("Background");
                if (background != null)
                {
                    var bgImage = background.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.raycastTarget = true;
                        var button = background.gameObject.GetComponent<Button>() ?? background.gameObject.AddComponent<Button>();
                        button.targetGraphic = bgImage;
                    }
                }
            }

            return childHost;
        }

        private static void InitializeRectTransform(RectTransform rectTransform, bool isRoot)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            if (isRoot)
            {
                StretchToParent(rectTransform);
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void LayoutBuiltNode(BuiltLiteNode built, Vector2 size, bool isRoot, bool isAbsoluteChild)
        {
            if (built == null || built.rectTransform == null)
            {
                return;
            }

            if (isRoot)
            {
                StretchToParent(built.rectTransform);
            }
            else if (!isAbsoluteChild)
            {
                built.rectTransform.sizeDelta = size;
            }

            var flowChildren = new List<BuiltLiteNode>();
            var absoluteChildren = new List<BuiltLiteNode>();
            for (var i = 0; i < built.children.Count; i++)
            {
                var child = built.children[i];
                if (IsAbsolutePosition(child.node))
                {
                    absoluteChildren.Add(child);
                }
                else
                {
                    flowChildren.Add(child);
                }
            }

            var padding = ParseBox(GetStyle(built.node, "padding"));
            var innerSize = new Vector2(
                Mathf.Max(0f, size.x - padding.left - padding.right),
                Mathf.Max(0f, size.y - padding.top - padding.bottom));

            if (ResolveControlType(built.node) == AIToUGUILiteControlType.Scroll && built.childHost is RectTransform contentRect)
            {
                contentRect.sizeDelta = innerSize;
                contentRect.anchoredPosition = Vector2.zero;
            }

            if (string.Equals(GetStyle(built.node, "display"), "flex", StringComparison.OrdinalIgnoreCase))
            {
                LayoutFlexChildren(built, flowChildren, size, innerSize, padding);
            }
            else
            {
                LayoutBlockChildren(built, flowChildren, size, innerSize, padding);
            }

            for (var i = 0; i < absoluteChildren.Count; i++)
            {
                var child = absoluteChildren[i];
                var childSize = MeasureBuiltNode(child, size, false, false, false);
                ApplyAbsoluteRect(child.rectTransform, child.node, size, childSize);
                LayoutBuiltNode(child, childSize, false, true);
            }
        }

        private static void RefreshTextComponents(BuiltLiteNode built)
        {
            if (built == null)
            {
                return;
            }

            var text = FindPrimaryText(built.gameObject);
            // Guard: FindPrimaryText falls back to any descendant TMP. If the located text
            // actually belongs to one of this node's real child nodes, the parent must NOT
            // adopt it as its own label — otherwise a plain container (e.g. a scoreboard
            // wrapper) would stretch its first text child to fill the container and repaint
            // it with the container's style, corrupting that child's position/color/alignment.
            if (text != null && TextBelongsToChildNode(built, text))
            {
                text = null;
            }

            if (text != null)
            {
                var pSize = built.rectTransform != null ? (built.rectTransform.rect.size.sqrMagnitude > 0f ? built.rectTransform.rect.size : built.rectTransform.sizeDelta) : Vector2.zero;
                if (text.rectTransform != built.rectTransform && pSize.x > 0f && pSize.y > 0f)
                {
                    var padding = ParseBox(GetStyle(built.node, "padding"));
                    float padH = (padding.left > 0 || padding.right > 0) ? (padding.left + padding.right) : 16f;
                    float padV = (padding.top > 0 || padding.bottom > 0) ? (padding.top + padding.bottom) : 16f;
                    text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    text.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, pSize.x - padH), Mathf.Max(0f, pSize.y - padV));
                    text.rectTransform.anchoredPosition = Vector2.zero;
                }

                var sizeHint = text.rectTransform.rect.size;
                if (sizeHint.x <= 0f || sizeHint.y <= 0f)
                {
                    sizeHint = pSize;
                }

                ConfigureTextLayout(text, built.node, sizeHint);
                text.ForceMeshUpdate();
            }

            for (var i = 0; i < built.children.Count; i++)
            {
                RefreshTextComponents(built.children[i]);
            }
        }

        private static Vector2 MeasureBuiltNode(BuiltLiteNode built, Vector2 availableSize, bool isRoot, bool stretchWidth, bool stretchHeight)
        {
            if (built == null)
            {
                return Vector2.zero;
            }

            if (isRoot)
            {
                return availableSize;
            }

            var node = built.node;
            var padding = ParseBox(GetStyle(node, "padding"));
            var size = ResolveNodeSize(node, availableSize, new Vector2(-1f, -1f));
            var margins = ParseMargins(node);

            if (IsAbsolutePosition(node))
            {
                var left = ParseCssLength(GetStyle(node, "left"));
                var right = ParseCssLength(GetStyle(node, "right"));
                var top = ParseCssLength(GetStyle(node, "top"));
                var bottom = ParseCssLength(GetStyle(node, "bottom"));

                if (size.x < 0f && left.IsValid && right.IsValid)
                {
                    size.x = Mathf.Max(0f, availableSize.x - ResolveCssLength(left, availableSize.x, 0f) - ResolveCssLength(right, availableSize.x, 0f));
                }

                if (size.y < 0f && top.IsValid && bottom.IsValid)
                {
                    size.y = Mathf.Max(0f, availableSize.y - ResolveCssLength(top, availableSize.y, 0f) - ResolveCssLength(bottom, availableSize.y, 0f));
                }
            }

            if (stretchWidth && size.x < 0f)
            {
                size.x = Mathf.Max(0f, availableSize.x);
            }

            if (stretchHeight && size.y < 0f)
            {
                size.y = Mathf.Max(0f, availableSize.y);
            }

            var display = GetStyle(node, "display");
            var flowChildren = new List<BuiltLiteNode>();
            for (var i = 0; i < built.children.Count; i++)
            {
                if (!IsAbsolutePosition(built.children[i].node))
                {
                    flowChildren.Add(built.children[i]);
                }
            }

            if (string.Equals(display, "flex", StringComparison.OrdinalIgnoreCase) && flowChildren.Count > 0)
            {
                var direction = GetStyle(node, "flex-direction");
                var isRow = string.IsNullOrWhiteSpace(direction) || string.Equals(direction, "row", StringComparison.OrdinalIgnoreCase);
                var alignItems = GetStyle(node, "align-items");
                var contentAvailable = new Vector2(
                    Mathf.Max(0f, (size.x >= 0f ? size.x : availableSize.x) - padding.left - padding.right),
                    Mathf.Max(0f, (size.y >= 0f ? size.y : availableSize.y) - padding.top - padding.bottom));
                var gap = Mathf.Max(0f, ParseFloat(GetStyle(node, "gap"), 0f));
                var requiredWidth = 0f;
                var requiredHeight = 0f;

                for (var i = 0; i < flowChildren.Count; i++)
                {
                    var child = flowChildren[i];
                    var childMargins = ParseMargins(child.node);
                    var childAvailable = isRow
                        ? new Vector2(
                            contentAvailable.x,
                            Mathf.Max(0f, contentAvailable.y - ResolveEdge(childMargins.Top, contentAvailable.y) - ResolveEdge(childMargins.Bottom, contentAvailable.y)))
                        : new Vector2(
                            Mathf.Max(0f, contentAvailable.x - ResolveEdge(childMargins.Left, contentAvailable.x) - ResolveEdge(childMargins.Right, contentAvailable.x)),
                            contentAvailable.y);
                    var stretchCrossAxis = string.IsNullOrWhiteSpace(alignItems) || string.Equals(alignItems, "stretch", StringComparison.OrdinalIgnoreCase);
                    var childStretchWidth = !isRow && stretchCrossAxis;
                    var childSize = MeasureBuiltNode(child, childAvailable, false, childStretchWidth, false);

                    if (isRow)
                    {
                        requiredWidth += childSize.x + ResolveEdge(childMargins.Left, contentAvailable.x) + ResolveEdge(childMargins.Right, contentAvailable.x) + (i > 0 ? gap : 0f);
                        requiredHeight = Mathf.Max(requiredHeight, childSize.y + ResolveEdge(childMargins.Top, contentAvailable.y) + ResolveEdge(childMargins.Bottom, contentAvailable.y));
                    }
                    else
                    {
                        requiredWidth = Mathf.Max(requiredWidth, childSize.x + ResolveEdge(childMargins.Left, contentAvailable.x) + ResolveEdge(childMargins.Right, contentAvailable.x));
                        requiredHeight += childSize.y + ResolveEdge(childMargins.Top, contentAvailable.y) + ResolveEdge(childMargins.Bottom, contentAvailable.y) + (i > 0 ? gap : 0f);
                    }
                }

                if (size.x < 0f)
                {
                    size.x = requiredWidth + padding.left + padding.right;
                }

                if (size.y < 0f)
                {
                    size.y = requiredHeight + padding.top + padding.bottom;
                }
            }
            else if (flowChildren.Count > 0)
            {
                var contentAvailableWidth = Mathf.Max(0f, (size.x >= 0f ? size.x : availableSize.x) - padding.left - padding.right);
                var requiredWidth = 0f;
                var requiredHeight = 0f;
                for (var i = 0; i < flowChildren.Count; i++)
                {
                    var child = flowChildren[i];
                    var childMargins = ParseMargins(child.node);
                    var childAvailable = new Vector2(
                        Mathf.Max(0f, contentAvailableWidth - ResolveEdge(childMargins.Left, contentAvailableWidth) - ResolveEdge(childMargins.Right, contentAvailableWidth)),
                        availableSize.y);
                    var childSize = MeasureBuiltNode(child, childAvailable, false, !HasExplicitWidth(child.node), false);
                    requiredWidth = Mathf.Max(requiredWidth, childSize.x + ResolveEdge(childMargins.Left, contentAvailableWidth) + ResolveEdge(childMargins.Right, contentAvailableWidth));
                    requiredHeight += childSize.y + ResolveEdge(childMargins.Top, availableSize.y) + ResolveEdge(childMargins.Bottom, availableSize.y);
                }

                if (size.x < 0f)
                {
                    size.x = requiredWidth + padding.left + padding.right;
                }

                if (size.y < 0f)
                {
                    size.y = requiredHeight + padding.top + padding.bottom;
                }
            }
            else
            {
                var contentSize = MeasureLeafContent(built, availableSize, stretchWidth);
                if (size.x < 0f)
                {
                    size.x = contentSize.x + padding.left + padding.right;
                }

                if (size.y < 0f)
                {
                    size.y = contentSize.y + padding.top + padding.bottom;
                }
            }

            size.x = Mathf.Max(0f, ClampLength(size.x, ParseCssLength(GetStyle(node, "min-width")), ParseCssLength(GetStyle(node, "max-width")), availableSize.x));
            size.y = Mathf.Max(0f, ClampLength(size.y, ParseCssLength(GetStyle(node, "min-height")), ParseCssLength(GetStyle(node, "max-height")), availableSize.y));
            return size;
        }

        private static Vector2 MeasureLeafContent(BuiltLiteNode built, Vector2 availableSize, bool stretchWidth)
        {
            var node = built.node;
            var controlType = ResolveControlType(node);
            var text = FindPrimaryText(built.gameObject);
            // Same guard as RefreshTextComponents: a container must not adopt a real child
            // node's text as its own leaf content, otherwise the child's text rect gets
            // resized/restyled with the container's bounds and styles.
            if (text != null && TextBelongsToChildNode(built, text))
            {
                text = null;
            }
            if (text == null)
            {
                return ResolveDefaultLeafSize(controlType);
            }

            var padding = ParseBox(GetStyle(node, "padding"));
            var maxWidth = ParseCssLength(GetStyle(node, "max-width"));
            var explicitWidth = ParseCssLength(GetStyle(node, "width"));
            var explicitHeight = ParseCssLength(GetStyle(node, "height"));
            var maxHeight = ParseCssLength(GetStyle(node, "max-height"));

            var widthConstraint = 4096f;
            if (explicitWidth.IsValid)
            {
                widthConstraint = Mathf.Max(0f, ResolveCssLength(explicitWidth, availableSize.x, 0f) - padding.left - padding.right);
            }
            else if (stretchWidth)
            {
                widthConstraint = Mathf.Max(0f, availableSize.x - padding.left - padding.right);
            }
            else if (maxWidth.IsValid)
            {
                widthConstraint = ResolveCssLength(maxWidth, availableSize.x, availableSize.x);
            }
            else if (availableSize.x > 0f)
            {
                widthConstraint = Mathf.Max(0f, availableSize.x - padding.left - padding.right);
            }

            var heightConstraint = 0f;
            if (explicitHeight.IsValid)
            {
                heightConstraint = Mathf.Max(0f, ResolveCssLength(explicitHeight, availableSize.y, 0f) - padding.top - padding.bottom);
            }
            else if (maxHeight.IsValid)
            {
                heightConstraint = Mathf.Max(0f, ResolveCssLength(maxHeight, availableSize.y, availableSize.y) - padding.top - padding.bottom);
            }
            else if (availableSize.y > 0f)
            {
                heightConstraint = Mathf.Max(0f, availableSize.y - padding.top - padding.bottom);
            }

            ConfigureTextLayout(text, node, new Vector2(widthConstraint, heightConstraint));
            text.ForceMeshUpdate();

            var preferred = text.GetPreferredValues(
                text.text ?? string.Empty,
                Mathf.Max(1f, widthConstraint),
                heightConstraint > 0f ? heightConstraint : 0f);

            var width = explicitWidth.IsValid
                ? ResolveCssLength(explicitWidth, availableSize.x, preferred.x)
                : stretchWidth
                    ? Mathf.Max(0f, availableSize.x - padding.left - padding.right)
                    : maxWidth.IsValid
                        ? Mathf.Min(preferred.x, ResolveCssLength(maxWidth, availableSize.x, preferred.x))
                        : preferred.x;

            if (widthConstraint > 0f && widthConstraint < 4096f)
            {
                width = Mathf.Min(width, widthConstraint);
            }

            var height = ClampMeasuredTextHeight(node, text.fontSize, preferred.y, heightConstraint);
            switch (controlType)
            {
                case AIToUGUILiteControlType.Button:
                    width += 24f;
                    height = Mathf.Max(height, 32f);
                    break;
                case AIToUGUILiteControlType.Input:
                    width = Mathf.Max(width, DefaultElementSize.x);
                    height = Mathf.Max(height, 36f);
                    break;
                case AIToUGUILiteControlType.Dropdown:
                    width += 28f;
                    width = Mathf.Max(width, DefaultElementSize.x);
                    height = Mathf.Max(height, 36f);
                    break;
                case AIToUGUILiteControlType.Toggle:
                    width += 30f;
                    height = Mathf.Max(height, 22f);
                    break;
            }

            return new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private static void LayoutFlexChildren(BuiltLiteNode parent, List<BuiltLiteNode> children, Vector2 parentSize, Vector2 innerSize, RectOffset padding)
        {
            if (children == null || children.Count == 0)
            {
                return;
            }

            var direction = GetStyle(parent.node, "flex-direction");
            var justify = GetStyle(parent.node, "justify-content");
            var align = GetStyle(parent.node, "align-items");
            var gap = Mathf.Max(0f, ParseFloat(GetStyle(parent.node, "gap"), 0f));

            // Per CSS spec, flex-direction defaults to "row" (not "column").
            if (string.IsNullOrWhiteSpace(direction) || string.Equals(direction, "row", StringComparison.OrdinalIgnoreCase))
            {
                LayoutRowChildren(parent, children, parentSize, innerSize, padding, justify, align, gap);
                return;
            }

            LayoutColumnChildren(parent, children, parentSize, innerSize, padding, justify, align, gap);
        }

        private static void LayoutRowChildren(BuiltLiteNode parent, List<BuiltLiteNode> children, Vector2 parentSize, Vector2 innerSize, RectOffset padding, string justify, string align, float gap)
        {
            var childSizes = new Vector2[children.Count];
            var childMargins = new LiteCssEdges[children.Count];
            var totalFixedWidth = 0f;
            var firstAutoLeftIndex = -1;

            for (var i = 0; i < children.Count; i++)
            {
                childMargins[i] = ParseMargins(children[i].node);
                var childAvailable = new Vector2(
                    innerSize.x,
                    Mathf.Max(0f, innerSize.y - ResolveEdge(childMargins[i].Top, innerSize.y) - ResolveEdge(childMargins[i].Bottom, innerSize.y)));
                childSizes[i] = MeasureBuiltNode(children[i], childAvailable, false, false, false);
                totalFixedWidth += childSizes[i].x + ResolveEdge(childMargins[i].Left, innerSize.x) + ResolveEdge(childMargins[i].Right, innerSize.x);
                if (firstAutoLeftIndex < 0 && childMargins[i].Left.IsAuto)
                {
                    firstAutoLeftIndex = i;
                }
            }

            ApplyFlexShrink(children, childSizes, childMargins, innerSize.x, gap, true);
            totalFixedWidth = CalculateMainAxisFootprint(children, childSizes, childMargins, innerSize.x, true);

            float x = padding.left;
            var actualGap = gap;
            if (firstAutoLeftIndex < 0)
            {
                if (string.Equals(justify, "space-between", StringComparison.OrdinalIgnoreCase) && children.Count > 1)
                {
                    actualGap = Mathf.Max(gap, (innerSize.x - totalFixedWidth) / (children.Count - 1));
                }
                else if (string.Equals(justify, "center", StringComparison.OrdinalIgnoreCase))
                {
                    x += Mathf.Max(0f, (innerSize.x - (totalFixedWidth + gap * Mathf.Max(0, children.Count - 1))) * 0.5f);
                }
                else if (string.Equals(justify, "flex-end", StringComparison.OrdinalIgnoreCase) || string.Equals(justify, "end", StringComparison.OrdinalIgnoreCase))
                {
                    x += Mathf.Max(0f, innerSize.x - (totalFixedWidth + gap * Mathf.Max(0, children.Count - 1)));
                }
            }

            float rightCursor = padding.left + innerSize.x;
            if (firstAutoLeftIndex >= 0)
            {
                rightCursor = padding.left + innerSize.x;
                for (var i = children.Count - 1; i >= firstAutoLeftIndex; i--)
                {
                    rightCursor -= ResolveEdge(childMargins[i].Right, innerSize.x) + childSizes[i].x;
                    var y = ResolveCrossAxisPosition(align, padding.top, innerSize.y, childSizes[i].y, childMargins[i], false);
                    SetFlowRect(children[i].rectTransform, new Vector2(rightCursor, y), childSizes[i], parentSize);
                    LayoutBuiltNode(children[i], childSizes[i], false, false);
                    rightCursor -= ResolveEdge(childMargins[i].Left, innerSize.x) + gap;
                }
            }

            var lastLeftIndex = firstAutoLeftIndex >= 0 ? firstAutoLeftIndex : children.Count;
            for (var i = 0; i < lastLeftIndex; i++)
            {
                x += ResolveEdge(childMargins[i].Left, innerSize.x);
                var y = ResolveCrossAxisPosition(align, padding.top, innerSize.y, childSizes[i].y, childMargins[i], false);
                SetFlowRect(children[i].rectTransform, new Vector2(x, y), childSizes[i], parentSize);
                LayoutBuiltNode(children[i], childSizes[i], false, false);
                x += childSizes[i].x + ResolveEdge(childMargins[i].Right, innerSize.x);
                if (i < lastLeftIndex - 1)
                {
                    x += actualGap;
                }
            }
        }

        private static void LayoutColumnChildren(BuiltLiteNode parent, List<BuiltLiteNode> children, Vector2 parentSize, Vector2 innerSize, RectOffset padding, string justify, string align, float gap)
        {
            var childSizes = new Vector2[children.Count];
            var childMargins = new LiteCssEdges[children.Count];
            var totalFixedHeight = 0f;
            var firstAutoTopIndex = -1;

            for (var i = 0; i < children.Count; i++)
            {
                childMargins[i] = ParseMargins(children[i].node);
                var childAvailable = new Vector2(
                    Mathf.Max(0f, innerSize.x - ResolveEdge(childMargins[i].Left, innerSize.x) - ResolveEdge(childMargins[i].Right, innerSize.x)),
                    innerSize.y);
                childSizes[i] = MeasureBuiltNode(
                    children[i],
                    childAvailable,
                    false,
                    string.IsNullOrWhiteSpace(align) || string.Equals(align, "stretch", StringComparison.OrdinalIgnoreCase),
                    false);
                totalFixedHeight += childSizes[i].y + ResolveEdge(childMargins[i].Top, innerSize.y) + ResolveEdge(childMargins[i].Bottom, innerSize.y);
                if (firstAutoTopIndex < 0 && childMargins[i].Top.IsAuto)
                {
                    firstAutoTopIndex = i;
                }
            }

            ApplyFlexShrink(children, childSizes, childMargins, innerSize.y, gap, false);
            totalFixedHeight = CalculateMainAxisFootprint(children, childSizes, childMargins, innerSize.y, false);

            float y = padding.top;
            var actualGap = gap;
            if (firstAutoTopIndex < 0)
            {
                if (string.Equals(justify, "space-between", StringComparison.OrdinalIgnoreCase) && children.Count > 1)
                {
                    actualGap = Mathf.Max(gap, (innerSize.y - totalFixedHeight) / (children.Count - 1));
                }
                else if (string.Equals(justify, "center", StringComparison.OrdinalIgnoreCase))
                {
                    y += Mathf.Max(0f, (innerSize.y - (totalFixedHeight + gap * Mathf.Max(0, children.Count - 1))) * 0.5f);
                }
                else if (string.Equals(justify, "flex-end", StringComparison.OrdinalIgnoreCase) || string.Equals(justify, "end", StringComparison.OrdinalIgnoreCase))
                {
                    y += Mathf.Max(0f, innerSize.y - (totalFixedHeight + gap * Mathf.Max(0, children.Count - 1)));
                }
            }

            float bottomCursor = padding.top + innerSize.y;
            if (firstAutoTopIndex >= 0)
            {
                bottomCursor = padding.top + innerSize.y;
                for (var i = children.Count - 1; i >= firstAutoTopIndex; i--)
                {
                    bottomCursor -= ResolveEdge(childMargins[i].Bottom, innerSize.y) + childSizes[i].y;
                    var x = ResolveCrossAxisPosition(align, padding.left, innerSize.x, childSizes[i].x, childMargins[i], true);
                    SetFlowRect(children[i].rectTransform, new Vector2(x, bottomCursor), childSizes[i], parentSize);
                    LayoutBuiltNode(children[i], childSizes[i], false, false);
                    bottomCursor -= ResolveEdge(childMargins[i].Top, innerSize.y) + gap;
                }
            }

            var lastTopIndex = firstAutoTopIndex >= 0 ? firstAutoTopIndex : children.Count;
            for (var i = 0; i < lastTopIndex; i++)
            {
                y += ResolveEdge(childMargins[i].Top, innerSize.y);
                var x = ResolveCrossAxisPosition(align, padding.left, innerSize.x, childSizes[i].x, childMargins[i], true);
                SetFlowRect(children[i].rectTransform, new Vector2(x, y), childSizes[i], parentSize);
                LayoutBuiltNode(children[i], childSizes[i], false, false);
                y += childSizes[i].y + ResolveEdge(childMargins[i].Bottom, innerSize.y);
                if (i < lastTopIndex - 1)
                {
                    y += actualGap;
                }
            }
        }

        private static void ApplyFlexShrink(List<BuiltLiteNode> children, Vector2[] childSizes, LiteCssEdges[] childMargins, float availableMain, float gap, bool horizontal)
        {
            if (children == null || childSizes == null || childMargins == null || children.Count == 0 || availableMain <= 0f)
            {
                return;
            }

            var gapTotal = gap * Mathf.Max(0, children.Count - 1);
            var occupied = CalculateMainAxisFootprint(children, childSizes, childMargins, availableMain, horizontal) + gapTotal;
            var overflow = occupied - availableMain;
            if (overflow <= 0.01f)
            {
                return;
            }

            var weights = new float[children.Count];
            for (var pass = 0; pass < children.Count && overflow > 0.01f; pass++)
            {
                var totalWeight = 0f;
                for (var i = 0; i < children.Count; i++)
                {
                    var currentMain = horizontal ? childSizes[i].x : childSizes[i].y;
                    var minMain = ResolveFlexMinSize(children[i].node, availableMain, horizontal);
                    if (currentMain <= minMain + 0.01f)
                    {
                        weights[i] = 0f;
                        continue;
                    }

                    var shrink = ResolveFlexShrink(children[i].node);
                    weights[i] = Mathf.Max(0f, shrink) * Mathf.Max(1f, currentMain);
                    totalWeight += weights[i];
                }

                if (totalWeight <= 0.01f)
                {
                    break;
                }

                var consumed = 0f;
                for (var i = 0; i < children.Count; i++)
                {
                    if (weights[i] <= 0f)
                    {
                        continue;
                    }

                    var currentMain = horizontal ? childSizes[i].x : childSizes[i].y;
                    var minMain = ResolveFlexMinSize(children[i].node, availableMain, horizontal);
                    var reducible = Mathf.Max(0f, currentMain - minMain);
                    if (reducible <= 0.01f)
                    {
                        continue;
                    }

                    var targetReduction = overflow * (weights[i] / totalWeight);
                    var appliedReduction = Mathf.Min(reducible, targetReduction);
                    if (appliedReduction <= 0.01f)
                    {
                        continue;
                    }

                    if (horizontal)
                    {
                        childSizes[i].x = Mathf.Max(minMain, childSizes[i].x - appliedReduction);
                    }
                    else
                    {
                        childSizes[i].y = Mathf.Max(minMain, childSizes[i].y - appliedReduction);
                    }

                    consumed += appliedReduction;
                }

                if (consumed <= 0.01f)
                {
                    break;
                }

                overflow -= consumed;
            }
        }

        private static float CalculateMainAxisFootprint(List<BuiltLiteNode> children, Vector2[] childSizes, LiteCssEdges[] childMargins, float parentMainSize, bool horizontal)
        {
            float total = 0f;
            for (var i = 0; i < children.Count; i++)
            {
                total += horizontal
                    ? childSizes[i].x + ResolveEdge(childMargins[i].Left, parentMainSize) + ResolveEdge(childMargins[i].Right, parentMainSize)
                    : childSizes[i].y + ResolveEdge(childMargins[i].Top, parentMainSize) + ResolveEdge(childMargins[i].Bottom, parentMainSize);
            }

            return total;
        }

        private static float ResolveFlexShrink(LiteCompiledNodeDto node)
        {
            var explicitShrink = ParseFloat(GetStyle(node, "flex-shrink"), float.NaN);
            if (!float.IsNaN(explicitShrink))
            {
                return Mathf.Max(0f, explicitShrink);
            }

            var flex = GetStyle(node, "flex");
            if (string.Equals(flex, "none", StringComparison.OrdinalIgnoreCase))
            {
                return 0f;
            }

            return 1f;
        }

        private static float ResolveFlexMinSize(LiteCompiledNodeDto node, float parentMainSize, bool horizontal)
        {
            var min = ParseCssLength(GetStyle(node, horizontal ? "min-width" : "min-height"));
            return Mathf.Max(0f, ResolveCssLength(min, parentMainSize, 0f));
        }

        private static void LayoutBlockChildren(BuiltLiteNode parent, List<BuiltLiteNode> children, Vector2 parentSize, Vector2 innerSize, RectOffset padding)
        {
            if (children == null || children.Count == 0)
            {
                return;
            }

            float y = padding.top;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var margins = ParseMargins(child.node);
                var childSize = MeasureBuiltNode(
                    child,
                    new Vector2(
                        Mathf.Max(0f, innerSize.x - ResolveEdge(margins.Left, innerSize.x) - ResolveEdge(margins.Right, innerSize.x)),
                        innerSize.y),
                    false,
                    !HasExplicitWidth(child.node),
                    false);
                y += ResolveEdge(margins.Top, innerSize.y);
                var x = padding.left + ResolveEdge(margins.Left, innerSize.x);
                SetFlowRect(child.rectTransform, new Vector2(x, y), childSize, parentSize);
                LayoutBuiltNode(child, childSize, false, false);
                y += childSize.y + ResolveEdge(margins.Bottom, innerSize.y);
            }
        }

        private static float ResolveCrossAxisPosition(string align, float startOffset, float innerSize, float childSize, LiteCssEdges margins, bool horizontalAxis)
        {
            var leading = horizontalAxis ? ResolveEdge(margins.Left, innerSize) : ResolveEdge(margins.Top, innerSize);
            var trailing = horizontalAxis ? ResolveEdge(margins.Right, innerSize) : ResolveEdge(margins.Bottom, innerSize);
            if (string.Equals(align, "center", StringComparison.OrdinalIgnoreCase))
            {
                return startOffset + Mathf.Max(0f, (innerSize - childSize) * 0.5f) + leading - trailing;
            }

            if (string.Equals(align, "flex-end", StringComparison.OrdinalIgnoreCase) || string.Equals(align, "end", StringComparison.OrdinalIgnoreCase))
            {
                return startOffset + Mathf.Max(0f, innerSize - childSize - trailing);
            }

            return startOffset + leading;
        }

        private struct AnchorResult
        {
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public bool isStretch;
        }

        private static AnchorResult InferAnchorPreset(
            LiteCompiledNodeDto node,
            float x,
            float y,
            Vector2 size,
            Vector2 parentSize)
        {
            var defaultCenter = new AnchorResult
            {
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                isStretch = false
            };

            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return defaultCenter;
            }

            // 1. 全屏拉伸（背景图、全屏暗色遮罩）
            if (size.x >= parentSize.x - 4f && size.y >= parentSize.y - 4f)
            {
                return new AnchorResult
                {
                    anchorMin = Vector2.zero,
                    anchorMax = Vector2.one,
                    isStretch = true
                };
            }

            var nodeName = node != null && !string.IsNullOrEmpty(node.name) ? node.name.ToLowerInvariant() : "";
            var role = node != null && !string.IsNullOrEmpty(node.role) ? node.role.ToLowerInvariant() : "";

            // 弹窗与对话框默认中心对齐（遮罩背景除外）
            if (role.Contains("modal") || role.Contains("dialog") || nodeName.Contains("modal") || nodeName.Contains("dialog"))
            {
                if (nodeName.Contains("overlay") || nodeName.Contains("dim") || role.Contains("background"))
                {
                    if (size.x >= parentSize.x * 0.8f && size.y >= parentSize.y * 0.8f)
                    {
                        return new AnchorResult
                        {
                            anchorMin = Vector2.zero,
                            anchorMax = Vector2.one,
                            isStretch = true
                        };
                    }
                }
                return defaultCenter;
            }

            float distLeft = x;
            float distRight = parentSize.x - (x + size.x);
            float distTop = y;
            float distBottom = parentSize.y - (y + size.y);

            // 2. 顶部通栏横向拉伸（宽度超过85%且紧贴顶部）
            if (size.x >= parentSize.x * 0.85f && distTop <= 40f)
            {
                return new AnchorResult
                {
                    anchorMin = new Vector2(0f, 1f),
                    anchorMax = new Vector2(1f, 1f),
                    isStretch = true
                };
            }

            // 3. 底部通栏横向拉伸（宽度超过85%且紧贴底部）
            if (size.x >= parentSize.x * 0.85f && distBottom <= 40f)
            {
                return new AnchorResult
                {
                    anchorMin = new Vector2(0f, 0f),
                    anchorMax = new Vector2(1f, 0f),
                    isStretch = true
                };
            }

            // 边缘贴靠阈值
            float hThreshold = Mathf.Min(parentSize.x * 0.18f, 260f);
            float vThreshold = Mathf.Min(parentSize.y * 0.18f, 180f);

            bool nearLeft = distLeft <= hThreshold && distRight > distLeft * 1.5f;
            bool nearRight = distRight <= hThreshold && distLeft > distRight * 1.5f;
            bool nearTop = distTop <= vThreshold && distBottom > distTop * 1.5f;
            bool nearBottom = distBottom <= vThreshold && distTop > distBottom * 1.5f;

            // 语义辅助判定
            if (nodeName.Contains("back") || nodeName.Contains("return"))
            {
                nearLeft = true;
                nearTop = true;
            }
            else if (nodeName.Contains("close") && (nearTop || distTop < parentSize.y * 0.35f))
            {
                nearRight = true;
                nearTop = true;
            }

            // 4. 四个角落对齐
            if (nearLeft && nearTop)
            {
                // 左上 (Top-Left)
                return new AnchorResult { anchorMin = new Vector2(0f, 1f), anchorMax = new Vector2(0f, 1f), isStretch = false };
            }
            if (nearRight && nearTop)
            {
                // 右上 (Top-Right)
                return new AnchorResult { anchorMin = new Vector2(1f, 1f), anchorMax = new Vector2(1f, 1f), isStretch = false };
            }
            if (nearLeft && nearBottom)
            {
                // 左下 (Bottom-Left)
                return new AnchorResult { anchorMin = new Vector2(0f, 0f), anchorMax = new Vector2(0f, 0f), isStretch = false };
            }
            if (nearRight && nearBottom)
            {
                // 右下 (Bottom-Right)
                return new AnchorResult { anchorMin = new Vector2(1f, 0f), anchorMax = new Vector2(1f, 0f), isStretch = false };
            }

            // 5. 单侧居中对齐（上方、下方、左侧、右侧）
            float centerX = x + size.x * 0.5f;
            float centerY = y + size.y * 0.5f;
            float parentCenterX = parentSize.x * 0.5f;
            float parentCenterY = parentSize.y * 0.5f;

            if (nearTop && Mathf.Abs(centerX - parentCenterX) < parentSize.x * 0.25f)
            {
                // 顶部居中 (Top-Center)
                return new AnchorResult { anchorMin = new Vector2(0.5f, 1f), anchorMax = new Vector2(0.5f, 1f), isStretch = false };
            }
            if (nearBottom && Mathf.Abs(centerX - parentCenterX) < parentSize.x * 0.25f)
            {
                // 底部居中 (Bottom-Center)
                return new AnchorResult { anchorMin = new Vector2(0.5f, 0f), anchorMax = new Vector2(0.5f, 0f), isStretch = false };
            }
            if (nearLeft && Mathf.Abs(centerY - parentCenterY) < parentSize.y * 0.25f)
            {
                // 左侧居中 (Left-Center)
                return new AnchorResult { anchorMin = new Vector2(0f, 0.5f), anchorMax = new Vector2(0f, 0.5f), isStretch = false };
            }
            if (nearRight && Mathf.Abs(centerY - parentCenterY) < parentSize.y * 0.25f)
            {
                // 右侧居中 (Right-Center)
                return new AnchorResult { anchorMin = new Vector2(1f, 0.5f), anchorMax = new Vector2(1f, 0.5f), isStretch = false };
            }

            // 6. 默认兜底：界面中心对齐 (Middle-Center)
            return defaultCenter;
        }

        private static void SetFlowRect(RectTransform rectTransform, Vector2 topLeft, Vector2 size, Vector2 parentSize)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            float posX = (topLeft.x + size.x * 0.5f) - (0.5f * parentSize.x);
            float posY = 0.5f * parentSize.y - (topLeft.y + size.y * 0.5f);
            rectTransform.anchoredPosition = new Vector2(posX, posY);
        }

        private static void ApplyAbsoluteRect(RectTransform rectTransform, LiteCompiledNodeDto node, Vector2 parentSize, Vector2 fallbackSize)
        {
            if (rectTransform == null || node == null)
            {
                return;
            }

            var left = ParseCssLength(GetStyle(node, "left"));
            var right = ParseCssLength(GetStyle(node, "right"));
            var top = ParseCssLength(GetStyle(node, "top"));
            var bottom = ParseCssLength(GetStyle(node, "bottom"));
            var size = ResolveNodeSize(node, parentSize, fallbackSize);

            if (size.x < 0f && left.IsValid && right.IsValid)
            {
                size.x = Mathf.Max(0f, parentSize.x - ResolveCssLength(left, parentSize.x, 0f) - ResolveCssLength(right, parentSize.x, 0f));
            }

            if (size.y < 0f && top.IsValid && bottom.IsValid)
            {
                size.y = Mathf.Max(0f, parentSize.y - ResolveCssLength(top, parentSize.y, 0f) - ResolveCssLength(bottom, parentSize.y, 0f));
            }

            size.x = Mathf.Max(0f, size.x < 0f ? fallbackSize.x : size.x);
            size.y = Mathf.Max(0f, size.y < 0f ? fallbackSize.y : size.y);

            float x;
            if (left.IsValid)
            {
                x = ResolveCssLength(left, parentSize.x, 0f);
            }
            else if (right.IsValid)
            {
                x = parentSize.x - ResolveCssLength(right, parentSize.x, 0f) - size.x;
            }
            else
            {
                x = 0f;
            }

            float y;
            if (top.IsValid)
            {
                y = ResolveCssLength(top, parentSize.y, 0f);
            }
            else if (bottom.IsValid)
            {
                y = parentSize.y - ResolveCssLength(bottom, parentSize.y, 0f) - size.y;
            }
            else
            {
                y = 0f;
            }

            var anchor = InferAnchorPreset(node, x, y, size, parentSize);
            rectTransform.anchorMin = anchor.anchorMin;
            rectTransform.anchorMax = anchor.anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (anchor.isStretch)
            {
                if (anchor.anchorMin == Vector2.zero && anchor.anchorMax == Vector2.one)
                {
                    rectTransform.offsetMin = new Vector2(x, parentSize.y - (y + size.y));
                    rectTransform.offsetMax = new Vector2(-(parentSize.x - (x + size.x)), -y);
                }
                else if (Mathf.Approximately(anchor.anchorMin.y, anchor.anchorMax.y))
                {
                    rectTransform.sizeDelta = new Vector2(size.x - parentSize.x, size.y);
                    float posX = (x + size.x * 0.5f) - (0.5f * parentSize.x);
                    float posY = parentSize.y * (1f - anchor.anchorMin.y) - (y + size.y * 0.5f);
                    rectTransform.anchoredPosition = new Vector2(posX, posY);
                }
                else
                {
                    rectTransform.sizeDelta = size;
                    float posX = (x + size.x * 0.5f) - (anchor.anchorMin.x * parentSize.x);
                    float posY = parentSize.y * (1f - anchor.anchorMin.y) - (y + size.y * 0.5f);
                    rectTransform.anchoredPosition = new Vector2(posX, posY);
                }
            }
            else
            {
                rectTransform.sizeDelta = size;
                float posX = (x + size.x * 0.5f) - (anchor.anchorMin.x * parentSize.x);
                float posY = parentSize.y * (1f - anchor.anchorMin.y) - (y + size.y * 0.5f);
                rectTransform.anchoredPosition = new Vector2(posX, posY);
            }
        }

        private static Graphic ConfigureGraphic(
            GameObject gameObject,
            LiteCompiledNodeDto node,
            AIToUGUILiteControlType controlType,
            Dictionary<string, string> tokenMap,
            LiteCompiledThemeDto theme,
            bool isRoot,
            bool isInTemplate,
            string parentSpriteName,
            Vector2 myGlobalPos,
            Vector2 parentSize,
            out string mySpriteName)
        {
            mySpriteName = string.Empty;

            // Button handles its own Image as a child node in BuildButton() — skip it entirely.
            if (controlType == AIToUGUILiteControlType.Button)
            {
                return null;
            }

            var needsGraphic = controlType == AIToUGUILiteControlType.Input ||
                               controlType == AIToUGUILiteControlType.Dropdown ||
                               controlType == AIToUGUILiteControlType.Progress ||
                               controlType == AIToUGUILiteControlType.Image ||
                               HasVisibleFill(node, tokenMap) ||
                               IsStructuralUiElement(node) ||
                               (isRoot && theme != null);

            if (!needsGraphic)
            {
                return null;
            }

            var image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            var parsedColor = ResolveBackgroundColor(node, controlType, tokenMap, theme, isRoot);
            
            // 应用 ui_color 色彩图集子 Sprite 智能赋图，带真实物理坐标相交避让
            var mySprite = ApplyColorAtlasSprite(image, node, controlType, parsedColor, parentSpriteName, myGlobalPos, parentSize);
            if (mySprite != null)
            {
                mySpriteName = mySprite.name;
            }

            // Inside templates, only the Background child (which carries the Button component)
            // should block raycasts. All other decorative images/divs should let clicks pass
            // through to the Background (or to whichever child is the actual hit target).
            if (isInTemplate)
            {
                image.raycastTarget = false;
            }

            return image;
        }

        private static bool HasVisibleFill(LiteCompiledNodeDto node, Dictionary<string, string> tokenMap)
        {
            return TryResolveColor(ResolveBackgroundColorValue(node), tokenMap, out _);
        }

        private static bool ShouldMask(LiteCompiledNodeDto node, AIToUGUILiteControlType controlType, bool isInTemplate)
        {
            if (controlType == AIToUGUILiteControlType.Scroll)
            {
                return false;
            }

            // ScrollRect content children (templates) are laid out by GridLayoutGroup
            // and clipped by the Viewport's RectMask2D. They do not need their own Mask.
            if (!string.IsNullOrEmpty(node.role) && node.role.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // All descendants inside a template are clipped by the Viewport's RectMask2D.
            // Adding Mask on individual decorative nodes causes draw-call overhead and
            // can interfere with the Background Button's hit testing.
            if (isInTemplate)
            {
                return false;
            }

            var overflow = GetStyle(node, "overflow");
            var overflowX = GetStyle(node, "overflow-x");
            var overflowY = GetStyle(node, "overflow-y");
            return string.Equals(overflow, "hidden", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(overflowX, "hidden", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(overflowY, "hidden", StringComparison.OrdinalIgnoreCase);
        }

        private static void ConfigureBorder(RectTransform rectTransform, LiteCompiledNodeDto node, Dictionary<string, string> tokenMap, LiteCompiledThemeDto theme)
        {
            if (rectTransform == null || node == null)
            {
                return;
            }

            if (!TryResolveBorder(node, tokenMap, theme, out var width, out var color) || width <= 0f || color.a <= 0f)
            {
                return;
            }

            CreateBorderEdge(rectTransform, "__lite_BorderTop", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -width), new Vector2(0f, 0f));
            CreateBorderEdge(rectTransform, "__lite_BorderBottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, width));
            CreateBorderEdge(rectTransform, "__lite_BorderLeft", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, width), new Vector2(width, -width));
            CreateBorderEdge(rectTransform, "__lite_BorderRight", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-width, width), new Vector2(0f, -width));
        }

        private static bool TryResolveBorder(LiteCompiledNodeDto node, Dictionary<string, string> tokenMap, LiteCompiledThemeDto theme, out float width, out Color color)
        {
            width = 0f;
            color = Color.clear;

            if (node == null)
            {
                return false;
            }

            width = Mathf.Max(0f, node.visual != null ? node.visual.outlineWidth : 0f);
            var colorRaw = FirstNonEmpty(
                node.visual != null ? node.visual.outlineColor : null,
                ExtractBorderColor(node.visual != null ? node.visual.border : null),
                theme != null ? theme.outlineColor : null);

            if (width <= 0f && node.visual != null && !string.IsNullOrWhiteSpace(node.visual.border))
            {
                width = Mathf.Max(0f, ParseFloat(node.visual.border, 0f));
            }

            if (width < 10f)
            {
                return false;
            }

            return TryResolveColor(colorRaw, tokenMap, out color);
        }

        private static string ExtractBorderColor(string rawBorder)
        {
            if (string.IsNullOrWhiteSpace(rawBorder))
            {
                return string.Empty;
            }

            var colorMatch = ColorRegex.Match(rawBorder);
            return colorMatch.Success ? colorMatch.Value : string.Empty;
        }

        private static void CreateBorderEdge(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rectTransform = edge.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;

            var image = edge.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void EnsureMask(GameObject gameObject, Graphic existingGraphic)
        {
            if (gameObject == null)
            {
                return;
            }

            var graphic = existingGraphic as Image ?? gameObject.GetComponent<Image>();
            if (graphic == null)
            {
                graphic = gameObject.AddComponent<Image>();
                graphic.color = new Color(1f, 1f, 1f, 0.001f);
            }

            var mask = gameObject.GetComponent<Mask>() ?? gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = graphic.color.a > 0.01f;
        }

        private static string BuildButton(
            GameObject gameObject,
            RectTransform rectTransform,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            string parentSpriteName,
            Vector2 myGlobalPos)
        {
            // Create background Image as a child node (not on the Button node itself).
            var bgColor = ResolveBackgroundColor(node, AIToUGUILiteControlType.Button, tokenMap, theme, false);
            var bgImage = CreateFillImage(rectTransform, "Background", bgColor, node, parentSpriteName, myGlobalPos);
            Stretch(bgImage.rectTransform, 0f, 0f);
            bgImage.raycastTarget = true;

            // Button component on the root node, targetGraphic points to child Image.
            var button = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            button.targetGraphic = bgImage;

            // Only create Label child when the button has display text.
            var displayText = ResolveDisplayText(node);
            if (!string.IsNullOrWhiteSpace(displayText))
            {
                var labelName = DeriveButtonLabelName(node.name);
                var textRect = CreateChildRect(rectTransform, labelName);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                var parentSize = rectTransform.rect.size.sqrMagnitude > 0f ? rectTransform.rect.size : rectTransform.sizeDelta;
                var padding = ParseBox(GetStyle(node, "padding"));
                float padH = (padding.left > 0 || padding.right > 0) ? (padding.left + padding.right) : 16f;
                float padV = (padding.top > 0 || padding.bottom > 0) ? (padding.top + padding.bottom) : 16f;
                var textWidth = Mathf.Max(0f, parentSize.x - padH);
                var textHeight = Mathf.Max(0f, parentSize.y - padV);
                textRect.sizeDelta = new Vector2(textWidth, textHeight);

                var label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.text = displayText;
                var buttonBgSpriteName = bgImage != null && bgImage.sprite != null ? bgImage.sprite.name : parentSpriteName;
                ApplyTextStyle(label, node, theme, tokenMap, options, true, buttonBgSpriteName);
                ConfigureTextLayout(label, node, textRect.sizeDelta);
            }

            return bgImage != null && bgImage.sprite != null ? bgImage.sprite.name : string.Empty;
        }

        private static string DeriveLabelName(string nodeName, string fallbackDefault = "Label")
        {
            var baseName = string.IsNullOrWhiteSpace(nodeName) ? fallbackDefault : nodeName.Trim();

            // 无论是 Btn 开头或者 Btn 结尾，Txt 都不需要带 Btn
            const string btnStr = "Btn";
            if (baseName.StartsWith(btnStr, StringComparison.OrdinalIgnoreCase) && baseName.Length > btnStr.Length)
            {
                baseName = baseName.Substring(btnStr.Length);
            }
            if (baseName.EndsWith(btnStr, StringComparison.OrdinalIgnoreCase) && baseName.Length > btnStr.Length)
            {
                baseName = baseName.Substring(0, baseName.Length - btnStr.Length);
            }

            baseName = baseName.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = fallbackDefault;
            }

            if (baseName.StartsWith("Txt", StringComparison.OrdinalIgnoreCase))
            {
                return baseName;
            }
            return "Txt" + baseName;
        }

        /// <summary>
        /// Derive the label child name from the button node name.
        /// "PlayAgainBtn" → "TxtPlayAgain", "BtnBack" → "TxtBack", "MyButton" → "TxtMyButton".
        /// </summary>
        private static string DeriveButtonLabelName(string nodeName)
        {
            return DeriveLabelName(nodeName, "Button");
        }

        private static void BuildInput(GameObject gameObject, RectTransform rectTransform, LiteCompiledNodeDto node, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap, AIToUGUILitePreviewBuildOptions options)
        {
            var input = gameObject.GetComponent<TMP_InputField>() ?? gameObject.AddComponent<TMP_InputField>();

            var viewport = CreateChildRect(rectTransform, "Viewport");
            Stretch(viewport, 8f, 8f);

            var textRect = CreateChildRect(viewport, "Text");
            Stretch(textRect, 0f, 0f);
            var textComponent = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyTextStyle(textComponent, node, theme, tokenMap, options, false);
            textComponent.text = ResolveDisplayText(node);
            input.textViewport = viewport;
            input.textComponent = textComponent;

            var placeholderRect = CreateChildRect(viewport, "Placeholder");
            Stretch(placeholderRect, 0f, 0f);
            var placeholder = placeholderRect.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyTextStyle(placeholder, node, theme, tokenMap, options, false);
            placeholder.color = new Color(placeholder.color.r, placeholder.color.g, placeholder.color.b, placeholder.color.a * 0.4f);
            placeholder.text = ResolveDisplayText(node);
            input.placeholder = placeholder;
        }

        private static void BuildToggle(GameObject gameObject, RectTransform rectTransform, LiteCompiledNodeDto node, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap, AIToUGUILitePreviewBuildOptions options)
        {
            var toggle = gameObject.GetComponent<Toggle>() ?? gameObject.AddComponent<Toggle>();

            var background = CreateFillImage(rectTransform, "Background", new Color(1f, 1f, 1f, 0.15f));
            background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            background.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            background.rectTransform.pivot = new Vector2(0f, 0.5f);
            background.rectTransform.sizeDelta = new Vector2(22f, 22f);
            background.rectTransform.anchoredPosition = new Vector2(0f, -11f);

            var checkmark = CreateFillImage(background.rectTransform, "Checkmark", ResolveAccentColor(theme, tokenMap));
            Stretch(checkmark.rectTransform, 4f, 4f);

            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            var labelName = DeriveLabelName(node != null ? node.name : null, "Toggle");
            CreateTextChild(rectTransform, labelName, ResolveDisplayText(node), node, theme, tokenMap, options, false, 30f, 0f);
        }

        private static void BuildSlider(GameObject gameObject, RectTransform rectTransform, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap)
        {
            var slider = gameObject.GetComponent<Slider>() ?? gameObject.AddComponent<Slider>();

            var track = CreateFillImage(rectTransform, "Track", new Color(1f, 1f, 1f, 0.12f));
            Stretch(track.rectTransform, 0f, 0f);

            var fill = CreateFillImage(rectTransform, "Fill", ResolveAccentColor(theme, tokenMap));
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0.65f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            slider.fillRect = fill.rectTransform;
        }

        private static void BuildScrollbar(GameObject gameObject, RectTransform rectTransform, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap)
        {
            var scrollbar = gameObject.GetComponent<Scrollbar>() ?? gameObject.AddComponent<Scrollbar>();

            var handle = CreateFillImage(rectTransform, "Handle", ResolveAccentColor(theme, tokenMap));
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0.25f, 1f);
            handle.rectTransform.offsetMin = Vector2.zero;
            handle.rectTransform.offsetMax = Vector2.zero;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
        }

        private static RectTransform BuildScroll(GameObject gameObject, RectTransform rectTransform, LiteCompiledNodeDto node, Dictionary<string, string> tokenMap)
        {
            var scrollRect = gameObject.GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();

            // Use RectMask2D instead of Mask for better performance and fewer draw-call side-effects.
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.SetParent(rectTransform, false);
            Stretch(viewportRect, 0f, 0f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

            var content = new GameObject("Content", typeof(RectTransform));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.sizeDelta = rectTransform.sizeDelta;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Infer scroll direction from flex-direction:
            //   flex-direction:row → horizontal scroll list
            //   flex-direction:column → vertical scroll list
            // When flex-direction is not set, default to vertical (most common for lists).
            var flexDirection = GetStyle(node, "flex-direction");
            bool isHorizontal;
            if (string.Equals(flexDirection, "row", StringComparison.OrdinalIgnoreCase))
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
                isHorizontal = true;
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                isHorizontal = false;
            }

            // Calculate template cell size from child template nodes.
            var cellSize = CalculateTemplateCellSize(node);

            // ── GridLayoutGroup ──
            var gridLayout = content.GetComponent<GridLayoutGroup>() ?? content.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = Vector2.zero;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = isHorizontal ? GridLayoutGroup.Axis.Vertical : GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            if (isHorizontal)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                gridLayout.constraintCount = 1;
            }
            else
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 1;
            }

            // ── ContentSizeFitter ──
            var sizeFitter = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
            if (isHorizontal)
            {
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return contentRect;
        }

        /// <summary>
        /// Calculate the maximum bounding box across all direct child template nodes.
        /// Each template's bounding box is computed by recursively scanning its entire subtree.
        /// The cell size is the max(width, height) of all templates' bounding boxes.
        /// </summary>
        private static Vector2 CalculateTemplateCellSize(LiteCompiledNodeDto node)
        {
            var children = node?.children ?? Array.Empty<LiteCompiledNodeDto>();
            float maxWidth = 100f;
            float maxHeight = 50f;

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null) continue;

                var bbox = CalculateNodeBoundingBox(child);
                maxWidth = Mathf.Max(maxWidth, bbox.x);
                maxHeight = Mathf.Max(maxHeight, bbox.y);
            }

            return new Vector2(maxWidth, maxHeight);
        }

        /// <summary>
        /// Recursively compute the bounding box of a node and all its descendants.
        /// The returned (maxRight, maxBottom) is in the NODE'S OWN local coordinate space
        /// (origin at the node's top-left). It does NOT include the node's own left/top
        /// offset — that is the caller's responsibility.
        ///
        /// For each node:
        ///   ownExtent = (width, height) if explicitly set in px, otherwise (0, 0)
        ///   For each child:
        ///     childOrigin = (child.left, child.top) in px
        ///     childBbox = CalculateNodeBoundingBox(child)  ← relative to child's origin
        ///     childRight  = childOrigin.x + childBbox.x
        ///     childBottom = childOrigin.y + childBbox.y
        ///   maxRight  = max(ownExtent.x, max of childRights)
        ///   maxBottom = max(ownExtent.y, max of childBottoms)
        /// </summary>
        private static Vector2 CalculateNodeBoundingBox(LiteCompiledNodeDto node)
        {
            if (node == null) return Vector2.zero;

            var ownWidth = ParseCssLength(GetStyle(node, "width"));
            var ownHeight = ParseCssLength(GetStyle(node, "height"));

            float ownW = ownWidth.Unit == LiteLengthUnit.Pixel ? ownWidth.Value : 0f;
            float ownH = ownHeight.Unit == LiteLengthUnit.Pixel ? ownHeight.Value : 0f;

            float maxRight = ownW;
            float maxBottom = ownH;

            var children = node.children ?? Array.Empty<LiteCompiledNodeDto>();
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null) continue;

                // childBbox is relative to child's own origin (top-left).
                var childBbox = CalculateNodeBoundingBox(child);
                var childLeft = ParseCssLength(GetStyle(child, "left"));
                var childTop = ParseCssLength(GetStyle(child, "top"));
                float childL = childLeft.Unit == LiteLengthUnit.Pixel ? childLeft.Value : 0f;
                float childT = childTop.Unit == LiteLengthUnit.Pixel ? childTop.Value : 0f;

                // Map child's bbox into this node's coordinate space.
                maxRight = Mathf.Max(maxRight, childL + childBbox.x);
                maxBottom = Mathf.Max(maxBottom, childT + childBbox.y);
            }

            return new Vector2(maxRight, maxBottom);
        }

        private static void BuildDropdown(GameObject gameObject, RectTransform rectTransform, LiteCompiledNodeDto node, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap, AIToUGUILitePreviewBuildOptions options)
        {
            var dropdown = gameObject.GetComponent<TMP_Dropdown>() ?? gameObject.AddComponent<TMP_Dropdown>();

            var labelName = DeriveLabelName(node != null ? node.name : null, "Dropdown");
            var labelRect = CreateChildRect(rectTransform, labelName);
            Stretch(labelRect, 12f, 32f, 12f, 12f);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyTextStyle(label, node, theme, tokenMap, options, false);
            label.text = ResolveDisplayText(node);
            dropdown.captionText = label;

            var arrowRect = CreateChildRect(rectTransform, "Arrow");
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(24f, 24f);
            arrowRect.anchoredPosition = new Vector2(-8f, -12f);
            var arrow = arrowRect.gameObject.AddComponent<TextMeshProUGUI>();
            arrow.text = "v";
            ApplyTextStyle(arrow, node, theme, tokenMap, options, true);

            dropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Option A"),
                new TMP_Dropdown.OptionData("Option B")
            };
        }

        private static void BuildProgress(GameObject gameObject, RectTransform rectTransform, LiteCompiledNodeDto node, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap)
        {
            var track = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            track.color = ResolveBackgroundColor(node, AIToUGUILiteControlType.Progress, tokenMap, theme, false);

            var fill = CreateFillImage(rectTransform, "Fill", ResolveAccentColor(theme, tokenMap));
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0.72f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
        }

        private static Color EnsureTextContrastWithSprite(Color rawColor, string parentSpriteName)
        {
            if (string.IsNullOrEmpty(parentSpriteName))
            {
                return rawColor;
            }

            var spriteLower = parentSpriteName.ToLowerInvariant();

            // 1. 如果背景是亮白切片 (ui_white) 或亮黄切片 (ui_yellow)
            if (spriteLower.Contains("ui_white") || spriteLower.Contains("ui_yellow"))
            {
                // 如果原始文字颜色偏亮 (RGB均值 > 0.55)，文字必定看不清！强制反转为高对比度的深墨色 (#1A2238)
                float luminance = (rawColor.r + rawColor.g + rawColor.b) / 3f;
                if (luminance > 0.55f)
                {
                    return new Color(0.10f, 0.13f, 0.22f, 1f); // 强高对比度深蓝墨色 #1A2238
                }
            }
            // 2. 如果背景是深色切片 (ui_dark) 或中灰切片 (ui_gray)
            else if (spriteLower.Contains("ui_dark") || spriteLower.Contains("ui_gray"))
            {
                // 如果原始文字颜色偏暗 (RGB均值 < 0.45)，强制翻转为纯白高亮字 (#FFFFFF)
                float luminance = (rawColor.r + rawColor.g + rawColor.b) / 3f;
                if (luminance < 0.45f)
                {
                    return Color.white;
                }
            }

            return rawColor;
        }

        private static void BuildStandaloneText(
            GameObject gameObject,
            RectTransform rectTransform,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            string parentSpriteName = "")
        {
            var label = EnsureStandaloneTextComponent(gameObject, rectTransform);
            if (label == null)
            {
                return;
            }

            label.text = ResolveDisplayText(node);
            ApplyTextStyle(label, node, theme, tokenMap, options, false, parentSpriteName);
            ConfigureTextLayout(label, node, rectTransform.rect.size.sqrMagnitude > 0f ? rectTransform.rect.size : rectTransform.sizeDelta);
        }

        private static TextMeshProUGUI EnsureStandaloneTextComponent(GameObject gameObject, RectTransform rectTransform)
        {
            if (gameObject == null || rectTransform == null)
            {
                return null;
            }

            var existing = gameObject.GetComponent<TextMeshProUGUI>();
            if (existing != null)
            {
                return existing;
            }

            var graphics = gameObject.GetComponents<Graphic>();
            for (var i = 0; i < graphics.Length; i++)
            {
                var graphic = graphics[i];
                if (graphic != null && !(graphic is TextMeshProUGUI))
                {
                    var labelName = DeriveLabelName(gameObject.name, "Label");
                    var childRect = CreateChildRect(rectTransform, labelName);
                    childRect.anchorMin = new Vector2(0.5f, 0.5f);
                    childRect.anchorMax = new Vector2(0.5f, 0.5f);
                    childRect.pivot = new Vector2(0.5f, 0.5f);
                    childRect.anchoredPosition = Vector2.zero;
                    var pSize = rectTransform.rect.size.sqrMagnitude > 0f ? rectTransform.rect.size : rectTransform.sizeDelta;
                    childRect.sizeDelta = pSize;
                    return childRect.gameObject.AddComponent<TextMeshProUGUI>();
                }
            }

            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI CreateTextChild(
            RectTransform parent,
            string name,
            string text,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            bool centered,
            float leftInset,
            float rightInset,
            string parentSpriteName = "")
        {
            var rectTransform = CreateChildRect(parent, name);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            var pSize = parent.rect.size.sqrMagnitude > 0f ? parent.rect.size : parent.sizeDelta;
            var textWidth = Mathf.Max(0f, pSize.x - (leftInset + rightInset));
            var textHeight = Mathf.Max(0f, pSize.y);
            rectTransform.sizeDelta = new Vector2(textWidth, textHeight);

            var label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text ?? string.Empty;
            ApplyTextStyle(label, node, theme, tokenMap, options, centered, parentSpriteName);
            ConfigureTextLayout(label, node, rectTransform.sizeDelta);
            return label;
        }

        private static void ApplyTextStyle(
            TMP_Text label,
            LiteCompiledNodeDto node,
            LiteCompiledThemeDto theme,
            Dictionary<string, string> tokenMap,
            AIToUGUILitePreviewBuildOptions options,
            bool centered,
            string parentSpriteName = "")
        {
            if (label == null)
            {
                return;
            }

            label.font = AIToUGUILiteFontUtility.ResolveFont(options != null ? options.defaultFontOverride : null, ResolveFontFamily(node));
            var rawColor = ResolveTextColor(node, theme, tokenMap);
            label.color = EnsureTextContrastWithSprite(rawColor, parentSpriteName);
            label.fontSize = Mathf.Max(12f, ParseFloat(GetStyle(node, "font-size"), ResolveDefaultFontSize(node)));
            label.enableWordWrapping = true;
            label.richText = false;
            label.raycastTarget = false;
            label.alignment = ResolveTextAlignment(node, centered);
            label.text = ApplyTextTransform(label.text, GetStyle(node, "text-transform"));
            ApplyTextStyling(label as TextMeshProUGUI, node);
        }

        private static TextAlignmentOptions ResolveTextAlignment(LiteCompiledNodeDto node, bool centered)
        {
            var controlType = ResolveControlType(node);
            var middleAligned = controlType == AIToUGUILiteControlType.Input ||
                                controlType == AIToUGUILiteControlType.Dropdown ||
                                controlType == AIToUGUILiteControlType.Toggle;

            if (centered || ShouldCenterAlignedText(node))
            {
                return TextAlignmentOptions.Center;
            }

            var align = FirstNonEmpty(GetStyle(node, "text-align"), GetStyle(node, "justify-content"), GetStyle(node, "align-items"));
            if (string.Equals(align, "center", StringComparison.OrdinalIgnoreCase))
            {
                return middleAligned ? TextAlignmentOptions.Center : TextAlignmentOptions.Top;
            }

            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase) || string.Equals(align, "end", StringComparison.OrdinalIgnoreCase))
            {
                return middleAligned ? TextAlignmentOptions.Right : TextAlignmentOptions.TopRight;
            }

            return middleAligned ? TextAlignmentOptions.Left : TextAlignmentOptions.TopLeft;
        }

        private static bool ShouldCenterAlignedText(LiteCompiledNodeDto node)
        {
            if (node == null)
            {
                return false;
            }

            if (ResolveControlType(node) == AIToUGUILiteControlType.Button ||
                string.Equals(node.tag, "button", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(node.role) &&
                (node.role.StartsWith("button/", StringComparison.OrdinalIgnoreCase) ||
                 node.role.StartsWith("chip/", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return HasClass(node, "slab-button") ||
                   HasClass(node, "small-button") ||
                   HasClass(node, "chip");
        }

        private static void ConfigureTextLayout(TMP_Text label, LiteCompiledNodeDto node, Vector2 availableSize)
        {
            if (label == null || node == null)
            {
                return;
            }

            var baseFontSize = Mathf.Max(12f, ParseFloat(GetStyle(node, "font-size"), label.fontSize > 0f ? label.fontSize : 24f));
            var singleLine = ShouldForceSingleLine(node);
            var wrapping = !singleLine && ShouldWrapText(node);
            var maxLines = ResolveMaxVisibleLines(node, availableSize, baseFontSize, wrapping);

            label.enableWordWrapping = wrapping;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maxVisibleLines = maxLines > 0 ? maxLines : UnlimitedVisibleLines;
            label.enableAutoSizing = singleLine;
            if (label.enableAutoSizing)
            {
                label.fontSizeMax = baseFontSize;
                label.fontSizeMin = Mathf.Max(10f, Mathf.Min(baseFontSize, baseFontSize * 0.72f));
            }
            else
            {
                label.fontSizeMin = baseFontSize;
                label.fontSizeMax = baseFontSize;
            }
        }

        private static float ClampMeasuredTextHeight(LiteCompiledNodeDto node, float fontSize, float preferredHeight, float heightConstraint)
        {
            var height = preferredHeight;
            var maxLines = ResolveMaxVisibleLines(node, new Vector2(0f, heightConstraint), fontSize, ShouldWrapText(node));
            if (maxLines > 0)
            {
                height = Mathf.Min(height, ResolveTextLineHeightPixels(node, fontSize) * maxLines);
            }

            if (heightConstraint > 0f)
            {
                height = Mathf.Min(height, heightConstraint);
            }

            return Mathf.Max(0f, height);
        }

        private static bool ShouldForceSingleLine(LiteCompiledNodeDto node)
        {
            if (node == null)
            {
                return false;
            }

            var controlType = ResolveControlType(node);
            if (controlType == AIToUGUILiteControlType.Button ||
                string.Equals(node.tag, "button", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(node.role) && node.role.StartsWith("button/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return HasClass(node, "headline") ||
                   HasClass(node, "section-title") ||
                   HasClass(node, "micro-label") ||
                   HasClass(node, "stat-number") ||
                   HasClass(node, "chip") ||
                   HasClass(node, "small-button") ||
                   HasClass(node, "slab-button");
        }

        private static bool ShouldWrapText(LiteCompiledNodeDto node)
        {
            if (node == null)
            {
                return false;
            }

            if (ShouldForceSingleLine(node))
            {
                return false;
            }

            if (HasClass(node, "body-text"))
            {
                return true;
            }

            return !string.Equals(node.tag, "span", StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveMaxVisibleLines(LiteCompiledNodeDto node, Vector2 availableSize, float fontSize, bool wrapping)
        {
            if (node == null)
            {
                return UnlimitedVisibleLines;
            }

            if (ShouldForceSingleLine(node))
            {
                return 1;
            }

            if (!wrapping)
            {
                return UnlimitedVisibleLines;
            }

            if (HasClass(node, "body-text"))
            {
                var cappedLines = 4;
                if (availableSize.y > 0f)
                {
                    var lineHeight = ResolveTextLineHeightPixels(node, fontSize);
                    if (lineHeight > 0f)
                    {
                        cappedLines = Mathf.Min(cappedLines, Mathf.Max(1, Mathf.FloorToInt((availableSize.y + 0.5f) / lineHeight)));
                    }
                }

                return Mathf.Max(1, cappedLines);
            }

            return UnlimitedVisibleLines;
        }

        private static float ResolveTextLineHeightPixels(LiteCompiledNodeDto node, float fontSize)
        {
            var lineHeight = GetStyle(node, "line-height");
            if (string.IsNullOrWhiteSpace(lineHeight))
            {
                return fontSize * 1.2f;
            }

            var normalized = lineHeight.Trim().ToLowerInvariant();
            if (normalized.EndsWith("px", StringComparison.Ordinal))
            {
                return Mathf.Max(0f, ParseFloat(normalized, fontSize));
            }

            if (normalized.EndsWith("%", StringComparison.Ordinal))
            {
                return Mathf.Max(0f, fontSize * ParseFloat(normalized, 100f) * 0.01f);
            }

            var value = ParseFloat(normalized, 1.2f);
            return value <= 10f ? Mathf.Max(0f, fontSize * value) : Mathf.Max(0f, value);
        }

        private static void ApplyTextStyling(TextMeshProUGUI text, LiteCompiledNodeDto node)
        {
            if (text == null || node == null)
            {
                return;
            }

            text.characterSpacing = ParseFloat(GetStyle(node, "letter-spacing"), 0f);
            var lineHeight = GetStyle(node, "line-height");
            if (!string.IsNullOrWhiteSpace(lineHeight))
            {
                var targetLineHeight = ResolveTextLineHeightPixels(node, text.fontSize);
                text.lineSpacing = Mathf.Max(-text.fontSize, targetLineHeight - text.fontSize);
            }

            var fontWeight = ParseFloat(GetStyle(node, "font-weight"), 400f);
            if (fontWeight >= 700f)
            {
                text.fontStyle |= FontStyles.Bold;
            }
        }

        private static bool HasClass(LiteCompiledNodeDto node, string className)
        {
            if (node == null || node.classes == null || string.IsNullOrWhiteSpace(className))
            {
                return false;
            }

            for (var i = 0; i < node.classes.Length; i++)
            {
                if (string.Equals(node.classes[i], className, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ResolveNodeSize(LiteCompiledNodeDto node, Vector2 parentSize, Vector2 defaultSize)
        {
            var width = ParseCssLength(GetStyle(node, "width"));
            var height = ParseCssLength(GetStyle(node, "height"));
            var minWidth = ParseCssLength(GetStyle(node, "min-width"));
            var minHeight = ParseCssLength(GetStyle(node, "min-height"));
            var maxWidth = ParseCssLength(GetStyle(node, "max-width"));
            var maxHeight = ParseCssLength(GetStyle(node, "max-height"));

            var resolvedWidth = ResolveCssLength(width, parentSize.x, defaultSize.x);
            var resolvedHeight = ResolveCssLength(height, parentSize.y, defaultSize.y);

            if (!width.IsValid && maxWidth.IsValid)
            {
                resolvedWidth = Mathf.Min(parentSize.x, ResolveCssLength(maxWidth, parentSize.x, parentSize.x));
            }

            if (!height.IsValid && maxHeight.IsValid)
            {
                resolvedHeight = Mathf.Min(parentSize.y, ResolveCssLength(maxHeight, parentSize.y, parentSize.y));
            }

            resolvedWidth = ClampLength(resolvedWidth, minWidth, maxWidth, parentSize.x);
            resolvedHeight = ClampLength(resolvedHeight, minHeight, maxHeight, parentSize.y);
            return new Vector2(resolvedWidth, resolvedHeight);
        }

        private static float ClampLength(float value, LiteCssLength min, LiteCssLength max, float parentLength)
        {
            if (value < 0f && !min.IsValid)
            {
                return value;
            }

            if (min.IsValid)
            {
                value = Mathf.Max(value, ResolveCssLength(min, parentLength, value));
            }

            if (max.IsValid)
            {
                value = Mathf.Min(value, ResolveCssLength(max, parentLength, value));
            }

            return value;
        }

        private static LiteCssLength ParseCssLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new LiteCssLength(0f, LiteLengthUnit.None);
            }

            var normalized = value.Trim().ToLowerInvariant();
            if (normalized == "auto")
            {
                return new LiteCssLength(0f, LiteLengthUnit.Auto);
            }

            if (normalized.EndsWith("%", StringComparison.Ordinal))
            {
                var rawPercent = normalized.Substring(0, normalized.Length - 1);
                if (float.TryParse(rawPercent, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
                {
                    return new LiteCssLength(percent, LiteLengthUnit.Percent);
                }
            }

            var number = ParseFloat(normalized, float.NaN);
            if (float.IsNaN(number))
            {
                return new LiteCssLength(0f, LiteLengthUnit.None);
            }

            return new LiteCssLength(number, LiteLengthUnit.Pixel);
        }

        private static float ResolveCssLength(LiteCssLength length, float parentLength, float fallback)
        {
            switch (length.Unit)
            {
                case LiteLengthUnit.Pixel:
                    return length.Value;
                case LiteLengthUnit.Percent:
                    return parentLength * length.Value * 0.01f;
                case LiteLengthUnit.Auto:
                    return fallback;
                default:
                    return fallback;
            }
        }

        private static RectOffset ParseBox(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new RectOffset();
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return new RectOffset();
            }

            var values = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                values[i] = Mathf.RoundToInt(ParseFloat(parts[i], 0f));
            }

            switch (values.Length)
            {
                case 1:
                    return new RectOffset(values[0], values[0], values[0], values[0]);
                case 2:
                    return new RectOffset(values[1], values[1], values[0], values[0]);
                case 3:
                    return new RectOffset(values[1], values[1], values[0], values[2]);
                default:
                    return new RectOffset(values[3], values[1], values[0], values[2]);
            }
        }

        private static LiteCssEdges ParseMargins(LiteCompiledNodeDto node)
        {
            return ParseEdges(
                GetStyle(node, "margin"),
                GetStyle(node, "margin-left"),
                GetStyle(node, "margin-right"),
                GetStyle(node, "margin-top"),
                GetStyle(node, "margin-bottom"));
        }

        private static LiteCssEdges ParseEdges(string shorthand, string leftOverride, string rightOverride, string topOverride, string bottomOverride)
        {
            var left = new LiteCssLength(0f, LiteLengthUnit.None);
            var right = new LiteCssLength(0f, LiteLengthUnit.None);
            var top = new LiteCssLength(0f, LiteLengthUnit.None);
            var bottom = new LiteCssLength(0f, LiteLengthUnit.None);

            if (!string.IsNullOrWhiteSpace(shorthand))
            {
                var parts = shorthand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var values = new LiteCssLength[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    values[i] = ParseCssLength(parts[i]);
                }

                switch (values.Length)
                {
                    case 1:
                        top = right = bottom = left = values[0];
                        break;
                    case 2:
                        top = bottom = values[0];
                        right = left = values[1];
                        break;
                    case 3:
                        top = values[0];
                        right = left = values[1];
                        bottom = values[2];
                        break;
                    default:
                        top = values[0];
                        right = values[1];
                        bottom = values[2];
                        left = values[3];
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(leftOverride))
            {
                left = ParseCssLength(leftOverride);
            }

            if (!string.IsNullOrWhiteSpace(rightOverride))
            {
                right = ParseCssLength(rightOverride);
            }

            if (!string.IsNullOrWhiteSpace(topOverride))
            {
                top = ParseCssLength(topOverride);
            }

            if (!string.IsNullOrWhiteSpace(bottomOverride))
            {
                bottom = ParseCssLength(bottomOverride);
            }

            return new LiteCssEdges(left, right, top, bottom);
        }

        private static float ResolveEdge(LiteCssLength edge, float parentLength)
        {
            return edge.IsAuto ? 0f : ResolveCssLength(edge, parentLength, 0f);
        }

        private static bool HasExplicitWidth(LiteCompiledNodeDto node)
        {
            return ParseCssLength(GetStyle(node, "width")).IsValid;
        }

        private static bool IsAbsolutePosition(LiteCompiledNodeDto node)
        {
            var position = GetStyle(node, "position");
            return string.Equals(position, "absolute", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(position, "fixed", StringComparison.OrdinalIgnoreCase);
        }

        private static TextMeshProUGUI FindPrimaryText(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            if (gameObject.TryGetComponent<TextMeshProUGUI>(out var directText))
            {
                return directText;
            }

            var expectedName = DeriveLabelName(gameObject.name, "Label");
            var label = gameObject.transform.Find(expectedName);
            if (label != null && label.TryGetComponent<TextMeshProUGUI>(out var labelText))
            {
                return labelText;
            }

            var legacyLabel = gameObject.transform.Find(InternalLabelName);
            if (legacyLabel != null && legacyLabel.TryGetComponent<TextMeshProUGUI>(out var legacyText))
            {
                return legacyText;
            }

            return gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        /// <summary>
        /// Returns true when <paramref name="text"/> belongs to one of the node's real
        /// child nodes (i.e. it is, or is nested under, a bundle child element). Such text
        /// must not be treated as the parent's own label, otherwise RefreshTextComponents
        /// would resize/restyle the child using the parent's bounds and node styles.
        /// </summary>
        private static bool TextBelongsToChildNode(BuiltLiteNode built, TMP_Text text)
        {
            if (built == null || text == null || built.children == null || built.children.Count == 0)
            {
                return false;
            }

            var textTransform = text.transform;
            for (var i = 0; i < built.children.Count; i++)
            {
                var child = built.children[i];
                if (child == null || child.rectTransform == null)
                {
                    continue;
                }

                if (textTransform == child.rectTransform || textTransform.IsChildOf(child.rectTransform))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ResolveDefaultLeafSize(AIToUGUILiteControlType controlType)
        {
            switch (controlType)
            {
                case AIToUGUILiteControlType.Toggle:
                    return DefaultToggleSize;
                case AIToUGUILiteControlType.Slider:
                case AIToUGUILiteControlType.Scrollbar:
                case AIToUGUILiteControlType.Progress:
                case AIToUGUILiteControlType.Button:
                case AIToUGUILiteControlType.Input:
                case AIToUGUILiteControlType.Dropdown:
                    return DefaultElementSize;
                case AIToUGUILiteControlType.Div:
                case AIToUGUILiteControlType.Image:
                    return Vector2.zero;
                default:
                    return DefaultElementSize;
            }
        }

        private static string ResolvePageName(LiteCompiledPageDto page)
        {
            if (page == null)
            {
                return "Page";
            }

            if (!string.IsNullOrWhiteSpace(page.displayName))
            {
                return page.displayName;
            }

            if (!string.IsNullOrWhiteSpace(page.pageId))
            {
                return page.pageId;
            }

            return "Page";
        }

        private static string ResolveDisplayText(LiteCompiledNodeDto node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(node.text))
            {
                return node.text;
            }

            return FirstNonEmpty(GetAttribute(node, "data-ui-value"), GetAttribute(node, "value"), string.Empty);
        }

        private static string ResolveFontFamily(LiteCompiledNodeDto node)
        {
            return GetStyle(node, "font-family");
        }

        private static Color ResolveTextColor(LiteCompiledNodeDto node, LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap)
        {
            var raw = FirstNonEmpty(
                GetStyle(node, "color"),
                theme != null ? theme.textPrimary : null,
                "#ffffff");
            return TryResolveColor(raw, tokenMap, out var color) ? color : Color.white;
        }

        private static Color ResolveBackgroundColor(LiteCompiledNodeDto node, AIToUGUILiteControlType controlType, Dictionary<string, string> tokenMap, LiteCompiledThemeDto theme, bool isRoot)
        {
            var raw = ResolveBackgroundColorValue(node);
            if (TryResolveColor(raw, tokenMap, out var color))
            {
                color.a *= ResolveOpacity(node);
                return color;
            }

            string fallback;
            if (controlType == AIToUGUILiteControlType.Button)
            {
                fallback = FirstNonEmpty(theme != null ? theme.buttonFill : null, theme != null ? theme.accentColor : null);
            }
            else if (controlType == AIToUGUILiteControlType.Progress)
            {
                fallback = "rgba(255,255,255,0.12)";
            }
            else if (controlType == AIToUGUILiteControlType.Input || controlType == AIToUGUILiteControlType.Dropdown)
            {
                fallback = FirstNonEmpty(theme != null ? theme.cardFill : null, theme != null ? theme.panelFill : null);
            }
            else
            {
                fallback = isRoot
                    ? FirstNonEmpty(theme != null ? theme.pageBackground : null, theme != null ? theme.panelFill : null)
                    : theme != null ? theme.panelFill : "#202020";
            }

            if (TryResolveColor(fallback, tokenMap, out color))
            {
                return color;
            }

            return new Color(0.13f, 0.16f, 0.2f, 0.96f);
        }

        private static string ResolveBackgroundColorValue(LiteCompiledNodeDto node)
        {
            return FirstNonEmpty(
                node != null && node.visual != null ? node.visual.fillColor : null,
                node != null && node.visual != null ? node.visual.backgroundColor : null,
                node != null && node.visual != null ? node.visual.background : null,
                GetInlineStyle(node, "background-color"),
                GetInlineStyle(node, "background"));
        }

        private static float ResolveOpacity(LiteCompiledNodeDto node)
        {
            return Mathf.Clamp01(ParseFloat(GetStyle(node, "opacity"), 1f));
        }

        private static float ResolveDefaultFontSize(LiteCompiledNodeDto node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.role))
            {
                return 20f;
            }

            if (node.role.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 32f;
            }

            if (node.role.IndexOf("headline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 28f;
            }

            return 20f;
        }

        private static Color ResolveAccentColor(LiteCompiledThemeDto theme, Dictionary<string, string> tokenMap)
        {
            return TryResolveColor(theme != null ? theme.accentColor : "#f0c550", tokenMap, out var color)
                ? color
                : new Color(0.92f, 0.77f, 0.34f, 1f);
        }

        private static AIToUGUILiteControlType ResolveControlType(LiteCompiledNodeDto node)
        {
            if (node == null)
            {
                return AIToUGUILiteControlType.Div;
            }

            if (Enum.TryParse(node.controlType, true, out AIToUGUILiteControlType controlType))
            {
                if (controlType == AIToUGUILiteControlType.Auto)
                {
                    return InferControlType(node);
                }

                return controlType;
            }

            return InferControlType(node);
        }

        private static AIToUGUILiteControlType InferControlType(LiteCompiledNodeDto node)
        {
            var tag = string.IsNullOrWhiteSpace(node.tag) ? "div" : node.tag.ToLowerInvariant();
            switch (tag)
            {
                case "button":
                    return AIToUGUILiteControlType.Button;
                case "input":
                case "textarea":
                    return AIToUGUILiteControlType.Input;
                case "scroll":
                    return AIToUGUILiteControlType.Scroll;
                case "scrollbar":
                    return AIToUGUILiteControlType.Scrollbar;
                case "toggle":
                    return AIToUGUILiteControlType.Toggle;
                case "slider":
                    return AIToUGUILiteControlType.Slider;
                case "dropdown":
                    return AIToUGUILiteControlType.Dropdown;
                case "image":
                case "img":
                    return AIToUGUILiteControlType.Image;
                case "progress":
                    return AIToUGUILiteControlType.Progress;
            }

            return !string.IsNullOrWhiteSpace(ResolveDisplayText(node)) && (node.children == null || node.children.Length == 0)
                ? AIToUGUILiteControlType.Text
                : AIToUGUILiteControlType.Div;
        }

        private static RectTransform CreateChildRect(RectTransform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform, float leftInset, float rightInset)
        {
            Stretch(rectTransform, leftInset, rightInset, leftInset, rightInset);
        }

        private static void Stretch(RectTransform rectTransform, float leftInset, float rightInset, float topInset, float bottomInset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(leftInset, bottomInset);
            rectTransform.offsetMax = new Vector2(-rightInset, -topInset);
        }

        private static Image CreateFillImage(RectTransform parent, string name, Color color, LiteCompiledNodeDto node = null, string parentSpriteName = "", Vector2 myGlobalPos = default)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rectTransform = child.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            var image = child.GetComponent<Image>();
            
            // 应用 ui_color 子 Sprite 充填，绝对互斥避让，保持 Color.white
            ApplyColorAtlasSprite(image, node, AIToUGUILiteControlType.Progress, color, parentSpriteName, myGlobalPos);
            return image;
        }

        private static string GetStyle(LiteCompiledNodeDto node, string key)
        {
            if (node == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            switch (key)
            {
                case "display":
                    return FirstNonEmpty(node.layout != null ? node.layout.display : null, GetInlineStyle(node, key));
                case "position":
                    return FirstNonEmpty(node.layout != null ? node.layout.position : null, GetInlineStyle(node, key));
                case "left":
                    return FirstNonEmpty(node.layout != null ? node.layout.left : null, GetInlineStyle(node, key));
                case "right":
                    return FirstNonEmpty(node.layout != null ? node.layout.right : null, GetInlineStyle(node, key));
                case "top":
                    return FirstNonEmpty(node.layout != null ? node.layout.top : null, GetInlineStyle(node, key));
                case "bottom":
                    return FirstNonEmpty(node.layout != null ? node.layout.bottom : null, GetInlineStyle(node, key));
                case "width":
                    return FirstNonEmpty(node.layout != null ? node.layout.width : null, GetInlineStyle(node, key));
                case "height":
                    return FirstNonEmpty(node.layout != null ? node.layout.height : null, GetInlineStyle(node, key));
                case "min-width":
                    return FirstNonEmpty(node.layout != null ? node.layout.minWidth : null, GetInlineStyle(node, key));
                case "max-width":
                    return FirstNonEmpty(node.layout != null ? node.layout.maxWidth : null, GetInlineStyle(node, key));
                case "min-height":
                    return FirstNonEmpty(node.layout != null ? node.layout.minHeight : null, GetInlineStyle(node, key));
                case "max-height":
                    return FirstNonEmpty(node.layout != null ? node.layout.maxHeight : null, GetInlineStyle(node, key));
                case "padding":
                    return FirstNonEmpty(node.layout != null ? node.layout.padding : null, GetInlineStyle(node, key));
                case "margin":
                    return FirstNonEmpty(node.layout != null ? node.layout.margin : null, GetInlineStyle(node, key));
                case "margin-left":
                    return FirstNonEmpty(node.layout != null ? node.layout.marginLeft : null, GetInlineStyle(node, key));
                case "margin-right":
                    return FirstNonEmpty(node.layout != null ? node.layout.marginRight : null, GetInlineStyle(node, key));
                case "margin-top":
                    return FirstNonEmpty(node.layout != null ? node.layout.marginTop : null, GetInlineStyle(node, key));
                case "margin-bottom":
                    return FirstNonEmpty(node.layout != null ? node.layout.marginBottom : null, GetInlineStyle(node, key));
                case "gap":
                    return FirstNonEmpty(node.layout != null ? node.layout.gap : null, GetInlineStyle(node, key));
                case "justify-content":
                    return FirstNonEmpty(node.layout != null ? node.layout.justifyContent : null, GetInlineStyle(node, key));
                case "align-items":
                    return FirstNonEmpty(node.layout != null ? node.layout.alignItems : null, GetInlineStyle(node, key));
                case "flex-direction":
                    return FirstNonEmpty(node.layout != null ? node.layout.flexDirection : null, GetInlineStyle(node, key));
                case "overflow":
                    return FirstNonEmpty(node.layout != null ? node.layout.overflow : null, GetInlineStyle(node, key));
                case "overflow-x":
                    return FirstNonEmpty(node.layout != null ? node.layout.overflowX : null, GetInlineStyle(node, key));
                case "overflow-y":
                    return FirstNonEmpty(node.layout != null ? node.layout.overflowY : null, GetInlineStyle(node, key));
                case "box-sizing":
                    return FirstNonEmpty(node.layout != null ? node.layout.boxSizing : null, GetInlineStyle(node, key));
                case "background":
                    return FirstNonEmpty(node.visual != null ? node.visual.background : null, GetInlineStyle(node, key));
                case "background-color":
                    return FirstNonEmpty(node.visual != null ? node.visual.backgroundColor : null, GetInlineStyle(node, key));
                case "opacity":
                    return FirstNonEmpty(node.visual != null ? node.visual.opacity : null, GetInlineStyle(node, key));
                case "color":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.color : null, GetInlineStyle(node, key));
                case "font-size":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.fontSize : null, GetInlineStyle(node, key));
                case "font-family":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.fontFamily : null, GetInlineStyle(node, key));
                case "font-weight":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.fontWeight : null, GetInlineStyle(node, key));
                case "line-height":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.lineHeight : null, GetInlineStyle(node, key));
                case "text-align":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.textAlign : null, GetInlineStyle(node, key));
                case "letter-spacing":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.letterSpacing : null, GetInlineStyle(node, key));
                case "text-transform":
                    return FirstNonEmpty(node.textStyle != null ? node.textStyle.textTransform : null, GetInlineStyle(node, key));
                default:
                    return GetInlineStyle(node, key);
            }
        }

        private static string GetAttribute(LiteCompiledNodeDto node, string name)
        {
            if (node == null || node.attributes == null || string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            for (var i = 0; i < node.attributes.Length; i++)
            {
                var attribute = node.attributes[i];
                if (attribute != null && string.Equals(attribute.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.value ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string GetInlineStyle(LiteCompiledNodeDto node, string propertyName)
        {
            var style = GetAttribute(node, "style");
            if (string.IsNullOrWhiteSpace(style) || string.IsNullOrWhiteSpace(propertyName))
            {
                return string.Empty;
            }

            var parts = style.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var separatorIndex = part.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = part.Substring(0, separatorIndex).Trim();
                if (!string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return part.Substring(separatorIndex + 1).Trim();
            }

            return string.Empty;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }

        private static float ParseFloat(string raw, float fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            var match = NumberRegex.Match(raw);
            if (!match.Success)
            {
                return fallback;
            }

            return float.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        private static bool TryResolveColor(string raw, Dictionary<string, string> tokenMap, out Color color)
        {
            color = Color.clear;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            raw = ResolveTokenValue(raw.Trim(), tokenMap);
            if (string.Equals(raw, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                color = Color.clear;
                return true;
            }

            if (ColorUtility.TryParseHtmlString(raw, out color))
            {
                return true;
            }

            var rgbaMatch = RgbaRegex.Match(raw);
            if (rgbaMatch.Success)
            {
                color = new Color(
                    Mathf.Clamp01(ParseFloat(rgbaMatch.Groups["r"].Value, 0f) / 255f),
                    Mathf.Clamp01(ParseFloat(rgbaMatch.Groups["g"].Value, 0f) / 255f),
                    Mathf.Clamp01(ParseFloat(rgbaMatch.Groups["b"].Value, 0f) / 255f),
                    rgbaMatch.Groups["a"].Success ? Mathf.Clamp01(ParseFloat(rgbaMatch.Groups["a"].Value, 1f)) : 1f);
                return true;
            }

            var match = ColorRegex.Match(raw);
            if (match.Success && !string.Equals(match.Value, raw, StringComparison.OrdinalIgnoreCase))
            {
                return TryResolveColor(match.Value, tokenMap, out color);
            }

            return false;
        }

        private static string ResolveTokenValue(string raw, Dictionary<string, string> tokenMap)
        {
            if (string.IsNullOrWhiteSpace(raw) || tokenMap == null || tokenMap.Count == 0)
            {
                return raw;
            }

            var match = VarRegex.Match(raw);
            if (match.Success)
            {
                var tokenId = match.Groups["token"].Value;
                if (tokenMap.TryGetValue(tokenId, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return raw;
        }

        private static Dictionary<string, string> BuildTokenMap(LiteCompiledThemeDto theme)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (theme == null || theme.tokens == null)
            {
                return map;
            }

            for (var i = 0; i < theme.tokens.Length; i++)
            {
                var token = theme.tokens[i];
                if (token == null || string.IsNullOrWhiteSpace(token.tokenId))
                {
                    continue;
                }

                map[token.tokenId] = token.value ?? string.Empty;
            }

            return map;
        }

        private static string ApplyTextTransform(string text, string transform)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(transform))
            {
                return text;
            }

            if (string.Equals(transform, "uppercase", StringComparison.OrdinalIgnoreCase))
            {
                return text.ToUpperInvariant();
            }

            if (string.Equals(transform, "lowercase", StringComparison.OrdinalIgnoreCase))
            {
                return text.ToLowerInvariant();
            }

            return text;
        }

        private static string SanitizeName(string preferred, string fallback)
        {
            var raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "Node";
            }

            return raw.Replace("/", "_").Replace("\\", "_").Trim();
        }

        private static void MarkSceneDirty(GameObject mountRoot)
        {
            if (mountRoot != null && mountRoot.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(mountRoot.scene);
            }
        }

        #region UI Color Atlas & Spatial AABB Overlap Avoidance Engine

        private struct SpatialSpriteRecord
        {
            public Rect globalRect;
            public string spriteName;
        }

        private static readonly List<SpatialSpriteRecord> s_SpatialRecords = new List<SpatialSpriteRecord>();
        private static readonly Dictionary<string, Sprite> s_ColorAtlasSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static bool s_ColorAtlasLoaded;

        private static void ResetSpatialRecords()
        {
            s_SpatialRecords.Clear();
        }

        private static Rect ComputeGlobalCanvasRect(LiteCompiledNodeDto node, Vector2 myGlobalPos, Vector2 parentSize)
        {
            if (node == null || node.layout == null) return new Rect(myGlobalPos.x, myGlobalPos.y, parentSize.x > 0 ? parentSize.x : 100f, parentSize.y > 0 ? parentSize.y : 100f);

            var l = node.layout;
            float rawW = ParseFloat(l.width, 0f);
            float rawH = ParseFloat(l.height, 0f);

            float w = rawW > 0f ? rawW : (parentSize.x > 0f ? parentSize.x : 100f);
            float h = rawH > 0f ? rawH : (parentSize.y > 0f ? parentSize.y : 100f);

            return new Rect(myGlobalPos.x, myGlobalPos.y, w, h);
        }

        private static HashSet<string> GetOccupiedSpatialSprites(Rect targetRect)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (targetRect.width <= 2f || targetRect.height <= 2f) return set;

            for (int i = 0; i < s_SpatialRecords.Count; i++)
            {
                var record = s_SpatialRecords[i];
                if (string.IsNullOrEmpty(record.spriteName)) continue;

                float minX = Mathf.Max(targetRect.xMin, record.globalRect.xMin);
                float maxX = Mathf.Min(targetRect.xMax, record.globalRect.xMax);
                float minY = Mathf.Max(targetRect.yMin, record.globalRect.yMin);
                float maxY = Mathf.Min(targetRect.yMax, record.globalRect.yMax);

                if (maxX - minX > 3f && maxY - minY > 3f)
                {
                    set.Add(record.spriteName);
                }
            }

            return set;
        }

        private static bool IsStructuralUiElement(LiteCompiledNodeDto node)
        {
            if (node == null) return false;
            var nameLower = (node.name ?? string.Empty).ToLowerInvariant();
            var roleLower = (node.role ?? string.Empty).ToLowerInvariant();
            var classStr = node.classes != null ? string.Join(" ", node.classes).ToLowerInvariant() : string.Empty;

            return nameLower.Contains("slot") || nameLower.Contains("card") || nameLower.Contains("item") ||
                   nameLower.Contains("box") || nameLower.Contains("cell") || nameLower.Contains("equip") ||
                   nameLower.Contains("rune") || nameLower.Contains("option") || nameLower.Contains("choice") ||
                   nameLower.Contains("select") || nameLower.Contains("avatar") || nameLower.Contains("portrait") ||
                   classStr.Contains("card") || classStr.Contains("slot") || classStr.Contains("item") ||
                   nameLower.Contains("hero") || nameLower.Contains("skin") || nameLower.Contains("panel");
        }

        private static void EnsureColorAtlasLoaded()
        {
            if (s_ColorAtlasLoaded) return;
            s_ColorAtlasLoaded = true;

            const string atlasPath = "Assets/AIToUGUI_LITE/Editor/UIElement/ui_color.png";
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(atlasPath);
            if (allAssets != null)
            {
                for (var i = 0; i < allAssets.Length; i++)
                {
                    if (allAssets[i] is Sprite sprite && !string.IsNullOrEmpty(sprite.name))
                    {
                        s_ColorAtlasSpriteCache[sprite.name] = sprite;
                    }
                }
            }
        }

        private static Sprite ApplyColorAtlasSprite(
            Image image,
            LiteCompiledNodeDto node,
            AIToUGUILiteControlType controlType,
            Color parsedColor,
            string parentSpriteName,
            Vector2 myGlobalPos = default,
            Vector2 parentSize = default)
        {
            if (image == null) return null;

            EnsureColorAtlasLoaded();
            if (s_ColorAtlasSpriteCache.Count == 0)
            {
                image.color = Color.white;
                return null;
            }

            var globalRect = ComputeGlobalCanvasRect(node, myGlobalPos, parentSize);
            var occupiedSprites = GetOccupiedSpatialSprites(globalRect);

            var targetSubSpriteName = MatchSubSpriteName(node, controlType, parsedColor, parentSpriteName, occupiedSprites);

            if (s_ColorAtlasSpriteCache.TryGetValue(targetSubSpriteName, out var sprite) && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;

                if (globalRect.width > 2f && globalRect.height > 2f)
                {
                    s_SpatialRecords.Add(new SpatialSpriteRecord { globalRect = globalRect, spriteName = sprite.name });
                }

                return sprite;
            }

            if (s_ColorAtlasSpriteCache.TryGetValue("ui_white", out var whiteSprite) && whiteSprite != null)
            {
                image.sprite = whiteSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;

                if (globalRect.width > 2f && globalRect.height > 2f)
                {
                    s_SpatialRecords.Add(new SpatialSpriteRecord { globalRect = globalRect, spriteName = whiteSprite.name });
                }

                return whiteSprite;
            }

            image.color = Color.white;
            return null;
        }

        private static string MatchSubSpriteName(
            LiteCompiledNodeDto node,
            AIToUGUILiteControlType controlType,
            Color c,
            string parentSpriteName,
            HashSet<string> occupiedSprites)
        {
            var rawMatched = DeriveRawSpriteName(node, controlType, c);

            var forbiddenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(parentSpriteName))
            {
                forbiddenSet.Add(parentSpriteName);
            }
            if (occupiedSprites != null)
            {
                foreach (var s in occupiedSprites)
                {
                    if (!string.IsNullOrEmpty(s)) forbiddenSet.Add(s);
                }
            }

            if (forbiddenSet.Contains(rawMatched))
            {
                if (IsStructuralUiElement(node))
                {
                    string[] structuralCandidates = new string[] { "ui_white", "ui_gray", "ui_purple", "ui_dark", "ui_blue", "ui_yellow" };
                    for (int i = 0; i < structuralCandidates.Length; i++)
                    {
                        var cand = structuralCandidates[i];
                        if (!forbiddenSet.Contains(cand))
                        {
                            return cand;
                        }
                    }
                }

                string[] generalCandidates = new string[] { "ui_yellow", "ui_blue", "ui_green", "ui_dark", "ui_gray", "ui_white", "ui_purple", "ui_red" };
                for (int i = 0; i < generalCandidates.Length; i++)
                {
                    var cand = generalCandidates[i];
                    if (!forbiddenSet.Contains(cand))
                    {
                        return cand;
                    }
                }

                if (string.Equals(parentSpriteName, "ui_white", StringComparison.OrdinalIgnoreCase)) return "ui_dark";
                if (string.Equals(parentSpriteName, "ui_gray", StringComparison.OrdinalIgnoreCase)) return "ui_dark";
                if (string.Equals(parentSpriteName, "ui_dark", StringComparison.OrdinalIgnoreCase)) return "ui_white";
                return "ui_dark";
            }

            return rawMatched;
        }

        private static string DeriveRawSpriteName(LiteCompiledNodeDto node, AIToUGUILiteControlType controlType, Color c)
        {
            var isButton = controlType == AIToUGUILiteControlType.Button;
            var isProgress = controlType == AIToUGUILiteControlType.Progress;
            var isInput = controlType == AIToUGUILiteControlType.Input;

            var nameLower = node != null ? (node.name ?? string.Empty).ToLowerInvariant() : string.Empty;
            var roleLower = node != null ? (node.role ?? string.Empty).ToLowerInvariant() : string.Empty;
            var classStr = node != null && node.classes != null ? string.Join(" ", node.classes).ToLowerInvariant() : string.Empty;

            if (nameLower.Contains("danger") || nameLower.Contains("cancel") || nameLower.Contains("delete") || nameLower.Contains("red") || classStr.Contains("danger"))
            {
                return "ui_red";
            }

            if (nameLower.Contains("confirm") || nameLower.Contains("start") || nameLower.Contains("success") || nameLower.Contains("green"))
            {
                return "ui_green";
            }

            if (nameLower.Contains("lock") || nameLower.Contains("vip") || nameLower.Contains("coin") || nameLower.Contains("gold") || nameLower.Contains("star") || nameLower.Contains("yellow"))
            {
                return "ui_yellow";
            }

            if (nameLower.Contains("skill") || nameLower.Contains("rune") || nameLower.Contains("equip") || nameLower.Contains("purple"))
            {
                return "ui_purple";
            }

            if (nameLower.Contains("chat") || nameLower.Contains("search") || isInput || nameLower.Contains("disabled"))
            {
                return "ui_gray";
            }

            var isRootOrModal = string.Equals(roleLower, "page/root", StringComparison.OrdinalIgnoreCase) ||
                                nameLower.Contains("modal") || nameLower.Contains("dialog") ||
                                nameLower.Contains("bg") || nameLower.Contains("panel") || nameLower.Contains("mask");

            if (isRootOrModal && !isButton)
            {
                return "ui_dark";
            }

            if (isButton)
            {
                if (c.r > 0.8f && c.g > 0.6f && c.b < 0.3f) return "ui_yellow";
                if (c.g > 0.6f && c.r < 0.4f) return "ui_green";
                if (c.r > 0.7f && c.g < 0.4f) return "ui_red";
                if (classStr.Contains("primary")) return "ui_yellow";
                return "ui_blue";
            }

            if (c.a <= 0.35f)
            {
                return "ui_dark";
            }

            Color.RGBToHSV(c, out var h, out var s, out var v);

            if (s < 0.25f || v < 0.35f)
            {
                if (v > 0.85f) return "ui_white";
                if (v < 0.4f) return "ui_dark";
                return "ui_gray";
            }

            var hueDeg = h * 360f;

            if (hueDeg >= 70f && hueDeg <= 165f) return "ui_green";
            if (hueDeg >= 256f && hueDeg <= 315f) return "ui_purple";
            if (hueDeg >= 320f || hueDeg <= 25f) return "ui_red";
            if (hueDeg >= 26f && hueDeg <= 69f) return "ui_yellow";

            if (hueDeg >= 170f && hueDeg <= 255f)
            {
                return isButton ? "ui_blue" : "ui_dark";
            }

            // 默认兜底为干净的白色/暗色，绝对不再默认兜底为大蓝色！
            return "ui_white";
        }

        #endregion
    }
}

#endif

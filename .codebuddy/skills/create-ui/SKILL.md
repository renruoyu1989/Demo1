---
name: create-ui
description: Generate Unity UGUI Prefabs from natural language UI descriptions. This skill uses pure Python CLI toolchain — no server required. Triggered by /create_ui command, or user asking to "create a UI", "make a game UI", "generate a UI panel".
---

# /create_ui — AI-Driven Unity UGUI Prefab Generator

## Purpose

Transform a natural-language UI description into a ready-to-use Unity UGUI Prefab. The pipeline: AI plans the design → generates HTML/CSS source files → validates via Python CLI toolchain → compiles to `compiled_site_bundle.json` → Unity AIToUGUI_LITE imports to `.prefab`.

No HTTP server required. All toolchain steps run as direct Python CLI commands.

## Workflow

### Step 1: Create Workspace

Create a workspace directory under `workspaces/<task_id>/`. Use a short unique ID (e.g., timestamp hex).

The standard workspace structure:
```
workspaces/<task_id>/
  source/
    site.json
    theme.css
    pages/
    shared/
  reports/
```

Write `source/site.json`:
```json
{
  "siteId": "my-ui",
  "displayName": "UI Name",
  "designWidth": 1920,
  "designHeight": 1080,
  "themeCss": "theme.css",
  "sharedStyles": ["shared/widgets.css"],
  "pages": [{
    "pageId": "main",
    "title": "Main Page",
    "html": "pages/main.html",
    "prefabName": "MainPage",
    "targetLayer": "Normal"
  }]
}
```

### Step 2: Plan (AI Artifacts)

Generate planning docs in `source/`:
- `source/draft_visual_intent.md` — Style axes, material, color relationships
- `source/page_scope.md` — Scope mode, page list, priorities
- `source/site_plan.md` — Shared invariants, page delta matrix
- `source/page_plans/<pageId>.md` — Per-page layout budget, canonical roles, CTA

### Step 3: Generate Source Files

Generate HTML/CSS following the rules in `references/AIToUGUI_CSS与HTML实现细则.md`.

Key constraints:
- All elements must use `position:absolute` with explicit `left/top/width/height` (except template children in scroll containers)
- All elements need `box-sizing:border-box`
- All buttons require `data-ui-element="button"`
- 1920×1080 design canvas

### Icon Rule (No Text-Glyph Icons — High Miss Rate)

Do **NOT** represent icons / emblems with text glyphs (`⚔ 💀 ✊ 🪙 ✚ ⚠ ✅ 🎙 …`) inside a `<div>` or `<button>`.

The compiler maps **any element carrying text** to `TextMeshPro`, so a text-glyph "icon" becomes a TextMeshPro node instead of an `Image` — wrong component type, and such glyphs often render as tofu (□) because the icon font lacks them.

Represent icons as visual nodes instead:

```html
<!-- ✅ styled box -->
<div data-ui-name="IconColKill" class="stat-col-icon"
     style="position:absolute;left:20px;top:2px;width:32px;height:26px;box-sizing:border-box;background-color:#94a3b8;border-radius:6px;"></div>

<!-- ✅ or an image / SVG asset host -->
<div data-ui-name="IconColKill" data-ui-element="image"
     data-ui-asset-id="icon-kill" data-ui-asset-type="icon" data-ui-asset-usage="icon"
     data-ui-asset-import="sprite"
     style="position:absolute;left:20px;top:2px;width:32px;height:26px;box-sizing:border-box;"></div>
```

Only use real text when the content is genuinely text (labels, numbers, titles). If you find yourself writing an icon as a character, stop and make it a box or an image node.

### ScrollView Rule (High Miss Rate — Read Carefully)

`data-ui-type="scroll"` is the **only** way to create a Unity ScrollRect. CSS `overflow:scroll` does NOT work.

The single most common mistake in this pipeline is **generating multiple identical card elements directly without a scroll container**. This mistake breaks the runtime data-binding. Avoid it at all costs.

#### Golden Rule

**If a region contains 2+ child elements that share the same internal structure (same child layout, same class, same icon+text pattern), it IS a runtime-driven list/grid. Always write it as a scrollview with ONE template, regardless of whether the items currently fit on screen.**

This applies to: skill grids, item inventories, card lists, tab content panels, mission/reward lists, shop items — any area where the items are structurally identical and would be populated from data at runtime.

#### Judgment Flowchart

```
Does the region have a name containing "Section", "List", "Grid", "Panel", 
"区域", "列表", "网格", "卡片" ?
└─ YES → Is it a container that holds multiple card-like items 
          with the same structure?
   ├─ YES (2+ identical cards) → ✅ MUST be data-ui-type="scroll"
   └─ NO (items are all unique/different) → regular div, no scroll

Is the region clearly a grid/list from the reference image or UI description?
└─ YES → ✅ MUST be data-ui-type="scroll"

Does the region only contain 0-1 items, or items of completely different types?
└─ YES → NOT a scrollview, use absolute positioning
```

#### Common Pitfall — DO NOT do this

```html
<!-- ❌ WRONG: 4 identical cards directly placed — NO scrollview -->
<div data-ui-name="SynergySection" style="...width:860px;height:490px...">
  <div data-ui-name="SynergyCard_1" class="synergy-card" style="left:0;top:60px;...">...</div>
  <div data-ui-name="SynergyCard_2" class="synergy-card" style="left:450px;top:60px;...">...</div>
  <div data-ui-name="SynergyCard_3" class="synergy-card" style="left:0;top:280px;...">...</div>
  <div data-ui-name="SynergyCard_4" class="synergy-card" style="left:450px;top:280px;...">...</div>
</div>
```

Even if all 4 cards fit on screen, this is **wrong** because:
- At runtime, the actual card count may differ (more or fewer)
- The cards should come from data, not be hardcoded
- A scroll container allows the content area to scroll when there are more items

#### Correct pattern

```html
<!-- ✅ CORRECT: scroll container with ONE ItemTemplate -->
<div data-ui-name="SynergySection" data-ui-type="scroll" data-ui-element="scrollview"
     style="position:absolute;left:380px;top:130px;width:860px;height:490px;box-sizing:border-box;">
  <div data-ui-name="ItemTemplate" data-ui-role="templates/item"
       style="width:410px;height:190px;box-sizing:border-box;">
    <div class="skill-circle" style="position:absolute;left:20px;top:20px;width:80px;height:80px;box-sizing:border-box;border-radius:50%;"></div>
    <div style="position:absolute;left:120px;top:40px;width:20px;height:30px;box-sizing:border-box;text-align:center;">+</div>
    <div class="skill-circle" style="position:absolute;left:180px;top:20px;width:80px;height:80px;box-sizing:border-box;border-radius:50%;"></div>
    <div style="position:absolute;left:20px;top:135px;width:370px;height:30px;box-sizing:border-box;text-align:center;">ItemName</div>
  </div>
</div>
```

#### Locked/missing items are still templated

If some items in the grid have a special state (locked, active, etc.), **do not** create separate elements for each state. Instead include all possible state visuals inside the **single template** with appropriate `data-ui-role` attributes, or use CSS to toggle visibility. The template represents the item at its fullest.

#### Self-check (MANDATORY — run this before writing any HTML)

1. Look at each region in the planning docs
2. Search for phrases like "section", "grid", "list", "card", "item", "区域", "网格", "列表", "卡片", "模板"
3. Ask: "Does this region contain **structurally identical repeated units**?"
4. If YES → `data-ui-type="scroll"` with ONE `data-ui-role="templates/item"`
5. If NO → regular div with absolute children

### Full-Canvas & Peripheral UI Completeness Rule (High Miss Rate — Read Carefully)

在分析 UI 设计图或生成页面结构时，需严格遵循**全域网格扫描与全量元素提取法则**，防止因视线过度聚焦于中央主内容区或强 Call-to-Action 按钮，而遗漏边缘与次要功能控件。

#### 1. UI 元素遗漏防范原则 (Loss Prevention Principles)
- **禁止基于尺寸与边缘位置丢弃元素**：处于界面角落、四边栏或次要层级的小图标、悬浮微构件、辅助文本与状态指示器，均属于有效 UI 控件，必须全量在 HTML 中建模。
- **功能控件与背景装饰剥离**：任何包含图标、图形符号、交互提示文字或独立边界框的元素，一律视为功能节点（Functional Node），严禁合入静态背景层。

#### 2. Canvas 空间四区与四边通用审计规范 (Full-Canvas Audit Checklist)

在撰写 `page_plans/<pageId>.md` 和页面 HTML 之前，必须对画板进行全域 360 度空间矩阵审查：

1. **Header / Top Zone (顶栏区)**：
   - 全量提取：导航按键、全局标题、系统状态指示、资源/数据汇总、二级筛选/下拉页签、全局控制按钮。
2. **Footer / Bottom Zone (底栏区)**：
   - 全量提取：输入/沟通对话控件、频道与状态切页、次要辅助操作、主要行动入口（Primary CTA）。
3. **Margins & Sidebars (两侧侧边栏与边缘区)**：
   - 全量提取：辅助功能菜单、悬浮快捷按键、侧边抽屉 trigger、次要状态指示卡片。
4. **Corners & Overlays (四角与浮层微构件)**：
   - 全量提取：贴角放置的辅助小图标、状态角标、浮动提示框、版本/版权信息说明。

**通用铁律**：
- **参考图背景忠实还原原则 (Background Fidelity Rule)**：当参考图中**存在显式的全屏背景、大原画背景图或渐变底板**时，在提取 HTML 结构时**严禁省略背景节点**！必须在根节点 `<div data-ui-page="...">` 内的首个位置，将全屏背景提取为独立的 DOM 节点（如 `<div id="Background" data-ui-name="Background" data-ui-role="container/background"></div>`），确保视觉图中的全屏背景 100% 忠实还原至 Unity Prefab 中。
- **HTML 节点名称规范 (data-ui-name)**：所有具有业务意义或层级结构的 UI 节点，**必须显式写上 `data-ui-name="xxx"`**（如 `<div id="HeaderHudPanel" data-ui-name="HeaderHudPanel">`）。`data-ui-name` 将 100% 决定 Unity Prefab 中 GameObject 的名字，严禁出现匿名的 `<div>` 结构。
- 设计图中呈现的所有可见文本、视觉图标与交互构件，均**必须在 HTML 节点树中具备独立绝对定位节点**，确保 UI 结构的完整还原。

### Step 4: Run Pipeline (Python CLI)

Run these 4 commands in order from the project root:

```powershell
# 1. Contract
python .gemini/skills/create-ui/PythonTool/generate_site_contract.py workspaces/<task_id> --write-self-check --allowlist .gemini/skills/create-ui/references/AIToUGUI_Allowlist参考.json

# 2. Validate
python .gemini/skills/create-ui/PythonTool/validate_and_prepare_repair.py workspaces/<task_id> --allowlist .gemini/skills/create-ui/references/AIToUGUI_Allowlist参考.json

# 3. Compile
python .gemini/skills/create-ui/PythonTool/compile_site_bundle.py workspaces/<task_id>

# 4. Theme Health (optional but recommended)
python .gemini/skills/create-ui/PythonTool/audit_compiled_theme_health.py workspaces/<task_id>
```

After each step, check the reports:
- `workspaces/<task_id>/reports/validation_report.json` — must show `status: "pass"` and `errorCount: 0`
- `workspaces/<task_id>/reports/repair_package/repair_summary.md` — fix guide if validate fails
- `workspaces/<task_id>/compiled_site_bundle.json` — output artifact

If validation fails, read the repair summary, fix the HTML/CSS, and re-run from step 1.

### Step 5: Import to Unity & Generate Prefab

**MCP 自动化无感调用（无需任何 MenuItem 菜单显示）：**

```json
mcp_unityMCP_execute_code(action="execute", code="return AIToUGUI.Lite.LiteBatchImporter.ImportLatest();")
```

**代码调用（指定路径）：**

```csharp
AIToUGUI.Lite.LiteBatchImporter.ImportFromPath(bundlePath);           // → Assets/Prefabs/
AIToUGUI.Lite.LiteBatchImporter.ImportFromPath(bundlePath, "Assets/Logic/M525");
```

**手动操作：** `Tools → AIToUGUI → Import from File...`

## Reference Documents

All reference docs are in `references/`:

| Document | Content |
|----------|---------|
| `AIToUGUI_GenerationLauncher.md` | Generation entry checklist |
| `UI风格参考.md` | Style reference guide |
| `游戏UI页面识别标准文档.md` | UI page recognition standards |
| `AIToUGUI_形态语言与风格分叉规范.md` | Shape language & style divergence |
| `AIToUGUI_SitePackageSpec.md` | Site package structure spec |
| `AIToUGUI_能力池与当前上限.md` | Capability pool & limits |
| `AIToUGUI_Contract与自检规范.md` | Contract & self-check rules |
| `AIToUGUI_CSS与HTML实现细则.md` | **CSS & HTML implementation rules** (must read) |
| `AIToUGUI_PlanFirst风格与页面规划规范.md` | Plan-first style & page planning |
| `Unity能力与HTML写法映射.md` | **Unity capability → HTML mapping** (must read) |
| `AIToUGUI_Allowlist参考.json` | Allowlist reference |
| `AIToUGUI_Full能力对齐补充清单.md` | Full capability alignment supplement |
| `AIToUGUI_三段式生成与收敛流程.md` | Three-phase generation & convergence |
| `AIToUGUI_TeamGenerationWorkflow.md` | Team generation workflow |
| `主题与基础元素编写规范.md` | Theme & base element authoring |
| `../检查清单.md` | Quality checklist for all phases |
| `../使用指南.md` | Usage guide for non-Codex agents |

## Toolchain Scripts

Self-contained in `PythonTool/` (this skill's own directory):

| Script | Purpose |
|--------|---------|
| `generate_site_contract.py` | Generate contract + self-check from source |
| `validate_and_prepare_repair.py` | Validate + prepare repair package |
| `compile_site_bundle.py` | Compile to `compiled_site_bundle.json` |
| `audit_compiled_theme_health.py` | Post-compile theme health audit |
| `audit_style_diversity.py` | Audit style axis divergence across site collections |

These scripts only depend on Python 3 standard library — no pip installs needed.

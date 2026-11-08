# AIToUGUI CSS 与 HTML 实现细则

目标不是做浏览器级网站，而是做“可静态预览、可稳定校验、可编译、可在 Unity 中近似 1:1 还原”的游戏 UI 页面。

## 优先级

1. 可编译。
2. 可稳定还原。
3. 再考虑 authoring 是否省事。

## Allowlist First

只允许使用 allowlist 支持的：

- 标签
- 属性
- 选择器
- CSS 属性
- 值模式

不要先自由写页面，再靠 banlist 回收。

## Unity 消费层与预览层

最终进入 Unity 的样式，只能稳定来自：

- `:root` token
- `.class`
- `[data-ui-role="..."]`
- 节点 inline `style`

`html` / `body` 允许存在，但只用于浏览器预览画布复位：

- `margin:0`
- `width:1920px`
- `height:1080px`
- `overflow:hidden`

不要把页面主视觉、主色块、标题条、面板风格、核心按钮风格主要写在 `html` / `body` 上。

## 关键控件映射（data-ui-type）

HTML 中无法用 CSS `overflow:scroll` 或 tag 名表达某些 Unity 控件。必须用 `data-ui-type` 显式声明：

| data-ui-type 值 | Unity 控件 | 说明 |
|----------------|-----------|------|
| `"scroll"` | **ScrollRect** | 滚动列表/网格。编译器自动创建 Viewport+Mask+Content |
| `"button"` | Button | 覆盖默认推断 |
| `"input"` | TMP_InputField | 文本输入 |
| `"toggle"` | Toggle | 开关 |
| `"slider"` | Slider | 滑块 |
| `"dropdown"` | TMP_Dropdown | 下拉菜单 |
| `"progress"` | Image(fill) | 进度条 |
| `"scrollbar"` | Scrollbar | 滚动条 |

**重点：任何需要滚动的内容区域（列表、网格、长文本等），必须添加 `data-ui-type="scroll"`，CSS `overflow:scroll` 在 AIToUGUI 体系中不会自动转换为 ScrollRect。**

### ScrollRect 滚动方向

ScrollRect 的滚动方向通过 scroll 容器的 `flex-direction` 决定：

- `flex-direction: row` → **横向滚动**列表（水平滚动）
- `flex-direction: column` → **纵向滚动**列表（垂直滚动）
- 未指定时默认双向滚动（horizontal + vertical）

```html
<!-- 纵向滚动列表 -->
<div data-ui-name="SkillList" data-ui-type="scroll"
     style="display:flex;flex-direction:column;position:absolute;left:0;top:0;width:400px;height:600px;box-sizing:border-box;">
  ...
</div>

<!-- 横向滚动列表 -->
<div data-ui-name="CardRow" data-ui-type="scroll"
     style="display:flex;flex-direction:row;position:absolute;left:0;top:0;width:800px;height:200px;box-sizing:border-box;">
  ...
</div>
```

### ScrollRect 内容模板规则

ScrollRect 是运行时数据驱动的列表/网格，prefab 中**不需要也不应该**生成所有实例。采用模板替代：

- **无分类场景**：scroll 容器内只放一个 `ItemTemplate`（`data-ui-role="templates/item"`）
- **有分类场景**：放 `TitleTemplate`（`data-ui-role="templates/section-title"`）+ `ItemTemplate`
- 每个模板包含该类元素**完整的子结构**（图标、文字、按钮等）和**正确的尺寸**
- 模板内文本使用占位值如 `"ItemName"`、`"CategoryName"`

```html
<!-- 无分类：只生成1个 ItemTemplate -->
<!-- 注意：template 本身不需要是 button，内部按需放置具体的交互按钮 -->
<div data-ui-name="ContentList" data-ui-type="scroll"
     style="position:absolute;left:0;top:0;width:800px;height:600px;box-sizing:border-box;">
  <div data-ui-name="ItemTemplate" data-ui-role="templates/item"
       style="width:760px;height:90px;box-sizing:border-box;">
    <button data-ui-name="ItemBtn" data-ui-role="buttons/action" data-ui-element="button"
            style="width:760px;height:90px;box-sizing:border-box;background-color:transparent;border:1px solid;">
      <div style="width:64px;height:64px;border-radius:50%;border:1px solid;..."></div>
      <div style="font-size:14px;">ItemName</div>
    </button>
  </div>
</div>
```

## 样式职责划分

每个节点的样式来源固定分三层：

- `[data-ui-role="..."]`
  负责语义视觉，如背景、描边、圆角、阴影、发光、主色。
- `.class-name`
  负责通用布局和排版，如 flex、gap、字体等级、按钮文本布局。
- `style`
  负责页面独有的位置、尺寸和少量局部收口。

## 视觉优先与 shape 语义冲突

当前 authoring 统一按下面规则处理：

1. 如果浏览器预览中的最终轮廓是标准矩形或四角圆角矩形，就按矩形系 authoring。
2. 不要同时给同一个节点写：
   - `data-ui-shape="plate" / "banner" / "cut-corner"`
   - 又显式写 `border-radius`
3. `data-ui-shape` 表达的是轮廓拓扑，不是风格标签。
4. 修旧页面时，如果旧 `shapeId` 与显式圆角冲突，优先保留可见结果。

## data-ui-shape 使用边界

优先按下面口径写：

- 标准矩形
  - 不写异形 `data-ui-shape`
- 四角独立圆角矩形
  - 直接写 `border-radius`
- 只有确实需要非矩形轮廓时，才写：
  - `data-ui-shape="cut-corner"`
  - `data-ui-shape="plate"`
  - `data-ui-shape="banner"`

不要把下面这种写法当成常规模式：

- 为了“更有设计感”，给普通矩形额外挂一个异形 `shape`

## 阴影与边框 authoring 规则

1. 描边颜色应显式写在 `border` 或明确可映射的视觉语义里。
2. 如果目标视觉需要双层阴影，只允许这两层：
   - `glow`
   - `projection`
3. 不要随手堆多个深色阴影层，这会让 Unity 端更容易出现整体发黑。
4. 如果目标是金属边框、能量边框、铭文边框，先保证边框色和 glow 色显式存在，再补投影层。

## 字体桶规则

`font-family` 只作为 Unity 字体桶提示使用：

- `primary`
- `heading`
- `mono`

允许写成：

- `"primary", sans-serif`
- `"heading", sans-serif`
- `"mono", monospace`

不要再用具体网页字体名来承载最终风格差异，因为 Unity 侧不会按网页字体栈逐项还原。

## 标准类名

优先复用：

- `screen-root`
- `stack-horizontal`
- `stack-vertical`
- `gap-xs`
- `gap-sm`
- `gap-md`
- `gap-lg`
- `pad-sm`
- `pad-md`
- `pad-lg`
- `headline`
- `section-title`
- `body-text`
- `micro-label`
- `stat-number`
- `slab-button`
- `small-button`
- `chip`
- `chip-row`
- `stat-card`
- `info-list`
- `info-row`

## body 规则

`theme.css` 中必须存在：

```css
body {
  margin: 0;
  width: 1920px;
  height: 1080px;
  overflow: hidden;
}
```

推荐同时加上：

```css
html {
  margin: 0;
  width: 1920px;
  height: 1080px;
}
```

## 页面根规则

每页根节点都要满足：

- `data-ui-page`
- `data-ui-name`
- `data-ui-role`
- `class="screen-root"`
- 显式 `width:1920px; height:1080px;`

## 页面主背景规则

如果页面存在主背景图、场景底图或整页主视觉背景：

1. 背景承载节点必须覆盖整个 `site.json.designWidth x site.json.designHeight` 页面区域。
2. 页面主背景层禁止 `border-radius`。
3. 页面主背景层应视为整页底板，而不是一张带圆角的卡片。
4. `border-radius` 只应用于窗口、面板、按钮、卡片、弹框等前景 UI 模块。

## 命名与导出规则

1. 页面根、交互节点、绑定目标节点、动态容器、默认尺寸 opt-in 根、slot 节点必须显式写 `data-ui-name`。

2. 单页内 `data-ui-name` 必须唯一。

3. 不要把业务稳定节点命名成：
   - `Text1`
   - `Button2`
   - `Node_7`
   - `RedBox`

4. 不要使用保留前缀 `__ai_`。
   该前缀留给 Unity 侧内部辅助节点。

5. 修复旧页面时，不要随意改已有 `data-ui-name`。
   命名一旦被业务侧使用，就应视为稳定接口。

## 尺寸规则

以下节点必须显式宽高：

- 主页面根
- HUD 固定块
- 按钮
- 卡片
- 面板
- 模态框
- 资源块
- 统计行
- 进度条轨道
- 进度条填充
- chip

通常只有这些节点允许不写宽高：

- 小型文本叶子节点
- `span` 文本子节点
- Contract 中明确声明为 `auto-text` 的文本容器

## border-box 心智

所有 `width` / `height` 都按最终外框尺寸理解：

- padding 算在里面
- border 算在里面

不要按浏览器默认 `content-box` 去理解尺寸。

硬规则：

- 页面根节点必须最终解析到 `box-sizing:border-box`
- 任何带显式 `width` 或 `height`，同时又带 `padding` 或 `border` 的关键节点，也必须最终解析到 `box-sizing:border-box`
- 不允许写出“浏览器能靠 content-box 撑开，但 Unity 会按外框尺寸解释”的尺寸

## footprint 自检

对任何显式 `display:flex` 的固定尺寸容器，在输出前都要人工确认：

- 子节点主轴总 footprint + gap 不超过父容器可用内容区
- 子节点交叉轴 footprint 不超过父容器可用内容区
- 不依赖浏览器溢出、压缩或默认按钮盒模型把内容“塞进去”

## 文本规则

1. 标题、按钮、数值、标签、统计行默认按单行控件处理。
   它们要有明确高度。

2. 多行说明文案必须有明确宽度。
   不要让浏览器自由决定换行宽度。

3. 优先使用像素级 `line-height`。
   尤其是：
   - `headline`
   - `section-title`
   - `body-text`
   - `micro-label`
   - `stat-number`

4. 不要让文本自然撑开关键容器。
   关键容器尺寸先确定，再放文本。

## data-ui-element 规则

`data-ui-element` 只表达语义控件映射，不默认接管最终尺寸。

只有节点显式声明：

```html
data-ui-template-size="true"
```

并且在 Contract 的 `templateSizeNodes` 中登记后，才允许使用默认尺寸映射。

## 区域 authoring 规则

Unity 侧会按“左/中/右”和“上/中/下”区域推断锚点，所以页面 authoring 也要遵守区域心智：

1. 视觉上应居中的大块，要在 `site.json.designWidth x site.json.designHeight` 设计空间中真正居中。
2. 应贴左、贴右、贴顶、贴底的块，要明确处在对应边缘区域。
3. 不要把“想让它居中”的块写成一个靠左偏移的大矩形。
4. 不要把“想让它贴边”的块写在中间区域后再靠内容撑出来。

## flex 允许范围

允许使用：

- `display:flex`
- `flex-direction: row / column`
- `flex-grow`
- `flex-shrink`
- `flex:1`（flex 简写）
- `gap`
- `padding`
- `align-items: flex-start / center / flex-end / stretch`
- `justify-content: flex-start / center / flex-end / space-between`
- `margin-left:auto`
- `margin-top:auto`
- `max-width`
- `min-width`
- `min-height`

允许浏览器参与的能力只到这里。不要把最终尺寸寄托在以下行为上：

- 浏览器默认按钮尺寸
- 文本自然撑开关键容器
- `flex-wrap`

## 按钮约束

所有 `button` 必须有：

- `data-ui-name`
- `data-ui-role`
- `data-ui-element`
- 显式 `width`
- 显式 `height`

按钮默认应是文字居中。除非需求明确，否则不要做左对齐按钮文本。

如果后续脚本只需要监听点击：

- 按钮本体命名即可

如果后续脚本还要改单独的按钮文字：

- 按钮本体命名
- 文本子节点也单独命名

## 允许的文本写法

允许：

- 叶子 `div` 直接文本
- `span` 子文本
- `button` 直接文本

但重要文本块仍要明确：

- 宽度
- 高度或可推导的容器高度
- 行高
- 对齐方式

如果文字后续会被运行时单独读取或修改，还要满足：

- 文本节点本身有稳定 `data-ui-name`
- 不要只依赖父容器名称

## 禁止的浏览器专属能力

- `flex-wrap`
- `grid`
- `calc()`
- `aspect-ratio`
- `conic-gradient`
- `repeating-linear-gradient`
- `repeating-radial-gradient`
- 任意浏览器 `background-image`（仅显式本地 SVG 资产引用除外）
- `clip-path`
- `mask`
- `mix-blend-mode`
- `filter`
- `backdrop-filter`
- `position:fixed`
- 复杂 `@keyframes`（非 rotate/float/pulse 预设的自定义关键帧）

以下是 Unity 实际支持的（不要禁止）：

- `opacity`（0..1）
- `transform: rotate(<deg>)`
- `animation: rotate / float / pulse` 预设
- `animation-delay`
- `flex-grow` / `flex-shrink` / `flex:1`
- `border-style: dashed`
- `radial-gradient`（近似支持）
- 多层 `box-shadow`（有语义即可）

显式本地 SVG 资产引用属于正式支持范围，但必须同时满足：

- 宿主节点带完整 `data-ui-asset-*` 元数据
- 资源文件位于 `source/assets/*.svg`
- SVG 只承担 icon / frame / ornament / rotated background / complex plate 等复杂视觉
- 不依赖 `filter`、`mask`、`mix-blend-mode`、浏览器专有滤镜链
- 不把需要运行时修改的文本直接烤进 SVG

如果想做强风格效果，优先改写成：

- 单层 `linear-gradient(...)`
- 单层 `box-shadow`
- 或语义明确的双层 `box-shadow`（`glow + projection`）
- `data-ui-shape`
- `data-ui-frame`
- `data-ui-glow*`
- 额外装饰节点

## 常见失败原因

### 1. 浏览器里正常，Unity 里按钮变窄

常见原因不是 UGUI，而是 HTML 把尺寸建立在浏览器默认按钮行为、文本自然撑开或 flex 伸缩上。

### 2. 某些行高或文本高度异常

常见原因：

- 行容器没有明确高度
- 文本类没有明确像素 `line-height`
- 文本与布局职责混在同一层，导致浏览器自动补偿

### 3. 浏览器正常，Unity 偏差很大

优先检查：

- 是否依赖浏览器默认按钮尺寸
- 是否依赖内容自然撑开
- 是否使用了未声明的相对尺寸
- 是否使用 allowlist 之外的属性或值
- 是否把中心区域或边缘区域写错了 authoring 位置

### 4. 结构能导入，但运行时脚本拿不到节点

常见原因：

- 关键节点没有 `data-ui-name`
- 单页内有重复名称
- 只给容器命名，没有给实际要绑定的文本子节点命名
- 修复回合随手改了旧名字

## 图标规则（禁止文字字形图标）

不要在 `<div>` / `<button>` 里用文字字形（emoji 或 `⚔ 💀 ✊ 🪙 ✚ ⚠` 等符号）来充当图标。
编译器会把**任何带文字的节点**映射为 `TextMeshPro`，因此“文字图标”会得到错误的组件类型（应为 `Image`），
而且这些字形常常在图标字体里缺字，最终渲染成方框（□）。

图标请用可视节点表达：

- 纯色/形状块：
  ```html
  <div data-ui-name="IconX" style="position:absolute;left:20px;top:2px;width:32px;height:26px;box-sizing:border-box;background-color:#94a3b8;border-radius:6px;"></div>
  ```
- 或图片 / SVG 资产宿主：
  ```html
  <div data-ui-name="IconX" data-ui-element="image"
       data-ui-asset-id="icon-x" data-ui-asset-type="icon" data-ui-asset-usage="icon"
       data-ui-asset-import="sprite"
       style="position:absolute;left:20px;top:2px;width:32px;height:26px;box-sizing:border-box;"></div>
  ```

只有当内容确实是文本（标签、数值、标题）时才使用文字节点。

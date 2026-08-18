using System.Collections.Generic;
using Ember.Basic;
using Ember.Core;

namespace Game.Module
{
    /// <summary>
    /// 红点模块 —— 树形红点系统（红点传播 + 显示形态聚合）。
    ///
    /// <b>核心概念：</b>
    /// - <see cref="RedDotType"/>：功能类型，标记「谁触发了红点」
    /// - <see cref="RedDotUIItemType"/>：UI 节点类型，标记「红点挂在哪个 UI 节点」
    /// - 配置资产 <see cref="RedDotConfig"/> 描述每个功能要传播到的 UI 节点链（外 → 内），
    ///   运行时构建成 <see cref="RedDotNode"/> 树，子节点变化自动向上聚合。
    ///
    /// <b>定位：</b>
    /// 作为 <see cref="IEmberModule"/>（Phase = <see cref="ModulePhase.Gameplay"/>），
    /// 进入玩法时由 <see cref="EmberModuleCollector"/> 自动初始化，离开玩法时销毁。
    ///
    /// <b>启用：</b>
    /// 本模块 <see cref="Enabled"/> 默认 false（关闭）。启用时改为返回 true，
    /// 并在 <c>GameplayState.OnGameplayEnter</c> 里调用 <see cref="Initialize"/> 传入配置资产。
    ///
    /// <b>使用示例：</b>
    /// <code>
    /// // 1. 设置红点（业务触发）
    /// RedDotModule.Instance.SetRedNode(RedDotType.Mail_Unread, 3);
    ///
    /// // 2. 查询红点（业务/UI 读取）
    /// int num = RedDotModule.Instance.GetRedNodeNum(RedDotType.Mail_Unread);
    ///
    /// // 3. UI 绑定：拿到节点订阅变化
    /// var node = RedDotModule.Instance.GetNode(RedDotUIItemType.Main_Mail);
    /// node.NumChanged += OnMailRedDotChanged;   // OnMailRedDotChanged 里读 node.Num 刷新显示
    /// </code>
    /// </summary>
    public class RedDotModule : EmberSingleton<RedDotModule>, IEmberModule
    {
        private const string TAG = LogTags.Game + "." + nameof(RedDotModule);

        /// <summary>模块是否启用。默认关闭，需要时改为返回 true。</summary>
        public bool Enabled => false;

        public int Phase => ModulePhase.Gameplay;

        #region 内部参数

        /// <summary>红点配置资产（Initialize 传入）。</summary>
        private RedDotConfig _config;

        /// <summary>UI 节点类型 → 节点（供 UI 绑定）。</summary>
        private readonly Dictionary<RedDotUIItemType, RedDotNode> _itemNodes = new();

        /// <summary>功能类型 → 最内层节点（供 SetRedNode / GetRedNodeNum）。</summary>
        private readonly Dictionary<RedDotType, RedDotNode> _typeNodes = new();

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberModule.OnInit() { }

        void IEmberModule.OnDestroy()
        {
            Clear();
        }

        void IEmberModule.ResetModuleData()
        {
            Clear();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>清空配置与整棵红点树（销毁 / 热重启时用）。</summary>
        private void Clear()
        {
            foreach (var node in _itemNodes.Values)
                node.ClearNums();

            _itemNodes.Clear();
            _typeNodes.Clear();
            _config = null;
        }

        /// <summary>根据配置资产构建红点树（重建前先清空旧树）。</summary>
        private void BuildTree(RedDotConfig config)
        {
            _itemNodes.Clear();
            _typeNodes.Clear();

            // 1. 收集 UI 节点显示配置，去重
            var uiItemDic = new Dictionary<RedDotUIItemType, RedDotUIItemData>();
            foreach (var item in config.items)
            {
                if (!uiItemDic.ContainsKey(item.itemType))
                    uiItemDic.Add(item.itemType, item);
                else
                    EmberDebug.LogWarning(TAG, $"红点配置：UI 节点类型 {item.itemType} 重复，忽略后者");
            }

            // 2. 按功能构建节点链（外 → 内）
            foreach (var data in config.datas)
            {
                RedDotNode root = null;
                foreach (var itemType in data.uis)
                {
                    if (!uiItemDic.TryGetValue(itemType, out var uiItem))
                    {
                        EmberDebug.LogWarning(TAG, $"红点配置：UI 节点类型 {itemType} 未在 items 中定义，跳过");
                        continue;
                    }

                    if (!_itemNodes.TryGetValue(itemType, out var curNode))
                    {
                        curNode = new RedDotNode(uiItem);
                        _itemNodes.Add(itemType, curNode);
                    }

                    if (root != null)
                    {
                        if (curNode.Parent != null && curNode.Parent != root)
                            EmberDebug.LogWarning(TAG,
                                $"红点配置：UI 节点 {itemType} 被多个父节点引用（{curNode.Parent.ItemType} / {root.ItemType}）");

                        curNode.Parent = root;
                        root.Children ??= new List<RedDotNode>();
                        if (!root.Children.Contains(curNode))
                            root.Children.Add(curNode);
                    }

                    root = curNode;
                }

                if (root != null)
                    _typeNodes[data.id] = root;
            }
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 用配置资产初始化红点系统（构建红点树）。
        /// 进入 Gameplay 后调用一次；重复调用会清空旧树并重建。
        /// </summary>
        public void Initialize(RedDotConfig config)
        {
            if (config == null)
            {
                EmberDebug.LogError(TAG, "RedDotModule.Initialize: config 为 null");
                return;
            }

            _config = config;
            BuildTree(config);
            EmberDebug.LogInit(TAG,
                $"RedDotModule 初始化完成：{_itemNodes.Count} 个 UI 节点，{_typeNodes.Count} 个功能");
        }

        /// <summary>设置某功能类型的红点数量。value 为 0 表示清除。未配置的功能类型会忽略并告警。</summary>
        public void SetRedNode(RedDotType type, int num)
        {
            if (_typeNodes.TryGetValue(type, out var node))
                node.SetNum(type, num);
            else
                EmberDebug.LogWarning(TAG, $"红点功能类型 {type} 未在配置中定义，SetRedNode 被忽略");
        }

        /// <summary>查询某功能类型聚合后的红点数值。未配置返回 0。</summary>
        public int GetRedNodeNum(RedDotType type)
            => _typeNodes.TryGetValue(type, out var node) ? node.Num : 0;

        /// <summary>获取指定 UI 节点对应的红点节点（供 UI 绑定订阅）。未配置返回 null。</summary>
        public RedDotNode GetNode(RedDotUIItemType itemType)
            => _itemNodes.TryGetValue(itemType, out var node) ? node : null;

        #endregion
    }
}

using System;
using Ember.UI;
using Game.Narrative;
using Game.UI;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 名字输入请求 —— 剧情步骤与名字输入弹窗之间的中性参数，两侧都不必知道对方的类型。
    /// 由 <see cref="NovelPlayerNameInputStep"/> 创建并在打开页面时传入。
    /// </summary>
    public sealed class NovelNameInputRequest
    {
        #region 内部参数
        private readonly Action<string> _submit;
        private readonly Action _cancel;
        public string Title { get; }
        public string DefaultName { get; }
        public int MaxLength { get; }
        /// <summary>页面在 OnOpen 时回填自己，供步骤在被打断时精确关闭，而不是关掉“最上面那个弹窗”。</summary>
        public EUIPage Page { get; internal set; }
        /// <summary>已经交回过结果。重复提交或提交后再关闭都不会二次生效。</summary>
        public bool Settled { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        internal NovelNameInputRequest(string title, string defaultName, int maxLength,
            Action<string> submit, Action cancel)
        { Title = title; DefaultName = defaultName; MaxLength = maxLength; _submit = submit; _cancel = cancel; }

        /// <summary>玩家留空时用的名字；空配置也保证有一个可用值，不会把说话人栏变成空白。</summary>
        internal string FallbackName => string.IsNullOrWhiteSpace(DefaultName) ? "旅人" : DefaultName.Trim();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>玩家点了确定。名字为空时用默认名，不会写入空白。</summary>
        public void Submit(string name)
        {
            if (Settled) return;
            Settled = true;
            _submit?.Invoke(string.IsNullOrWhiteSpace(name) ? FallbackName : name.Trim());
        }

        /// <summary>玩家取消或以其他方式关掉了页面。仍然要交回一个结果，否则剧情会停在这一步。</summary>
        public void Cancel()
        {
            if (Settled) return;
            Settled = true;
            _cancel?.Invoke();
        }
        #endregion
    }

    /// <summary>
    /// 开局让玩家输入名字的自定义节点。
    ///
    /// <b>为什么放在 UI 程序集</b>：它要打开正式 EUI 页面，而 <c>GamePages</c> 与页面逻辑都在
    /// <c>Assembly-CSharp</c>；<c>Game.Narrative.Runtime</c> 无法反向引用它。这与既有
    /// <c>NovelSaveUI</c>（UI 侧）配合 <c>NovelSaveModule</c>（模块侧）的分工一致：
    /// 剧情契约留在叙事模块，需要同时依赖叙事与 UI 的适配器留在 UI 侧。
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/视觉小说/输入玩家名字", fileName = "PlayerNameInputStep")]
    public sealed class NovelPlayerNameInputStep : NovelCustomStepSO
    {
        #region 编辑器面板参数
        /// <summary>把输入的名字写进哪个全局字符串变量。后续用 {变量名} 绑定正文，或作为说话人变量。</summary>
        [SerializeField] private string _variableId = "playerName";
        [SerializeField] private string _title = "请输入你的名字";
        /// <summary>玩家留空时使用的名字。</summary>
        [SerializeField] private string _defaultName = "旅人";
        [SerializeField, Range(1, 12)] private int _maxLength = 8;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public override string DisplayName => "输入玩家名字";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Write(NovelCustomStepContext context, string name)
        {
            if (!context.IsAlive) return;
            if (!context.SetVariable(NovelVariableScope.Global, _variableId, new NovelValue(name), out string error))
            { context.Fail(error); return; }
            context.Complete();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override string Summary() => "输入名字 → " + (string.IsNullOrWhiteSpace(_variableId) ? "⚠ 未填变量" : _variableId)
            + "（最多 " + _maxLength + " 字，留空用「" + (string.IsNullOrWhiteSpace(_defaultName) ? "旅人" : _defaultName) + "」）";

        public override string Validate(NovelCustomStepValidation validation)
        {
            if (string.IsNullOrWhiteSpace(_variableId)) return "必须填写目标变量 ID";
            if (validation == null) return null;
            if (!validation.TryGetVariable(NovelVariableScope.Global, _variableId, out NovelValue declared))
                return "目标变量必须是已声明的全局变量：" + _variableId;
            if (declared.Type != NovelValueType.String) return "目标变量必须是字符串：" + _variableId;
            return null;
        }

        public override void OnBegin(NovelCustomStepContext context)
        {
            var request = new NovelNameInputRequest(_title, _defaultName, _maxLength,
                name => Write(context, name),
                () => Write(context, string.IsNullOrWhiteSpace(_defaultName) ? "旅人" : _defaultName.Trim()));
            context.State = request;
            EUIManager.Instance.ShowPopup(GamePages.EUINovelNameInputPage, request);
        }

        public override void OnCancel(NovelCustomStepContext context)
        {
            // 读档 / 退出 / 故障：先关掉还开着的输入页，避免它留在屏幕上收不到结果。
            var request = context.State as NovelNameInputRequest;
            if (request != null && request.Page != null) EUIManager.Instance.ClosePage(request.Page);
            else if (request == null) EUIManager.Instance.ClosePageByDef(GamePages.EUINovelNameInputPage);
        }
        #endregion
    }
}

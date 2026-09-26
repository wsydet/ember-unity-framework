/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelNameInputPage
 * ui role      : Page (EUINovelNameInputPage)
 * create date  : 2026/9/26 17:17:58
==============================================================*/
using Ember.UI;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 名字输入弹窗。参数是 <see cref="NovelNameInputRequest"/>，由
    /// <see cref="NovelPlayerNameInputStep"/> 在剧情里创建并打开。
    /// 页面只负责收字和交回结果，不碰剧情变量。
    ///
    /// <para><b>强制步骤，没有取消按钮</b>：玩家只能确认，或者什么都不做（剧情就停在这一步）。
    /// 页面被其它路径关闭时（读档把会话拆掉、退出到主菜单、故障）必须仍然交回一个结果，
    /// 否则剧情会永久停在这一步——这条兜底在 <see cref="OnClose"/> 里，与按钮无关。</para>
    /// </summary>
    public partial class EUINovelNameInputPage
    {
        #region 内部参数

        private NovelNameInputRequest _request;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void CloseSelf() => EUIManager.Instance.ClosePage(Page);

        private void Submit()
        {
            var request = _request; _request = null;
            string name = Inp_Name == null ? null : Inp_Name.text;
            CloseSelf();
            // 先关闭再交回结果：OnClose 看到 _request 已清空，不会重复结算。
            if (request != null) request.Submit(name);
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            base.OnInit();
            if (Btn_Confirm != null) Btn_Confirm.onClick.AddListener(Submit);
        }

        public override void OnOpen(object param)
        {
            base.OnOpen(param);
            _request = param as NovelNameInputRequest;
            if (_request == null) { CloseSelf(); return; }
            _request.Page = Page;
            if (Txt_Title != null && !string.IsNullOrEmpty(_request.Title)) Txt_Title.text = _request.Title;
            if (Inp_Name != null)
            {
                Inp_Name.characterLimit = Mathf.Max(1, _request.MaxLength);
                Inp_Name.text = _request.DefaultName ?? string.Empty;
                Inp_Name.ActivateInputField();
                Inp_Name.Select();
            }
        }

        public override void OnClose()
        {
            // 兜底：任何非「点确定」的关闭路径（读档、退出、故障）都必须交回结果并写默认名。
            // Cancel 自身幂等，重复调用安全。
            var request = _request; _request = null;
            base.OnClose();
            if (request != null) request.Cancel();
        }

        public override void OnDispose()
        {
            if (Btn_Confirm != null) Btn_Confirm.onClick.RemoveListener(Submit);
            _request = null;
            base.OnDispose();
        }

        #endregion
    }
}

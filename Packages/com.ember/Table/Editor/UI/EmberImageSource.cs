using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Ember.Table.Editor
{
    /// <summary>显式登记按 Key 寻址的图片列；不根据 resourcePath 字段名猜测资源类型。</summary>
    [Serializable]
    public sealed class EmberImageSource
    {
        #region 编辑器面板参数
        [LabelText("分类名称")] public string Label;
        [LabelText("来源表 ID")] public string TableId;
        [LabelText("主键列")] public string KeyColumn = "id";
        [LabelText("图片路径列")] public string ImageColumn = "resourcePath";
        [LabelText("自动分组列")] public string[] GroupColumns = Array.Empty<string>();
        [LabelText("按 Sprite 加载")] public bool Sprite;
        [LabelText("导入目录")] public string ImportFolder = "Assets/GameResource/Resources/UI/Common/Atlas/Imported";
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Identity => TableId + "/" + ImageColumn;
        public string AutoGroup(EmberTableSourceDocument document, EmberTableSourceDocument.Row row)
            => string.Join("/", GroupColumns.Select(c => GroupSegment(document.Get(row, c))));
        public static string GroupSegment(string value) => string.IsNullOrWhiteSpace(value)
            ? "未分类" : Uri.EscapeDataString(value);
        public void Validate(EmberTableSourceDocument document)
        {
            if (string.IsNullOrWhiteSpace(TableId) || string.IsNullOrWhiteSpace(Label))
                throw new InvalidOperationException("图片来源必须填写表 ID 和分类名称。");
            GroupColumns ??= Array.Empty<string>();
            foreach (string column in new[] { KeyColumn, ImageColumn }.Concat(GroupColumns))
                if (document.Column(column) < 0) throw new InvalidOperationException(TableId + " 缺少列：" + column);
        }
        #endregion
    }
}

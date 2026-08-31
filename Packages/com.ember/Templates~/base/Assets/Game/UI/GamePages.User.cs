// 用户页面注册区 —— 本文件属于用户，框架升级永不覆盖。
//
// 与框架的 GamePages.cs 为同名 partial 类，编译时自动拼接，
// 运行时用法完全一致：EUIManager.Instance.ShowMainPage(GamePages.XXX)。
//
// 添加方式（二选一）：
// ① 手写注册：
//      public static readonly EUIPageDef MyPage = new("Assets/Game/UI/Runtime/Prefabs/MyPage.prefab", UILayer.Popup, PageType.Popup);
// ② 通过 Ember 代码生成器（EmberCSharpImplementation 已配置指向本文件）自动写入。
using Ember.UI;

namespace Game.UI
{
    /// <summary>
    /// 用户页面注册区（partial 类，与框架的 GamePages.cs 拼接）。
    /// 框架升级绝不触碰本文件；新增页面注册写在下面。
    /// </summary>
    public static partial class GamePages
    {
        // TODO: 在此处继续添加（用户页面注册区）

    }
}
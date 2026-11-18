using Ember.Basic;
namespace Game.UI
{
    public partial class EUISoftGlassGMTestItem
    {
        public bool IsEnhanced => Enhanced.isOn;
        public override void OnInit() { base.OnInit(); Enhanced.onValueChanged.AddListener(OnEnhanced); Scroll.verticalNormalizedPosition=1; }
        public override void OnDispose() { Enhanced.onValueChanged.RemoveListener(OnEnhanced); base.OnDispose(); }
        private void OnEnhanced(bool value) => EmberDebug.LogEvent(LogTags.Game, "增强开关状态: " + value);
        public void SetEnhanced(bool value) => Enhanced.isOn=value;
    }
}
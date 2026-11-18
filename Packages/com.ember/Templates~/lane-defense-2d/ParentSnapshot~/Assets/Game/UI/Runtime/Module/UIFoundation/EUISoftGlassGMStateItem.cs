using Ember.Core;
namespace Game.UI
{
    public partial class EUISoftGlassGMStateItem
    {
        public void Refresh()
        {
            var launcher = GameLauncher.Instance;
            var state = launcher != null && launcher.Fsm != null && launcher.Fsm.Current != null ? launcher.Fsm.Current.Name : "—";
            if(State.text != state) State.text = state;
            UnityTime.text = "Unity  " + UnityEngine.Time.timeScale.ToString("0.00") + "×";
            EmberTime.text = "Ember  " + EmberTimeManager.Instance.TimeScale.ToString("0.00") + "×";
        }
    }
}
namespace Game.UI
{
    public partial class EUISoftGlassInputItem
    {
        #region 外部方法
        public TMPro.TMP_InputField Field => Input;
        public string Text { get => Input.text; set => Input.text = value ?? string.Empty; }
        public void SetError(string error) => Actions.SetError(error);
        public void SetReadOnly(bool value) => Actions.SetReadOnly(value);
        public void SetInteractable(bool value) => Actions.SetInteractable(value);
        #endregion
    }
}
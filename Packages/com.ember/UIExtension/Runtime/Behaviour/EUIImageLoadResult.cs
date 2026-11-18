namespace Ember.UIExtension
{
    public enum EUIImageLoadStatus { Success, Failed, Cancelled, Replaced, Cleared, ManualOverride, Disabled, Destroyed }
    public enum EUIImageLoadingDisplay { Placeholder, KeepPrevious }
    public readonly struct EUIImageLoadResult
    {
        public long OperationId { get; }
        public string Path { get; }
        public EUIImageLoadStatus Status { get; }
        public string Error { get; }
        public EUIImageLoadResult(long operationId,string path,EUIImageLoadStatus status,string error=null)
        {OperationId=operationId;Path=path;Status=status;Error=error;}
    }
}

// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

namespace Ember.Basic
{
    /// <summary>
    /// 日志文件上传器接口。
    /// 业务层实现此接口以对接自有服务端（HTTP、gRPC、FTP 等），
    /// 然后通过 <see cref="EmberFileLog.OnLogFileReady"/> 事件触发上传。
    ///
    /// 用法示例：
    /// <code>
    /// public class MyLogUploader : IEmberLogUploader
    /// {
    ///     public void Upload(string filePath)
    ///     {
    ///         // 异步上传到自有服务端
    ///         UploadToServerAsync(filePath);
    ///     }
    /// }
    ///
    /// // 在 GameLauncher 中注册
    /// EmberFileLog.OnLogFileReady += new MyLogUploader().Upload;
    /// </code>
    /// </summary>
    public interface IEmberLogUploader
    {
        /// <summary>
        /// 上传指定路径的日志文件。
        /// </summary>
        /// <param name="filePath">日志文件的完整路径。文件在调用此方法时已关闭，可安全读取。</param>
        void Upload(string filePath);
    }
}

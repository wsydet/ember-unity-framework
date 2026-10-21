using System;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Ember.Table.Editor
{
    /// <summary>构建只接受通过完整源表校验且与磁盘源表一致的产物。</summary>
    internal sealed class EmberTableBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 110;
        public void OnPreprocessBuild(BuildReport report)
        {
            var validation = EmberTablePipeline.ValidateAll();
            if (!validation.Succeeded)
                throw new BuildFailedException(string.Join("\n", validation.Diagnostics.Select(d => d.ToString())));
            foreach (var table in validation.Tables)
                if (!EmberTablePipeline.IsArtifactCurrent(table, out string reason))
                    throw new BuildFailedException(table.Definition.TableId + ": " + reason + " 请执行配表中心全量导出。");
        }
    }
}

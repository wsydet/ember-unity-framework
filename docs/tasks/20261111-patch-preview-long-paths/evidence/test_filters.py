from pathlib import Path
import re,json
r=Path('Packages/com.ember/Tests/EditMode')
names=[]
for cls in ['EmberTemplatePatchEditTests','EmberTemplateFileSystemEditTests']:
    for name in re.findall(r'public void (\w+)\(', (r/(cls+'.cs')).read_text(encoding='utf-8')):
        if name not in ['SetUp','TearDown','VersionGate_OnlyForwardPatch']: names.append('Ember.UI.Tests.'+cls+'.'+name)
for name in ['CommitPreparedTargets_AllTargetsShouldReplaceTogether','CommitPreparedTargets_RollbackFailureShouldPreserveOriginalErrorAndBackups','CopyDirectory_FolderMetadataShouldMaterializeVirtualEmptyDirectory','ParentSync_FaultAtAnyCommitStepShouldRestoreAllFormalTargets','ParentSync_SafeParentChangeShouldUpdateAssetsSnapshotAndMetadata','ParentSync_SemanticSceneShouldRerunInStageAndPreserveChildMeta','ParentSync_SemanticResultHashMismatchShouldWriteNothing']:
    names.append('Ember.UI.Tests.EmberTemplateTransactionEditTests.'+name)
for name in ['DeploymentAndIndependentUpdate_PreserveUtf8BytesAndLocalBackup','ChangesAfterPreview_RejectBeforeDeployment','DeploymentOrSkillOrRecordFailure_RestoresEverything','FirstDeploymentFailure_DoesNotLeaveSkillsOrDeploymentRecord','IndependentUpdate_FailureRollsBackEveryTarget','PathSafety_JunctionDiscoveryRoot_IsRejected']:
    names.append('Ember.UI.Tests.EmberTemplateSkillsEditTests.'+name)
p=Path(__file__).with_name('test-filters.json');p.write_text(json.dumps({'mode':'EditMode','test_names':names,'estimate':'30-90 seconds total; named new/directly affected methods only'},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(names))

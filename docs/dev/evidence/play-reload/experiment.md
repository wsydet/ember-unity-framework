# Play reload boundary experiments, 2026-10-24

Framework only, Unity 6000.5.4f1 / Ember 0.21.5 / HEAD ead31a7.
Windward untouched. FrameworkScene started to Main. One public RequestScriptReload reproduced IsInitialized=true/Fsm=null (before.json, after.json). Play was stopped.

Temporary Editor probe compiled through MCP and loaded type verified; Console errors=0. Probe recorded timing.txt. In beforeAssemblyReload it started async cleanup via Task.Delay(100), then requested isPlaying=false. Play stayed true through reload. New-domain delayCall stopped Play. Old-domain async completion never appeared.

Second experiment acquired LockReloadAssemblies inside beforeAssemblyReload and scheduled unlock in old-domain delayCall. Reload still occurred first; old unlock and async completion never appeared. Explicit UnlockReloadAssemblies sent through MCP after reload, probe disabled, Play=false confirmed. Probe Editor MVID changed c0d5cc3e-5b9a-4d10-ba70-aefe4c429d93 -> 98d31c40-abdd-42cd-9051-b0580ee6f85c before experiment 2, compilation/updating=false and Console errors=0.

Conclusion: beforeAssemblyReload is too late for a general async shutdown barrier. Stopping Play there does not await cleanup. Locking there did not postpone this reload. Existing PlayerData StopCore awaits a Unity-context pump before releasing storage ownership. Do not block the main thread waiting for that continuation or claim cancellation proves resource release. Full repair requires earlier reload admission or synchronous abort ownership contract. Not implemented or accepted yet. Temporary probe removed. These are failure/boundary evidence, not a repair or release.

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests
    {
        private async UniTask<PlayerController> Running()
        {
            var controller=Character();var candidate=await _player.PrepareAsync(controller,_input,_data);
            ((IEmberModule)_player).OnInit();_player.StartSession(candidate);return controller;
        }

        [UnityTest] public IEnumerator AnalogueStrengthDiagonalAndFacingAreIndependent()=>InputMotion().ToCoroutine();
        private async UniTask InputMotion()
        {
            var controller=await Running();
            Sticks(new Vector2(.45f,0),Vector2.up);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
            Assert.That(controller.Context.Move.magnitude,Is.GreaterThan(.1f).And.LessThan(.6f),
                $"active={_player.IsActive}, paused={_player.IsPaused}, available={controller.Context.ControlsAvailable}, device={_gamepad.leftStick.ReadValue()}, action={_input.Service.GetChannel("gameplay").ReadContinuous<Vector2>("Move")}");
            Assert.That(Vector2.Distance(controller.Context.Facing,Vector2.up),Is.LessThan(.001f));
            Sticks(Vector2.one,Vector2.zero);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
            Assert.That(controller.Context.Move.magnitude,Is.EqualTo(1).Within(.001f));
            Assert.That(controller.Context.Facing,Is.EqualTo(Vector2.up));
            for(var i=0;i<20;i++){((IEmberFixedUpdate)_player).FixedUpdate();await UniTask.WaitForFixedUpdate();}
            Assert.That(controller.Body.position.x,Is.GreaterThan(.2f));Assert.That(controller.Body.position.y,Is.GreaterThan(.2f));
            Assert.That(controller.Context.Velocity.magnitude,Is.LessThanOrEqualTo(6.01f));Assert.AreEqual("move",controller.CurrentState);
        }

        [UnityTest] public IEnumerator RealWallSweepAndPauseStopMotion()=>WallsAndPause().ToCoroutine();
        private async UniTask WallsAndPause()
        {
            var controller=await Running();var wall=Keep(new GameObject("Wall"));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,_scene);
            wall.transform.position=new Vector2(1.5f,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,20);
            Physics2D.SyncTransforms();Sticks(Vector2.right);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
            for(var i=0;i<30;i++){((IEmberFixedUpdate)_player).FixedUpdate();await UniTask.WaitForFixedUpdate();}
            Assert.That(controller.Body.position.x,Is.InRange(.5f,1.0f));
            var first=_player.Pause(this);var second=_player.Pause(wall);var position=controller.Body.position;
            first.Dispose();Assert.IsTrue(_player.IsPaused);Assert.IsFalse(controller.Body.simulated);
            ((IEmberFixedUpdate)_player).FixedUpdate();Assert.AreEqual(position,controller.Body.position);
            second.Dispose();Assert.IsFalse(_player.IsPaused);Assert.IsTrue(controller.Body.simulated);
            Assert.AreEqual(Vector2.zero,controller.Context.Velocity);
        }

        [UnityTest] public IEnumerator GatingClearsBufferedButtonsAndHeldPressDoesNotReappear()=>InputGate().ToCoroutine();
        private async UniTask InputGate()
        {
            var controller=await Running();Press(_keyboard.jKey);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();Assert.IsTrue(controller.Context.AttackPending);
            var deny=_input.Service.Request(this,200,new GameInputRule("gameplay",GameInputPermission.Deny));
            Assert.IsFalse(controller.Context.AttackPending);deny.Dispose();((IEmberUpdate)_player).Update();
            Assert.IsFalse(controller.Context.AttackPending);Release(_keyboard.jKey);UnityEngine.InputSystem.InputSystem.Update();Press(_keyboard.jKey);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
            Assert.IsTrue(controller.Context.AttackPending);((IEmberFixedUpdate)_player).FixedUpdate();Assert.IsFalse(controller.Context.AttackPending);
        }

        [UnityTest] public IEnumerator CapturedSchemeAndAnotherHostRemainIndependent()=>SchemeIsolation().ToCoroutine();
        private async UniTask SchemeIsolation()
        {
            var controller=await Running();_scheme.Configure(_graph,12);
            Assert.AreEqual(6,controller.Context.Scheme.Speed);
            var module=new PlayerModule();module.SetSchemeForNextSession(_scheme);
            var candidate=await module.PrepareAsync(Character(),_input,_data);
            try{((IEmberModule)module).OnInit();module.StartSession(candidate);Assert.AreEqual(12,module.Controller.Context.Scheme.Speed);
                Assert.AreNotSame(controller.Context.Scheme.Graph,module.Controller.Context.Scheme.Graph);}
            finally{((IEmberModule)module).OnDestroy();candidate.Dispose();}
            Assert.IsTrue(controller.IsRunning);
        }

        [UnityTest] public IEnumerator FourSidesAndNarrowPassageUseActualColliderGeometry()=>CollisionMatrix().ToCoroutine();
        private async UniTask CollisionMatrix()
        {
            var controller=await Running();
            foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
            {
                var wall=Keep(new GameObject("Directional Wall"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,_scene);
                wall.transform.position=direction*1.5f;wall.AddComponent<BoxCollider2D>().size=direction.x!=0?new Vector2(.2f,10):new Vector2(10,.2f);
                controller.Body.position=Vector2.zero;Physics2D.SyncTransforms();Sticks(direction);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
                for(var i=0;i<20;i++){((IEmberFixedUpdate)_player).FixedUpdate();await UniTask.WaitForFixedUpdate();}
                Assert.That(Vector2.Dot(controller.Body.position,direction),Is.InRange(.5f,1f));
                using(_player.Pause(this)){}UnityEngine.Object.DestroyImmediate(wall);
            }
            controller.Body.position=new Vector2(0,-2);
            var sides=new GameObject[2];
            for(var side=0;side<2;side++)
            {
                sides[side]=Keep(new GameObject("Narrow Passage"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sides[side],_scene);
                sides[side].transform.position=new Vector2(side==0?-.5f:.5f,1);sides[side].AddComponent<BoxCollider2D>().size=new Vector2(.2f,4);
            }
            Physics2D.SyncTransforms();Sticks(Vector2.up);UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();
            for(var i=0;i<20;i++){((IEmberFixedUpdate)_player).FixedUpdate();await UniTask.WaitForFixedUpdate();}
            Assert.That(controller.Body.position.y,Is.LessThanOrEqualTo(-1.49f),"比身体窄的入口必须拒绝穿过");
        }
    }
}

// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System.Runtime.CompilerServices;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// 驱动 IAsyncStateMachine 的轻量 Runner。
    /// </summary>
    internal class MoveNextRunner<TStateMachine> where TStateMachine : IAsyncStateMachine
    {
        public TStateMachine StateMachine;

        public void Run()
        {
            StateMachine.MoveNext();
        }
    }
}

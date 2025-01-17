﻿using EngineNS.Bricks.Terrain.CDLOD;
using EngineNS.GamePlay.Scene;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Bricks.PhysicsCore
{
    public class TtPhySceneDesc : AuxPtrType<PhySceneDesc>
    {
        public TtPhySceneDesc(PhySceneDesc self)
        {
            mCoreObject = self;
        }
    }
    public class TtPhyScene : AuxPtrType<PhyScene>
    {
        public TtPhyScene(PhyScene self)
        {
            mCoreObject = self;
        }
        public bool IsPxFetchingPose { get; protected set; }
        private static Profiler.TimeScope ScopeTickSimulate = Profiler.TimeScopeManager.GetTimeScope(typeof(TtPhyScene), "Tick.Simulate");
        private static Profiler.TimeScope ScopeTickUpdateActor = Profiler.TimeScopeManager.GetTimeScope(typeof(TtPhyScene), "Tick.UpdateActor");
        public unsafe void Tick(float elapsedSecond)
        {
            var elapse = elapsedSecond;
            if (elapse > 1.0F || elapse <= 0.0F)
                return;
            uint scratchMemBlockSize = 0;
            void* scratchMemBlock = (void*)0;
            uint errorState = 0;
            //这里要考虑elapse过久，分多次tick处理
            const float StepTime = 1 / 20.0f;
            int count = (int)(elapse / StepTime);
            float fm = elapse % StepTime;
            using (new Profiler.TimeScopeHelper(ScopeTickSimulate))
            {
                for (int i = 0; i < count; i++)
                {
                    mCoreObject.Simulate(StepTime, scratchMemBlock, scratchMemBlockSize, true);
                    mCoreObject.FetchResults(false, &errorState);
                }
                if (fm > 0)
                {
                    mCoreObject.Simulate(fm, scratchMemBlock, scratchMemBlockSize, true);
                    mCoreObject.FetchResults(false, &errorState);
                }
            }

            using (new Profiler.TimeScopeHelper(ScopeTickUpdateActor))
            {
                uint activeActorCount = 0;
                try
                {
                    mCoreObject.LockRead();
                    IsPxFetchingPose = true;
                    var actors = mCoreObject.UpdateActorTransforms(ref activeActorCount);
                    for (uint i = 0; i < activeActorCount; ++i)
                    {
                        var actor = mCoreObject.GetActor(actors, i);
                        if (actor.IsValidPointer)
                        {
                            actor.UpdateTransform();

                            var csActor = TtPhyActor.GetActor(actor);
                            if (csActor != null && csActor.TagNode != null)
                            {
                                csActor.TagNode.Placement.Position = csActor.mCoreObject.mPosition.AsDVector();
                                csActor.TagNode.Placement.Quat = csActor.mCoreObject.mRotation;
                            }
                        }
                    }
                }
                finally
                {
                    IsPxFetchingPose = false;
                    mCoreObject.UnlockRead();
                }
            }   
        }
        public TtPhyController CreateBoxController(in PhyBoxControllerDesc desc)
        {
            var self = mCoreObject.CreateBoxController(desc);
            if (self.IsValidPointer == false)
                return null;
            return new TtPhyController(self);
        }
        public TtPhyController CreateCapsuleController(in PhyCapsuleControllerDesc desc)
        {
            var self = mCoreObject.CreateCapsuleController(desc);
            if (self.IsValidPointer == false)
                return null;
            return new TtPhyController(self);
        }        
    }

    public class UPhySceneMember : IMemberTickable
    {
        private GamePlay.Scene.UScene HostScene;
        private TtPhyScene mPxScene;
        public TtPhyScene PxScene
        {
            get
            {
                if (mPxScene == null)
                {
                    var task = Initialize(HostScene);
                }
                return mPxScene;
            }
        }
        public async System.Threading.Tasks.Task<bool> Initialize(object host)
        {
            await Thread.TtAsyncDummyClass.DummyFunc();

            var scene = host as GamePlay.Scene.UScene;
            if (scene == null)
                return false;
            HostScene = scene;

            var pc = TtEngine.Instance.PhyModule.PhyContext;
            if (pc == null)
                return false;
            
            var desc = pc.CreateSceneDesc();
            desc.mCoreObject.SetFlags(PhySceneFlag.eENABLE_ACTIVE_ACTORS);
            var gravity = new Vector3(0, -9.8f, 0);
            desc.mCoreObject.SetGravity(in gravity);
            //desc.mCoreObject.SetOnTrigger()
            mPxScene = pc.CreateScene(desc);

            return true;
        }
        public void Cleanup(object host)
        {
            mPxScene?.Dispose();
            mPxScene = null;
        }
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTick = Profiler.TimeScopeManager.GetTimeScope(typeof(UPhySceneMember), nameof(TickLogic));
        System.Threading.AutoResetEvent PxSceneTickEndEvent = new System.Threading.AutoResetEvent(false);
        public void TickLogic(object host, float ellapse)
        {
            TickPxScene(ellapse);
            //var task = TtEngine.Instance.EventPoster.RunOn((state) =>
            //{
            //    TickPxScene(ellapse);
            //    return true;
            //}, Thread.Async.EAsyncTarget.TPools, null, PxSceneTickEndEvent);
            //PxSceneTickEndEvent?.WaitOne();

            //var hostNode = host as UNode;
            //hostNode.GetWorld().RegAfterTickAction(() =>
            //{
            //    PxSceneTickEndEvent?.WaitOne();
            //});
        }
        private void TickPxScene(float ellapse)
        {
            using (new Profiler.TimeScopeHelper(ScopeTick))
            {
                PxScene?.Tick(ellapse * 0.001f);
            }
        }
        public void OnHostNotify(object host, in FHostNotify notify)
        {

        }
    }
}

namespace EngineNS.GamePlay.Scene
{
    public partial class UScene
    {
        public Bricks.PhysicsCore.UPhySceneMember PxSceneMB { get; } = new Bricks.PhysicsCore.UPhySceneMember();
    }
}

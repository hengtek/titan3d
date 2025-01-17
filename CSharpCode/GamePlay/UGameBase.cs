﻿using EngineNS.Bricks.PhysicsCore.SceneNode;
using EngineNS.GamePlay.Camera;
using EngineNS.GamePlay.Scene;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.GamePlay
{
    [Macross.UMacross]
    public partial class UMacrossGame
    {
        [Rtti.Meta]
        public virtual async System.Threading.Tasks.Task<bool> BeginPlay(UGameInstance host)
        {
            await host.InitViewportSlate(TtEngine.Instance.Config.MainRPolicyName);

            return true;
        }
        [Rtti.Meta]
        public virtual void Tick(UGameInstance host, float elapsedMillisecond)
        {

        }
        [Rtti.Meta]
        public virtual void BeginDestroy(UGameInstance host)
        {
            host.FinalViewportSlate();
        }
    }
    [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.NoMacrossCreate)]
    public partial class UGameInstance : UModuleHost<UGameInstance>, ITickable
    {
        public int GetTickOrder()
        {
            return -1;
        }
        public virtual void TickLogic(float ellapse)
        {
            WorldViewportSlate?.TickLogic(ellapse);
        }
        public virtual void TickRender(float ellapse)
        {
            
        }
        public void TickBeginFrame(float ellapse)
        {

        }
        public virtual void TickSync(float ellapse)
        {
            WorldViewportSlate?.TickSync(ellapse);
        }

        [Rtti.Meta]
        public UGameViewportSlate WorldViewportSlate { get; } = new UGameViewportSlate(true);
        [Rtti.Meta]
        public Graphics.Pipeline.UCamera DefaultCamera 
        {
            get => WorldViewportSlate.RenderPolicy.DefaultCamera;
        }
        Macross.UMacrossGetter<UMacrossGame> mMcObject;
        public Macross.UMacrossGetter<UMacrossGame> McObject
        {
            get
            {
                if (mMcObject == null)
                    mMcObject = Macross.UMacrossGetter<UMacrossGame>.NewInstance();
                return mMcObject;
            }
        }
        protected override UGameInstance GetHost()
        {
            return this;
        }
        public virtual async System.Threading.Tasks.Task<bool> BeginPlay()
        {
            if (McObject == null)
                return false;
            if (McObject.Get() == null)
                return false;
            return await McObject.Get().BeginPlay(this);
        }
        public virtual void Tick(float elapsedMillisecond)
        {
            McObject?.Get()?.Tick(this, elapsedMillisecond);
        }
        public virtual void BeginDestroy()
        {
            McObject?.Get()?.BeginDestroy(this);
        }
        [Rtti.Meta]
        public async System.Threading.Tasks.Task InitViewportSlate(
            [RName.PGRName(FilterExts = Bricks.RenderPolicyEditor.TtRenderPolicyAsset.AssetExt)]
            RName rPolicy, 
            float zMin = 0, float zMax = 1)
        {
            await WorldViewportSlate.Initialize(null, rPolicy, zMin, zMax);
            WorldViewportSlate.RenderPolicy.DisableShadow = false;
            TtEngine.Instance.GfxDevice.SlateApplication.NativeWindow.RegEventProcessor(WorldViewportSlate);
        }
        [Rtti.Meta]
        public void FinalViewportSlate()
        {
            TtEngine.Instance.GfxDevice.SlateApplication.NativeWindow.UnregEventProcessor(WorldViewportSlate);
            TtEngine.RootFormManager.UnregRootForm(WorldViewportSlate);
        }

        [Rtti.Meta]
        public async System.Threading.Tasks.Task<GamePlay.Scene.UScene> LoadScene(
            [RName.PGRName(FilterExts = GamePlay.Scene.UScene.AssetExt)]
            RName mapName)
        {
            var viewport = this.WorldViewportSlate;
            var world = viewport.World;

            var scene = await GamePlay.Scene.UScene.LoadScene(world, mapName);
            if (scene != null)
            {
                world.Root.ClearChildren();
                world.Root.SetStyle(GamePlay.Scene.UNode.ENodeStyles.VisibleFollowParent);
                scene.Parent = world.Root;

                //await CreateCharacter(WorldViewportSlate.World, WorldViewportSlate.World.Root);
                //await CreateCharacter(scene);
                //await CreateSpereActor(scene);
                //await CreateBoxActor(scene);
                //world.CameraOffset = DVector3.Zero;
                return scene;
            }
            return null;
        }
        [Rtti.Meta]
        public GamePlay.Scene.Actor.UActor ChiefPlayer { get; set; }
        [Rtti.Meta]
        public async System.Threading.Tasks.Task CreateCharacter(Scene.UScene scene)
        {
            EngineNS.GamePlay.Scene.UNode root = scene;
            var playerData = new EngineNS.GamePlay.Scene.Actor.UActor.UActorData();
            ChiefPlayer = new EngineNS.GamePlay.Scene.Actor.UActor();
            await ChiefPlayer.InitializeNode(scene.World, playerData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            ChiefPlayer.Parent = root;
            ChiefPlayer.NodeData.Name = "UActor";
            ChiefPlayer.HitproxyType = EngineNS.Graphics.Pipeline.UHitProxy.EHitproxyType.None;
            ChiefPlayer.IsCastShadow = true;
            ChiefPlayer.SetStyle(EngineNS.GamePlay.Scene.UNode.ENodeStyles.VisibleFollowParent);
            ChiefPlayer.Placement.SetTransform(new DVector3(100, 30, 50), Vector3.One, Quaternion.Identity);

            var meshData1 = new EngineNS.GamePlay.Scene.UMeshNode.UMeshNodeData();
            meshData1.MeshName = RName.GetRName("utest/puppet/mesh/puppet.ums");
            meshData1.MdfQueueType = EngineNS.Rtti.UTypeDesc.TypeOf(typeof(EngineNS.Graphics.Mesh.UMdfSkinMesh)).TypeString;
            meshData1.AtomType = EngineNS.Rtti.UTypeDesc.TypeOf(typeof(EngineNS.Graphics.Mesh.TtMesh.TtAtom)).TypeString;
            var meshNode1 = new EngineNS.GamePlay.Scene.UMeshNode();
            await meshNode1.InitializeNode(scene.World, meshData1, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            meshNode1.NodeData.Name = "Robot1";
            meshNode1.Parent = ChiefPlayer;
            meshNode1.Placement.SetTransform(new DVector3(0.0f), new Vector3(0.01f), Quaternion.Identity);
            meshNode1.HitproxyType = EngineNS.Graphics.Pipeline.UHitProxy.EHitproxyType.FollowParent;
            meshNode1.IsAcceptShadow = false;
            meshNode1.IsCastShadow = true;

            //var sapnd = new EngineNS.Animation.SceneNode.USkeletonAnimPlayNode.USkeletonAnimPlayNodeData();
            //sapnd.Name = "PlayAnim";
            //sapnd.AnimatinName = RName.GetRName("utest/puppet/animation/w2_walk_aim_f_loop_ip.animclip");
            //await EngineNS.Animation.SceneNode.USkeletonAnimPlayNode.AddSkeletonAnimPlayNode(scene.World, meshNode1, sapnd, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UIdentityPlacement));
            var sapnd = new EngineNS.Animation.SceneNode.TtBlendSpaceAnimPlayNode.TtBlendSpaceAnimPlayNodeData();
            sapnd.Name = "PlayAnim";
            sapnd.AnimatinName = RName.GetRName("utest/puppet/animation/w2_walk_aim_f_loop_ip.animclip");
            sapnd.OverrideAsset = true;
            sapnd.Axises.Add(new EngineNS.Animation.Asset.BlendSpace.TtBlendSpace_Axis("Speed", -3, 3));
            sapnd.Axises.Add(new EngineNS.Animation.Asset.BlendSpace.TtBlendSpace_Axis("V"));
            sapnd.Points.Add(new EngineNS.Animation.SceneNode.FBlendSpacePoint(RName.GetRName("utest/puppet/animation/w2_stand_aim_idle_ip.animclip"), Vector3.Zero));
            sapnd.Points.Add(new EngineNS.Animation.SceneNode.FBlendSpacePoint(RName.GetRName("utest/puppet/animation/w2_run_f_loop_ip.animclip"), new Vector3(3, 0, 0)));
            await EngineNS.Animation.SceneNode.TtBlendSpaceAnimPlayNode.AddBlendSpace2DAnimPlayNode(scene.World, meshNode1, sapnd, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UIdentityPlacement));

            var characterController = new EngineNS.GamePlay.Controller.UCharacterController();
            await characterController.InitializeNode(scene.World, new EngineNS.GamePlay.Scene.UNodeData(), EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            characterController.Parent = root;
            characterController.ControlledCharacter = ChiefPlayer;

            var springArm = new EngineNS.GamePlay.Camera.UCameraSpringArm();
            var springArmData = new EngineNS.GamePlay.Camera.UCameraSpringArm.UCameraSpringArmData();
            springArmData.TargetOffset = DVector3.Up * 1.0f;
            springArmData.ArmLength = 5;
            await springArm.InitializeNode(scene.World, springArmData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));

            springArm.Parent = ChiefPlayer;

            characterController.CameraControlNode = springArm;

            var camera = new EngineNS.GamePlay.Camera.UCamera();
            await camera.InitializeNode(WorldViewportSlate.World, new EngineNS.GamePlay.Scene.UNodeData(), EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            camera.Parent = springArm;
            camera.Camera = WorldViewportSlate.RenderPolicy.DefaultCamera;

            var phyControl = new TtCapsulePhyControllerNode();
            var phyNodeData = new TtCapsulePhyControllerNode.TtCapsulePhyControllerNodeData();
            phyNodeData.Height = 1.5f;
            phyNodeData.Radius = 0.5f;
            await phyControl.InitializeNode(scene.World, phyNodeData, Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            phyControl.Parent = ChiefPlayer;

            var movement = new EngineNS.GamePlay.Movemnet.UCharacterMovement();
            movement.EnableGravity = true;
            await movement.InitializeNode(scene.World, new EngineNS.GamePlay.Scene.UNodeData() { Name = "Movement" }, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            movement.Parent = ChiefPlayer;

            characterController.MovementNode = movement;
        }

        public async System.Threading.Tasks.Task CreateSpereActor(Scene.UScene scene)
        {
            EngineNS.GamePlay.Scene.UNode root = scene;
            var playerData = new EngineNS.GamePlay.Scene.Actor.UActor.UActorData();
            var actor = new EngineNS.GamePlay.Scene.Actor.UActor();
            await actor.InitializeNode(scene.World, playerData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            actor.Parent = root;
            actor.NodeData.Name = "UActor";
            actor.HitproxyType = EngineNS.Graphics.Pipeline.UHitProxy.EHitproxyType.None;
            actor.IsCastShadow = true;
            actor.SetStyle(EngineNS.GamePlay.Scene.UNode.ENodeStyles.VisibleFollowParent);
            actor.Placement.SetTransform(new DVector3(100, 10, 50), Vector3.One, Quaternion.Identity);

            var phyControl = new TtPhySphereCollisionNode();
            var phyNodeData = new TtPhySphereCollisionNode.UPhySphereCollisionNodeData();
            phyNodeData.Radius = 0.5f;
            await phyControl.InitializeNode(scene.World, phyNodeData, Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            phyControl.Parent = actor;
        }
        public async System.Threading.Tasks.Task CreateBoxActor(Scene.UScene scene)
        {
            EngineNS.GamePlay.Scene.UNode root = scene;
            var playerData = new EngineNS.GamePlay.Scene.Actor.UActor.UActorData();
            var actor = new EngineNS.GamePlay.Scene.Actor.UActor();
            await actor.InitializeNode(scene.World, playerData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            actor.Parent = root;
            actor.NodeData.Name = "UActor";
            actor.HitproxyType = EngineNS.Graphics.Pipeline.UHitProxy.EHitproxyType.None;
            actor.IsCastShadow = true;
            actor.SetStyle(EngineNS.GamePlay.Scene.UNode.ENodeStyles.VisibleFollowParent);
            actor.Placement.SetTransform(new DVector3(100, 2, 50), Vector3.One, Quaternion.Identity);

            var phyControl = new TtPhyBoxCollisionNode();
            var phyNodeData = new TtPhyBoxCollisionNode.UPhyBoxCollisionNodeData();
            phyNodeData.PhyActorType = EPhyActorType.PAT_Static;
            await phyControl.InitializeNode(scene.World, phyNodeData, Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
            phyControl.Parent = actor;
        }
    }
}

namespace EngineNS
{
    public partial class TtEngine
    {
        [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.Unserializable | Rtti.MetaAttribute.EMetaFlags.MacrossReadOnly)]
        public GamePlay.UGameInstance GameInstance
        {
            get;
            set;
        }
    }
}
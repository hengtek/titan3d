﻿using EngineNS.Bricks.RenderPolicyEditor;
using EngineNS.GamePlay;
using EngineNS.Graphics.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Bricks.Procedure.Node
{
    public class TtErosionIncWaterShading : Graphics.Pipeline.Shader.UComputeShadingEnv
    {
        public override Vector3ui DispatchArg
        {
            get => new Vector3ui(32, 32, 1);
        }
        public TtErosionIncWaterShading()
        {
            CodeName = RName.GetRName("Shaders/Bricks/Procedure/Erosion/IncWater.compute", RName.ERNameType.Engine);
            MainName = "CS_IncWaterMain";

            this.UpdatePermutation();
        }
        protected override void EnvShadingDefines(in FPermutationId id, NxRHI.UShaderDefinitions defines)
        {
            base.EnvShadingDefines(in id, defines);
        }
        public override void OnDrawCall(NxRHI.UComputeDraw drawcall, Graphics.Pipeline.URenderPolicy policy)
        {
            var node = drawcall.TagObject as TtErosionIncWaterNode;
            var uav = policy.AttachmentCache.FindAttachement(node.WaterPinInOut).Uav;
            drawcall.BindUav("WaterTexture", uav);
        }
    }
    public class TtErosionIncWaterNode : Graphics.Pipeline.TtRenderGraphNode
    {
        public Graphics.Pipeline.TtRenderGraphPin WaterPinInOut = Graphics.Pipeline.TtRenderGraphPin.CreateInputOutput("Water", false, EPixelFormat.PXF_R32_FLOAT);

        public TtErosionIncWaterShading ShadingEnv;
        public NxRHI.UCommandList mCmdList;
        private NxRHI.UComputeDraw mDrawcall;
        public TtErosionIncWaterNode()
        {
            Name = "ErosionIncWater";
        }
        public override void Dispose()
        {
            CoreSDK.DisposeObject(ref mDrawcall);
            CoreSDK.DisposeObject(ref mCmdList);
        }
        public override void InitNodePins()
        {
            AddInputOutput(WaterPinInOut, NxRHI.EBufferType.BFT_SRV | NxRHI.EBufferType.BFT_UAV);
            WaterPinInOut.IsAllowInputNull = true;

            base.InitNodePins();
        }
        public override async Task Initialize(URenderPolicy policy, string debugName)
        {
            await base.Initialize(policy, debugName);
            ShadingEnv = UEngine.Instance.ShadingEnvManager.GetShadingEnv<TtErosionIncWaterShading>();

            var rc = UEngine.Instance.GfxDevice.RenderContext;
            mCmdList = rc.CreateCommandList();
            mDrawcall = rc.CreateComputeDraw();
            mDrawcall.TagObject = this;
        }
        public unsafe override void TickLogic(UWorld world, URenderPolicy policy, bool bClear)
        {
            using (new NxRHI.TtCmdListScope(mCmdList))
            {
                ShadingEnv.SetDrawcallDispatch(this, policy, mDrawcall, 1, 1, 1, true);
                mCmdList.PushGpuDraw(mDrawcall);
                mCmdList.PushAction(static (EngineNS.NxRHI.ICommandList cmd, void* arg1) =>
                {
                    
                }, IntPtr.Zero.ToPointer());
                mCmdList.FlushDraws();
            }
            
            UEngine.Instance.GfxDevice.RenderContext.GpuQueue.ExecuteCommandList(mCmdList, NxRHI.EQueueType.QU_Compute);            
        }
    }

    [Bricks.CodeBuilder.ContextMenu("Ending", "PGC\\Erosion\\Ending", UPolicyGraph.RGDEditorKeyword)]
    public class TtErosionEndingNode : Graphics.Pipeline.Common.TtEndingNode
    {
        public Graphics.Pipeline.TtRenderGraphPin HeightPinIn = Graphics.Pipeline.TtRenderGraphPin.CreateInput("Height");
        public TtErosionEndingNode()
        {
            Name = "ErosionEndingNode";
        }
        public override void InitNodePins()
        {
            AddInput(HeightPinIn, NxRHI.EBufferType.BFT_SRV);
            HeightPinIn.IsAllowInputNull = true;
        }
        public override void Dispose()
        {
            CoreSDK.DisposeObject(ref mFinishFence);
        }
        public override async Task Initialize(URenderPolicy policy, string debugName)
        {
            await base.Initialize(policy, debugName);

            var rc = UEngine.Instance.GfxDevice.RenderContext;
            var fenceDesc = new NxRHI.FFenceDesc();
            mFinishFence = rc.CreateFence(in fenceDesc, "TtErosionEndingNode");
        }
        public NxRHI.UFence mFinishFence;
        public override void TickLogic(UWorld world, URenderPolicy policy, bool bClear)
        {
            UEngine.Instance.GfxDevice.RenderContext.GpuQueue.IncreaseSignal(mFinishFence, NxRHI.EQueueType.QU_Compute);
        }
    }

    [Bricks.CodeBuilder.ContextMenu("GpuErosion", "Float1\\GpuErosion", UPgcGraph.PgcEditorKeyword)]
    public class TtGpuErosionNode : Node.UAnyTypeMonocular
    {
        RName mPolicyName;
        [Rtti.Meta]
        [RName.PGRName(FilterExts = URenderPolicyAsset.AssetExt)]
        public RName PolicyName
        {
            get
            {
                return mPolicyName;
            }
            set
            {
                var action = async () =>
                {
                    var policy = URenderPolicyAsset.LoadAsset(value).CreateRenderPolicy(null, "ErosionEndingNode");
                    await policy.Initialize(null);
                    Policy = policy;
                    mPolicyName = value;
                };
                action();
            }
        }
        public bool IsCapture { get; set; } = false;
        Graphics.Pipeline.URenderPolicy Policy;
        public unsafe override bool OnProcedure(UPgcGraph graph)
        {
            if (Policy == null)
                return false;
            var Input = graph.BufferCache.FindBuffer(SrcPin);
            var Output = graph.BufferCache.FindBuffer(ResultPin);
            var buffer = Input.GetGpuTexture2D<float>();
            //Input.Upload2GpuTexture2D()
            var incWater = Policy.FindNode<TtErosionIncWaterNode>("ErosionIncWater");
            var waterAttachement = Policy.AttachmentCache.ImportAttachment(incWater.WaterPinInOut);
            waterAttachement.SetImportedBuffer(buffer);
            var ending = Policy.RootNode as TtErosionEndingNode;
            var processor = new TtPgcGpuProcessor();
            processor.Policy = Policy;
            //processor.OnBufferRemoved = (Graphics.Pipeline.TtRenderGraphNode node, Graphics.Pipeline.TtRenderGraphPin pin, Graphics.Pipeline.TtAttachBuffer bf)=>
            //{
            //    if (ending == node && pin == ending.HeightPinIn)
            //    {

            //    }
            //};

            if (IsCapture)
            {
                UEngine.Instance.GfxDevice.RenderCmdQueue.CaptureRenderDocFrame = true;
                UEngine.Instance.GfxDevice.RenderCmdQueue.BeginFrameCapture();
            }
            processor.Process();
            if (IsCapture)
            {
                UEngine.Instance.GfxDevice.RenderCmdQueue.EndFrameCapture(this.Name);
            }

            ending.mFinishFence.WaitToExpect();
            //buffer.Fetch
            return true;
        }
    }
}
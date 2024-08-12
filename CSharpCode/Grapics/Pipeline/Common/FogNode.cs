﻿using EngineNS.GamePlay;
using EngineNS.Graphics.Mesh;
using EngineNS.Graphics.Pipeline.Shader;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Common
{
    public partial class TtFogShading : Shader.UGraphicsShadingEnv
    {
        public TtFogShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/FogShading.cginc", RName.ERNameType.Engine);

            TypeFog = this.PushPermutation<Graphics.Pipeline.URenderPolicy.ETypeAA>("ENV_FOGFACTOR_TYPE", (int)Graphics.Pipeline.URenderPolicy.ETypeFog.TypeCount);

            TypeFog.SetValue((int)Graphics.Pipeline.URenderPolicy.ETypeFog.None);

            this.UpdatePermutation();
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position,
                NxRHI.EVertexStreamType.VST_UV,};
        }
        protected override void EnvShadingDefines(in FPermutationId id, NxRHI.UShaderDefinitions defines)
        {
            defines.AddDefine("TypeFog_None", (int)Graphics.Pipeline.URenderPolicy.ETypeFog.None);
            defines.AddDefine("TypeFog_ExpHeight", (int)Graphics.Pipeline.URenderPolicy.ETypeFog.ExpHeight);
        }

        public UPermutationItem TypeFog
        {
            get;
            set;
        }
        public unsafe override void OnDrawCall(NxRHI.ICommandList cmd, NxRHI.UGraphicDraw drawcall, URenderPolicy policy, Mesh.TtMesh.TtAtom atom)
        {
            base.OnDrawCall(cmd, drawcall, policy, atom);

            var pipelinPolicy = policy.TagObject as URenderPolicy;

            var aaNode = drawcall.TagObject as TtFogNode;
            if (aaNode == null)
                aaNode = pipelinPolicy.FindFirstNode<Common.TtFogNode>();

            switch (pipelinPolicy.TypeFog)
            {
                case URenderPolicy.ETypeFog.None:
                    OnDrawcallEHF(drawcall, pipelinPolicy, aaNode);
                    break;
                case URenderPolicy.ETypeFog.ExpHeight:
                    OnDrawcallEHF(drawcall, pipelinPolicy, aaNode);
                    break;
            }
        }
    }
    [EGui.Controls.PropertyGrid.PGCategoryFilters(ExcludeFilters = new string[] { "Misc" })]
    public partial class TtFogNode : USceenSpaceNode
    {
        public TtRenderGraphPin ColorPinIn = TtRenderGraphPin.CreateInput("Color");
        public TtRenderGraphPin DepthPinIn = TtRenderGraphPin.CreateInput("Depth");
        public TtRenderGraphPin NoisePinIn = TtRenderGraphPin.CreateInput("Noise");
        public TtFogNode()
        {
            Name = "FogNode";
            TtFogNode_InitExpHeight();
        }
        public override void InitNodePins()
        {
            AddInput(ColorPinIn, NxRHI.EBufferType.BFT_SRV);

            AddInput(DepthPinIn, NxRHI.EBufferType.BFT_SRV);

            AddInput(NoisePinIn, NxRHI.EBufferType.BFT_SRV);

            base.InitNodePins();
        }
        public TtFogShading mBasePassShading;
        public override UGraphicsShadingEnv GetPassShading(TtMesh.TtAtom atom)
        {
            return mBasePassShading;
        }
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            await base.Initialize(policy, debugName);
            mBasePassShading = await UEngine.Instance.ShadingEnvManager.GetShadingEnv<TtFogShading>();
        }
        public override void FrameBuild(URenderPolicy policy)
        {
            base.FrameBuild(policy);
        }
        public override void BeforeTickLogic(URenderPolicy policy)
        {
            if (policy.TypeFog == URenderPolicy.ETypeFog.None)
            {
                this.MoveAttachment(ColorPinIn, ResultPinOut);
                return;
            }

            var buffer = this.FindAttachBuffer(ColorPinIn);
            if (buffer != null)
            {
                if (ResultPinOut.Attachement.Format != buffer.BufferDesc.Format)
                {
                    this.CreateGBuffers(policy, buffer.BufferDesc.Format);
                    ResultPinOut.Attachement.Format = buffer.BufferDesc.Format;    
                }
            }
        }
        public NxRHI.UCbView CBShadingEnv;
        public override void TickLogic(UWorld world, URenderPolicy policy, bool bClear)
        {
            if (policy.TypeFog == URenderPolicy.ETypeFog.None)
            {
                return;
            }
            base.TickLogic(world, policy, bClear);
        }
        public override void TickSync(URenderPolicy policy)
        {
            if (policy.TypeFog == URenderPolicy.ETypeFog.None)
            {
                return;
            }
            base.TickSync(policy);
            switch (policy.TypeFog)
            {
                case URenderPolicy.ETypeFog.None:
                    break;
                case URenderPolicy.ETypeFog.ExpHeight:
                    TickSyncEHF(policy);
                    break;
            }
        }
    }
}

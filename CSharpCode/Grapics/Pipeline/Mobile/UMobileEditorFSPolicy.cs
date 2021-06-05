﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Mobile
{
    public class UEditorFinalShading : Shader.UShadingEnv
    {
        public UEditorFinalShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/Mobile/MobileCopyEditor.cginc", RName.ERNameType.Engine);

            var disable_AO = new MacroDefine();//0
            disable_AO.Name = "ENV_DISABLE_AO";
            disable_AO.Values.Add("0");
            disable_AO.Values.Add("1");
            MacroDefines.Add(disable_AO);

            var disable_Sunshaft = new MacroDefine();//1
            disable_Sunshaft.Name = "ENV_DISABLE_SUNSHAFT";
            disable_Sunshaft.Values.Add("0");
            disable_Sunshaft.Values.Add("1");
            MacroDefines.Add(disable_Sunshaft);

            var disable_Bloom = new MacroDefine();//2
            disable_Bloom.Name = "ENV_DISABLE_BLOOM";
            disable_Bloom.Values.Add("0");
            disable_Bloom.Values.Add("1");
            MacroDefines.Add(disable_Bloom);

            var disable_Hdr = new MacroDefine();//2
            disable_Hdr.Name = "ENV_DISABLE_HDR";
            disable_Hdr.Values.Add("0");
            disable_Hdr.Values.Add("1");
            MacroDefines.Add(disable_Hdr);

            UpdatePermutation();

            uint permuationId;
            var values = new List<string>();
            values.Add("0");//disable_AO = 0
            values.Add("1");//disable_Sunshaft = 1
            values.Add("1");//disable_Bloom = 1
            values.Add("1");//disable_Hdr = 1
            this.GetPermutation(values, out permuationId);
            this.CurrentPermutationId = permuationId;
        }
        public UMobileEditorFSPolicy Manager;
        public unsafe override void OnBuildDrawCall(RHI.CDrawCall drawcall)
        {
        }
        public unsafe override void OnDrawCall(RHI.CDrawCall drawcall, IRenderPolicy policy, Mesh.UMesh mesh)
        {
            var gpuProgram = drawcall.Effect.ShaderProgram;
            var index = drawcall.mCoreObject.FindSRVIndex("gBaseSceneView");
            drawcall.mCoreObject.BindSRVAll(index, Manager.GBuffers.GBufferSRV[0].mCoreObject);

            index = drawcall.mCoreObject.FindSRVIndex("gPickedTex");
            drawcall.mCoreObject.BindSRVAll(index, Manager.PickedProxiableManager.PickHollowProcessor.GBuffers.GBufferSRV[0].mCoreObject);
        }
    }
    public class UEditorFinalProcessor : Common.USceenSpaceProcessor
    {
        public UMobileEditorFSPolicy Manager;
        public override async System.Threading.Tasks.Task Initialize(IRenderPolicy policy, Shader.UShadingEnv shading, float x, float y)
        {
            await base.Initialize(policy, shading, x, y);

            var hollowShading = shading as UEditorFinalShading;
            hollowShading.Manager = policy as UMobileEditorFSPolicy;
        }
    }
    public class UMobileEditorFSPolicy : UMobileFSPolicy
    {
        public override RHI.CShaderResourceView GetFinalShowRSV()
        {
            return EditorFinalProcessor.GBuffers.GBufferSRV[0];
        }
        public Common.UPickedProxiableManager PickedProxiableManager { get; protected set; } = new Common.UPickedProxiableManager();
        public UEditorFinalProcessor EditorFinalProcessor = new UEditorFinalProcessor();
        public UGraphicsBuffers GHitproxyBuffers { get; protected set; } = new UGraphicsBuffers();
        Common.UHitproxyShading mHitproxyShading;
        public UDrawBuffers HitproxyPass = new UDrawBuffers();
        public RenderPassDesc HitproxyPassDesc = new RenderPassDesc();
        private RHI.CFence mReadHitproxyFence;
        #region GetHitproxy
        private Support.CBlobObject mHitProxyData = new Support.CBlobObject();
        unsafe private ITexture2D* mReadableHitproxyTexture;
        public ITexture2D ReadableHitproxyTexture
        {
            get
            {
                unsafe
                {
                    return new ITexture2D(mReadableHitproxyTexture);
                }
            }
        }
        UInt32 mHitCheckRegion = 2;
        private Int32 Clamp(Int32 ValueIn, Int32 MinValue, Int32 MaxValue)
        {
            return ValueIn < MinValue ? MinValue : ValueIn < MaxValue ? ValueIn : MaxValue;
        }
        public IProxiable GetHitproxy(UInt32 MouseX, UInt32 MouseY)
        {
            var id = GetHitProxyID(MouseX, MouseY);
            if (id == 0)
                return null;
            return UEngine.Instance.GfxDevice.HitproxyManager.FindProxy(id);
        }

        public UInt32 GetHitProxyID(UInt32 MouseX, UInt32 MouseY)
        {
            unsafe
            {
                lock (this)
                {
                    return GetHitProxyIDImpl(MouseX, MouseY);
                }
            }
        }
        private unsafe UInt32 GetHitProxyIDImpl(UInt32 MouseX, UInt32 MouseY)
        {
            if (mReadableHitproxyTexture == (ITexture2D*)0)
                return 0;

            byte* pPixelData = (byte*)mHitProxyData.mCoreObject.GetData();
            if (pPixelData == (byte*)0)
                return 0;
            var pBitmapDesc = (PixelDesc*)pPixelData;
            pPixelData += sizeof(PixelDesc);

            IntColor* HitProxyIdArray = (IntColor*)pPixelData;

            Int32 RegionMinX = (Int32)(MouseX - mHitCheckRegion);
            Int32 RegionMinY = (Int32)(MouseY - mHitCheckRegion);
            Int32 RegionMaxX = (Int32)(MouseX + mHitCheckRegion);
            Int32 RegionMaxY = (Int32)(MouseY + mHitCheckRegion);

            RegionMinX = Clamp(RegionMinX, 0, (Int32)pBitmapDesc->Width - 1);
            RegionMinY = Clamp(RegionMinY, 0, (Int32)pBitmapDesc->Height - 1);
            RegionMaxX = Clamp(RegionMaxX, 0, (Int32)pBitmapDesc->Width - 1);
            RegionMaxY = Clamp(RegionMaxY, 0, (Int32)pBitmapDesc->Height - 1);

            Int32 RegionSizeX = RegionMaxX - RegionMinX + 1;
            Int32 RegionSizeY = RegionMaxY - RegionMinY + 1;

            if (RegionSizeX > 0 && RegionSizeY > 0)
            {                
                if (HitProxyIdArray == null)
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Error, "@Graphic", $"HitProxyError: Null Ptr!!!", "");
                    return 0;
                }

                int max = pBitmapDesc->Width * pBitmapDesc->Height;

                UInt32 HitProxyArrayIdx = 0;
                for (Int32 PointY = RegionMinY; PointY < RegionMaxY; PointY++)
                {
                    IntColor* TempHitProxyIdCache = &HitProxyIdArray[PointY * pBitmapDesc->Width];
                    for (Int32 PointX = RegionMinX; PointX < RegionMaxX; PointX++)
                    {
                        var hitId = UHitProxy.ConvertCpuTexColorToHitProxyId(TempHitProxyIdCache[PointX]);
                        if (hitId != 0)
                            return hitId;
                        HitProxyArrayIdx++;
                    }
                }
            }
            return 0;
        }
        #endregion
        public override async System.Threading.Tasks.Task Initialize(float x, float y)
        {
            await base.Initialize(x, y);

            var rc = UEngine.Instance.GfxDevice.RenderContext;
            HitproxyPass.Initialize(rc);

            GHitproxyBuffers.Initialize(1, EPixelFormat.PXF_D24_UNORM_S8_UINT, (uint)x, (uint)y);
            GHitproxyBuffers.CreateGBuffer(0, EPixelFormat.PXF_R8G8B8A8_UNORM, (uint)x, (uint)y);
            GHitproxyBuffers.SwapChainIndex = -1;
            GHitproxyBuffers.TargetViewIdentifier = GBuffers.TargetViewIdentifier;
            GHitproxyBuffers.Camera = GBuffers.Camera;

            HitproxyPassDesc.mFBLoadAction_Color = FrameBufferLoadAction.LoadActionClear;
            HitproxyPassDesc.mFBStoreAction_Color = FrameBufferStoreAction.StoreActionStore;
            HitproxyPassDesc.mFBClearColorRT0 = new Color4(0, 0, 0, 0);
            HitproxyPassDesc.mFBLoadAction_Depth = FrameBufferLoadAction.LoadActionClear;
            HitproxyPassDesc.mFBStoreAction_Depth = FrameBufferStoreAction.StoreActionStore;
            HitproxyPassDesc.mDepthClearValue = 1.0f;
            HitproxyPassDesc.mFBLoadAction_Stencil = FrameBufferLoadAction.LoadActionClear;
            HitproxyPassDesc.mFBStoreAction_Stencil = FrameBufferStoreAction.StoreActionStore;
            HitproxyPassDesc.mStencilClearValue = 0u;

            //mReadHitproxyFence = rc.CreateFence();
            //unsafe
            //{
            //    var cmdlist = BasePass.DrawCmdList.mCoreObject;
            //    cmdlist.Signal(mReadHitproxyFence.mCoreObject, 0);
            //}

            await PickedProxiableManager.Initialize(this, x, y);
            await EditorFinalProcessor.Initialize(this, UEngine.Instance.ShadingEnvManager.GetShadingEnv<UEditorFinalShading>(), x, y);
        }
        public override void OnResize(float x, float y)
        {
            base.OnResize(x, y);

            if (GHitproxyBuffers != null)
            {
                GHitproxyBuffers.OnResize(x, y);
            }

            PickedProxiableManager.OnResize(x, y);
            if (EditorFinalProcessor != null)
                EditorFinalProcessor.OnResize(x, y);
        }
        public unsafe override void Cleanup()
        {
            PickedProxiableManager?.Cleanup();
            PickedProxiableManager = null;

            EditorFinalProcessor?.Cleanup();
            EditorFinalProcessor = null;

            mReadHitproxyFence?.Dispose();
            mReadHitproxyFence = null;

            if (mReadableHitproxyTexture != (ITexture2D*)0)
            {
                ReadableHitproxyTexture.NativeSuper.NativeSuper.NativeSuper.Release();
                mReadableHitproxyTexture = (ITexture2D*)0;
            }

            GHitproxyBuffers?.Cleanup();
            GHitproxyBuffers = null;
            
            base.Cleanup();
        }
        //如果本渲染策略不提供指定的EShadingType，那么UAtom内的s对应的Drawcall就不会产生出来
        public override Shader.UShadingEnv GetPassShading(EShadingType type, Mesh.UMesh mesh)
        {
            if (mesh.Tag != null)
            {
                if (type == EShadingType.BasePass)
                {
                    return mesh.Tag.GetPassShading(type, mesh);
                }
            }
            switch (type)
            {
                case EShadingType.BasePass:
                    if (mBasePassShading == null)
                        mBasePassShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<Pipeline.Mobile.UBasePassOpaque>();
                    return mBasePassShading;
                case EShadingType.HitproxyPass:
                    if (mHitproxyShading == null)
                        mHitproxyShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<Pipeline.Common.UHitproxyShading>();
                    return mHitproxyShading;
                case EShadingType.Picked:
                    return PickedProxiableManager.PickedShading;
            }
            return null;
        }
        //如果产生了对应的ShadingType的Drawcall，则会callback到这里设置一些这个shading的特殊参数
        public override void OnDrawCall(Pipeline.IRenderPolicy.EShadingType shadingType, RHI.CDrawCall drawcall, Mesh.UMesh mesh)
        {
            mesh.MdfQueue.OnDrawCall(shadingType, drawcall, this, mesh);

            if (mesh.Tag != null)
            {
                var shading = mesh.Tag.GetPassShading(shadingType, mesh) as Mobile.UBasePassShading;
                if (shading != null)
                    shading.OnDrawCall(shadingType, drawcall, this, mesh);
            }
            else
            {
                if (shadingType == EShadingType.BasePass)
                    mBasePassShading.OnDrawCall(shadingType, drawcall, this, mesh);
                else if (shadingType == EShadingType.HitproxyPass)
                    return;
                else if (shadingType == EShadingType.Picked)
                    return;
                //else if (shadingType == EShadingType.HitproxyPass)
                //    mHitproxyShading.OnDrawCall(shadingType, drawcall, this, mesh);
            }
        }
        int DelayFrame = 0;
        bool CanDrawHitproxy = false;
        public unsafe override void TickLogic()
        {
            BasePass.ClearMeshDrawPassArray();
            BasePass.SetViewport(GBuffers.ViewPort);

            var cmdlist_hp = HitproxyPass.DrawCmdList.mCoreObject;
            cmdlist_hp.ClearMeshDrawPassArray();
            cmdlist_hp.SetViewport(GHitproxyBuffers.ViewPort.mCoreObject);
            
            foreach (var i in VisibleMeshes)
            {
                if (i.Atoms == null)
                    continue;

                for (int j = 0; j < i.Atoms.Length; j++)
                {
                    {
                        var drawcall = i.GetDrawCall(GBuffers, (uint)j, this, EShadingType.BasePass);
                        if (drawcall != null)
                        {
                            GBuffers.SureCBuffer(drawcall.Effect, "UMobileEditorFSPolicy");
                            drawcall.BindGBuffer(GBuffers);

                            var layer = i.Atoms[j].Material.RenderLayer;
                            BasePass.PushDrawCall(layer, drawcall);
                        }
                    }

                    if (CanDrawHitproxy && i.IsDrawHitproxy)
                    {
                        var hpDrawcall = i.GetDrawCall(GHitproxyBuffers, (uint)j, this, EShadingType.HitproxyPass);
                        if (hpDrawcall != null)
                        {
                            GHitproxyBuffers.SureCBuffer(hpDrawcall.Effect, "UMobileEditorFSPolicy.HitproxyBuffers");
                            hpDrawcall.BindGBuffer(GHitproxyBuffers);
                            
                            cmdlist_hp.PushDrawCall(hpDrawcall.mCoreObject);
                        }
                    }
                }
            }

            BasePass.BuildRenderPass(ref PassDesc, GBuffers.FrameBuffers);

            if (CanDrawHitproxy)
            {
                cmdlist_hp.BeginCommand();
                cmdlist_hp.BeginRenderPass(ref HitproxyPassDesc, GHitproxyBuffers.FrameBuffers.mCoreObject);
                cmdlist_hp.BuildRenderPass(0);
                cmdlist_hp.EndRenderPass();
                cmdlist_hp.EndCommand();

                fixed (ITexture2D** ppTexture = &mReadableHitproxyTexture)
                {
                    cmdlist_hp.CreateReadableTexture2D(ppTexture, GHitproxyBuffers.GBufferSRV[0].mCoreObject, GHitproxyBuffers.FrameBuffers.mCoreObject);
                }
                //cmdlist_hp.Signal(mReadHitproxyFence.mCoreObject, 0);
                CanDrawHitproxy = false;
                //DelayFrame = 0;
            }

            PickedProxiableManager.TickLogic(this);

            EditorFinalProcessor.TickLogic();
        }
        public unsafe override void TickRender()
        {
            var rc = UEngine.Instance.GfxDevice.RenderContext;
            BasePass.Commit(rc);

            var cmdlist_hp = HitproxyPass.CommitCmdList.mCoreObject;
            cmdlist_hp.Commit(rc.mCoreObject);

            DelayFrame++;
            if (DelayFrame >= 10)
            {
                //if (mReadHitproxyFence.mCoreObject.IsCompletion())
                {
                    if (mReadableHitproxyTexture != (ITexture2D*)0)
                    {
                        void* pData;
                        uint rowPitch;
                        uint depthPitch;
                        if (ReadableHitproxyTexture.Map(cmdlist_hp, 0, &pData, &rowPitch, &depthPitch) != 0)
                        {
                            lock (this)
                            {
                                ReadableHitproxyTexture.BuildImageBlob(mHitProxyData.mCoreObject, pData, rowPitch);
                            }
                            ReadableHitproxyTexture.Unmap(cmdlist_hp, 0);
                        }
                    }
                }
                DelayFrame = 0;
                CanDrawHitproxy = true;
            }

            PickedProxiableManager.TickRender(this);
            EditorFinalProcessor.TickRender();
        }
        public unsafe override void TickSync()
        {
            DelayFrame++;
            if (DelayFrame >= 10)
            {
                var cmdlist_hp = HitproxyPass.CommitCmdList.mCoreObject;
                //if (mReadHitproxyFence.mCoreObject.IsCompletion())
                {
                    if (mReadableHitproxyTexture != (ITexture2D*)0)
                    {
                        void* pData;
                        uint rowPitch;
                        uint depthPitch;
                        if (ReadableHitproxyTexture.Map(cmdlist_hp, 0, &pData, &rowPitch, &depthPitch) != 0)
                        {
                            ReadableHitproxyTexture.BuildImageBlob(mHitProxyData.mCoreObject, pData, rowPitch);
                            ReadableHitproxyTexture.Unmap(cmdlist_hp, 0);
                        }
                    }
                }
                DelayFrame = 0;
                CanDrawHitproxy = true;
            }

            BasePass.SwapBuffer();
            HitproxyPass.SwapBuffer();
            PickedProxiableManager.TickSync(this);
            EditorFinalProcessor.TickSync();

            GBuffers?.Camera?.mCoreObject.UpdateConstBufferData(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, 1);
        }
    }
}
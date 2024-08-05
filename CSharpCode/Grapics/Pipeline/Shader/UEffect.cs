﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Shader
{
    public class UEffect
    {
        public const string AssetExt = ".effect";

        public override string ToString()
        {
            return base.ToString();
        }
        public class UEffectDesc : IO.ISerializer
        {
            public void OnPreRead(object tagObject, object hostObject, bool fromXml)
            {

            }

            public void OnPropertyRead(object tagObject, PropertyInfo prop, bool fromXml)
            {

            }
            public const uint CurrentEffectVersion = 4;
            [Rtti.Meta()]
            public uint EffectVersion { get; set; } = CurrentEffectVersion;
            [Rtti.Meta()]
            public Hash160 GlobalEnvHash { get; set; }
            [Rtti.Meta()]
            public Hash160 CodeHash { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public Hash160 MaterialHash { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public Hash160 MdfQueueHash { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public RName MaterialName { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public string MdfQueueType { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public Shader.UShadingEnv.FPermutationId PermutationId { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public string ShadingType { get; set; }
            [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
            public uint InputStreams { get; set; }

            public Hash160 EffectHash;
        }
        public UEffectDesc Desc { get; set; } = new UEffectDesc();
        public NxRHI.UShaderEffect ShaderEffect { get; private set; }
        public NxRHI.UShaderDesc DescVS { get; private set; }
        public NxRHI.UShaderDesc DescPS { get; private set; }
        public UShadingEnv ShadingEnv { get; internal set; }

        internal UEffect UnsafeCloneForEditor()
        {
            var result = new UEffect();
            result.ShaderEffect = ShaderEffect;
            result.DescVS = DescVS;
            result.DescPS = DescPS;
            result.ShadingEnv = ShadingEnv;
            var meta = Rtti.TtClassMetaManager.Instance.GetMeta(Rtti.UTypeDescGetter<UEffectDesc>.TypeDesc);
            meta.CopyObjectMetaField(result.Desc, Desc);
            return result;
        }

        public unsafe void SaveTo(Hash160 hash)
        {
            var path = UEngine.Instance.FileManager.GetPath(IO.TtFileManager.ERootDir.Cache, IO.TtFileManager.ESystemDir.Effect);
            var file = path + hash.ToString() + UEffect.AssetExt;
            var xnd = new IO.TtXndHolder("UEffect", 0, 0);

            var descAttr = new XndAttribute(xnd.RootNode.mCoreObject.GetOrAddAttribute("Desc", 0, 0));
            using (var ar = descAttr.GetWriter(30))
            {
                ar.Write(Desc);
            }

            var vsNode = xnd.mCoreObject.NewNode("VSCode", 0, 0);
            xnd.RootNode.mCoreObject.AddNode(vsNode);
            DescVS.mCoreObject.SaveXnd(vsNode);
            CoreSDK.PtrType_Release(vsNode);

            var psNode = xnd.mCoreObject.NewNode("PSCode", 0, 0);
            xnd.RootNode.mCoreObject.AddNode(psNode);
            DescPS.mCoreObject.SaveXnd(psNode);
            CoreSDK.PtrType_Release(psNode);

            xnd.SaveXnd(file);
        }
        public static async Thread.Async.TtTask<UEffect> LoadEffect(Hash160 hash, UShadingEnv shading, UMaterial material, TtMdfQueueBase mdf)
        {
            var rc = UEngine.Instance.GfxDevice.RenderContext;
            var path = UEngine.Instance.FileManager.GetPath(IO.TtFileManager.ERootDir.Cache, IO.TtFileManager.ESystemDir.Effect);
            var file = path + hash.ToString() + UEffect.AssetExt;

            UEffect result = null;
            using (var xnd = IO.TtXndHolder.LoadXnd(file))
            {
                if (xnd == null)
                    return null;

                XndAttribute descAttr = new XndAttribute();
                XndNode vsNode = new XndNode();
                XndNode psNode = new XndNode();
                unsafe
                {
                    descAttr = xnd.RootNode.mCoreObject.TryGetAttribute("Desc");
                    if (descAttr.IsValidPointer == false)
                        return null;
                    vsNode = xnd.RootNode.mCoreObject.TryGetChildNode("VSCode");
                    if (vsNode.IsValidPointer == false)
                        return null;
                    psNode = xnd.RootNode.mCoreObject.TryGetChildNode("PSCode");
                    if (psNode.IsValidPointer == false)
                        return null;
                }

                result = await UEngine.Instance.EventPoster.Post((state) =>
                {
                    var effect = new UEffect();
                    IO.ISerializer desc;
                    using (var ar = descAttr.GetReader(effect))
                    {
                        IO.SerializerHelper.Read(ar, out desc, effect);
                    }
                    var effectDesc = desc as UEffectDesc;
                    if (effectDesc == null)
                        return null;

                    if (effectDesc.EffectVersion != UEffectDesc.CurrentEffectVersion)
                        return null;

                    var shadingCode = Editor.ShaderCompiler.TtShaderCodeManager.Instance.GetShaderCode(shading.CodeName);
                    if (rc.GlobalEnvHash != effectDesc.GlobalEnvHash ||
                            shadingCode.CodeHash != effectDesc.CodeHash ||
                            material.GetHash() != effectDesc.MaterialHash ||
                            mdf.GetHash() != effectDesc.MdfQueueHash)
                    {
                        return null;
                    }

                    effect.Desc = effectDesc;

                    unsafe
                    {
                        effect.DescVS = new NxRHI.UShaderDesc(NxRHI.EShaderType.SDT_VertexShader);
                        if (effect.DescVS.mCoreObject.LoadXnd(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, vsNode) == false)
                            return null;
                        effect.DescPS = new NxRHI.UShaderDesc(NxRHI.EShaderType.SDT_PixelShader);
                        if (effect.DescPS.mCoreObject.LoadXnd(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, psNode) == false)
                            return null;
                        effect.DescVS.mCoreObject.Type = NxRHI.EShaderType.SDT_VertexShader;
                        effect.DescPS.mCoreObject.Type = NxRHI.EShaderType.SDT_PixelShader;
                    }
                    return effect;
                }, Thread.Async.EAsyncTarget.AsyncIO);

                if (result == null)
                    return null;
            }

            NxRHI.UShader VertexShader = null;
            NxRHI.UShader PixelShader = null;
            if (UEngine.Instance.GfxDevice.RenderContext.RhiType == NxRHI.ERhiType.RHI_GL)
            {
                bool created = await UEngine.Instance.EventPoster.Post((state) =>
                {
                    VertexShader = rc.CreateShader(result.DescVS);
                    if (VertexShader == null)
                        return false;
                    PixelShader = rc.CreateShader(result.DescPS);
                    if (PixelShader == null)
                        return false;
                    return true;
                }, Thread.Async.EAsyncTarget.Render);
                if (created == false)
                    return null;
            }
            else
            {
                VertexShader = rc.CreateShader(result.DescVS);
                if (VertexShader == null)
                    return null;
                PixelShader = rc.CreateShader(result.DescPS);
                if (PixelShader == null)
                    return null;
            }

            NxRHI.UInputLayout InputLayout = null;
            unsafe
            {
                var layoutDesc = new NxRHI.UInputLayoutDesc(IMesh.CreateInputLayoutDesc(result.Desc.InputStreams));
                layoutDesc.mCoreObject.SetShaderDesc(result.DescVS.mCoreObject);
                InputLayout = UEngine.Instance.GfxDevice.RenderContext.CreateInputLayout(layoutDesc); //UEngine.Instance.GfxDevice.InputLayoutManager.GetPipelineState(rc, layoutDesc);
                if (InputLayout == null)
                {
                    System.Diagnostics.Debug.Assert(false);
                }

                result.ShaderEffect = rc.CreateShaderEffect(VertexShader, PixelShader);
                result.ShaderEffect.mCoreObject.BindInputLayout(InputLayout.mCoreObject);
            }

            var dbg_name = $"{shading},{material.AssetName},{Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType())}";
            VertexShader.SetDebugName($"VS:{dbg_name}");
            PixelShader.SetDebugName($"PS:{dbg_name}");
            result.ShaderEffect.DebugName = dbg_name;
            if (await LinkShaders(result) == false)
                return null;

            result.Desc.EffectHash = hash;
            result.ShadingEnv = shading;
            return result;
        }
        public static async Thread.Async.TtTask<UEffect> CreateEffect(UShadingEnv shading, UShadingEnv.FPermutationId permutationId, UMaterial material, TtMdfQueueBase mdf)
        {
            var rc = UEngine.Instance.GfxDevice.RenderContext;

            var result = new UEffect();
            var shadingCode = Editor.ShaderCompiler.TtShaderCodeManager.Instance.GetShaderCode(shading.CodeName);
            if (shadingCode == null)
                return null;

            result.Desc.GlobalEnvHash = Hash160.CreateHash160(rc.GlobalEnvDefines.ToString());
            result.Desc.CodeHash = shadingCode.CodeHash;
            result.Desc.MaterialHash = material.GetHash();
            result.Desc.MdfQueueHash = mdf.GetHash();
            result.Desc.MaterialName = material.AssetName;
            result.Desc.MdfQueueType = Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType());
            result.Desc.ShadingType = Rtti.UTypeDescManager.Instance.GetTypeStringFromType(shading.GetType());
            result.ShadingEnv = shading;

            var defines = new NxRHI.UShaderDefinitions();
            shading.GetShaderDefines(in permutationId, defines);

            var cfg = UEngine.Instance.Config;
            result.DescVS = await UEngine.Instance.EventPoster.Post((state) =>
            {
                var compilier = new Editor.ShaderCompiler.UHLSLCompiler();
                compilier.MdfQueue = mdf;
                return compilier.CompileShader(shading.CodeName.Address, "VS_Main", NxRHI.EShaderType.SDT_VertexShader,
                    shading, material, mdf.GetType(), defines, null, null, UEngine.Instance.Config.IsDebugShader);
            }, Thread.Async.EAsyncTarget.AsyncIO);
            if (result.DescVS == null)
                return null;

            result.DescPS = await UEngine.Instance.EventPoster.Post((state) =>
            {
                var compilier = new Editor.ShaderCompiler.UHLSLCompiler();
                compilier.MdfQueue = mdf;
                return compilier.CompileShader(shading.CodeName.Address, "PS_Main", NxRHI.EShaderType.SDT_PixelShader, 
                    shading, material, mdf.GetType(), defines, null, null, UEngine.Instance.Config.IsDebugShader);
            }, Thread.Async.EAsyncTarget.AsyncIO);
            if (result.DescPS == null)
                return null;

            NxRHI.UShader VertexShader = null;
            NxRHI.UShader PixelShader = null;
            if (UEngine.Instance.GfxDevice.RenderContext.RhiType == NxRHI.ERhiType.RHI_GL)
            {
                bool created = await UEngine.Instance.EventPoster.Post((state) =>
                {
                    VertexShader = rc.CreateShader(result.DescVS);
                    if (VertexShader == null)
                        return false;
                    PixelShader = rc.CreateShader(result.DescPS);
                    if (PixelShader == null)
                        return false;
                    return true;
                }, Thread.Async.EAsyncTarget.Render);
                if (created == false)
                    return null;
            }
            else
            {
                VertexShader = rc.CreateShader(result.DescVS);
                if (VertexShader == null)
                    return null;
                PixelShader = rc.CreateShader(result.DescPS);
                if (PixelShader == null)
                    return null;
            }

            NxRHI.UInputLayout InputLayout = null;
            unsafe
            {
                uint inputStreams = 0;
                mdf.mCoreObject.GetInputStreams(ref inputStreams);
                var layoutDesc = new NxRHI.UInputLayoutDesc(IMesh.CreateInputLayoutDesc(inputStreams));
                layoutDesc.mCoreObject.SetShaderDesc(result.DescVS.mCoreObject);
                InputLayout = UEngine.Instance.GfxDevice.RenderContext.CreateInputLayout(layoutDesc); //UEngine.Instance.GfxDevice.InputLayoutManager.GetPipelineState(rc, layoutDesc);

                result.Desc.InputStreams = inputStreams;

                result.ShaderEffect = rc.CreateShaderEffect(VertexShader, PixelShader);
                result.ShaderEffect.mCoreObject.BindInputLayout(InputLayout.mCoreObject);
            }
            if (await LinkShaders(result) == false)
                return null;

            VertexShader.SetDebugName($"VS:{shading},{material.AssetName},{Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType())}");
            PixelShader.SetDebugName($"PS:{shading},{material.AssetName},{Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType())}");
            result.ShaderEffect.DebugName = $"{shading},{material.AssetName},{Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType())}";
            return result;
        }
        public async Thread.Async.TtTask<bool> RefreshEffect(UMaterial material)
        {
            {
                var path = UEngine.Instance.FileManager.GetPath(IO.TtFileManager.ERootDir.Cache, IO.TtFileManager.ESystemDir.Effect);
                var file = path + this.Desc.EffectHash.ToString() + UEffect.AssetExt;
                if (IO.TtFileManager.FileExists(file))
                    IO.TtFileManager.DeleteFile(file);
            }

            var rc = UEngine.Instance.GfxDevice.RenderContext;

            var defines = new NxRHI.UShaderDefinitions();
            var type = Rtti.UTypeDescManager.Instance.GetTypeFromString(Desc.ShadingType);
            if (type == null)
                return false;
            var shading = UEngine.Instance.ShadingEnvManager.GetShadingEnv(type);
            if (shading == null)
                return false;
            shading.GetShaderDefines(Desc.PermutationId, defines);

            var mdfType = Rtti.UTypeDesc.TypeOf(Desc.MdfQueueType);
            if (mdfType == null)
                return false;
            var mdf = Rtti.UTypeDescManager.CreateInstance(mdfType) as Pipeline.Shader.TtMdfQueueBase;
            if (mdf == null)
                return false;

            mdf.Initialize(null);
            this.Desc.MaterialHash = material.GetHash();
            this.Desc.MdfQueueHash = mdf.GetHash();

            var descVS = await UEngine.Instance.EventPoster.Post((state) =>
            {
                var compilier = new Editor.ShaderCompiler.UHLSLCompiler();
                compilier.MdfQueue = mdf;
                return compilier.CompileShader(shading.CodeName.Address, "VS_Main", NxRHI.EShaderType.SDT_VertexShader, 
                    shading, material, mdfType.SystemType, defines, null, null, UEngine.Instance.Config.IsDebugShader);
            }, Thread.Async.EAsyncTarget.AsyncIO);
            if (descVS == null)
                return false;

            var descPS = await UEngine.Instance.EventPoster.Post((state) =>
            {
                var compilier = new Editor.ShaderCompiler.UHLSLCompiler();
                compilier.MdfQueue = mdf;
                return compilier.CompileShader(shading.CodeName.Address, "PS_Main", NxRHI.EShaderType.SDT_PixelShader, 
                    shading, material, mdfType.SystemType, defines, null, null, UEngine.Instance.Config.IsDebugShader);
            }, Thread.Async.EAsyncTarget.AsyncIO);
            if (descPS == null)
                return false;


            this.DescVS = descVS;
            this.DescPS = descPS;
            
            var VertexShader = rc.CreateShader(DescVS);
            if (VertexShader == null)
                return false;
            var PixelShader = rc.CreateShader(DescPS);
            if (PixelShader == null)
                return false;

            NxRHI.UInputLayout InputLayout = null;
            unsafe
            {
                uint inputSteams = 0;
                mdf.mCoreObject.GetInputStreams(ref inputSteams);

                var layoutDesc = new NxRHI.UInputLayoutDesc(IMesh.CreateInputLayoutDesc(inputSteams));
                layoutDesc.mCoreObject.SetShaderDesc(descVS.mCoreObject);
                InputLayout = UEngine.Instance.GfxDevice.RenderContext.CreateInputLayout(layoutDesc); //UEngine.Instance.GfxDevice.InputLayoutManager.GetPipelineState(rc, layoutDesc);
                
                Desc.InputStreams = inputSteams;
            }

            ShaderEffect = rc.CreateShaderEffect(VertexShader, PixelShader);
            ShaderEffect.mCoreObject.BindInputLayout(InputLayout.mCoreObject);
            if (await LinkShaders(this) == false)
                return false;

            var dbg_name = $"{shading},{material.AssetName},{Rtti.UTypeDescManager.Instance.GetTypeStringFromType(mdf.GetType())}";
            VertexShader.SetDebugName($"VS:{dbg_name}");
            PixelShader.SetDebugName($"PS:{dbg_name}");
            ShaderEffect.DebugName = dbg_name;
            return true;
        }
        private static async System.Threading.Tasks.Task<bool> LinkShaders(UEffect result)
        {
            //result.GraphicsEffect.mCoreObject.LinkShaders();
            return true;
        }

        public UCoreShaderBinder.UShaderResourceIndexer mBindIndexer;
        public UCoreShaderBinder.UShaderResourceIndexer BindIndexer
        {
            get
            {
                if (mBindIndexer == null)
                {
                    mBindIndexer = new UCoreShaderBinder.UShaderResourceIndexer();
                    mBindIndexer.UpdateBindResouce(this.ShaderEffect);
                }
                return mBindIndexer;
            }
        }
    }
    public class UEffectManager
    {
        public UEffect DummyEffect;
        public async System.Threading.Tasks.Task<bool> Initialize(UGfxDevice device)
        {
            DummyEffect = await this.GetEffect(UEngine.Instance.ShadingEnvManager.GetShadingEnv<UDummyShading>(), device.MaterialManager.ScreenMaterial, new Mesh.UMdfStaticMesh());

            if (DummyEffect == null)
            {
                DummyEffect = await UEffect.CreateEffect(UEngine.Instance.ShadingEnvManager.GetShadingEnv<UDummyShading>(),
                   new UShadingEnv.FPermutationId(0), device.MaterialManager.ScreenMaterial, new Mesh.UMdfStaticMesh());
            }
            if (DummyEffect == null)
                return false;

            device.CoreShaderBinder.UpdateIndex(DummyEffect.ShaderEffect);
            return true;
        }
        public void Cleanup()
        {
            System.Diagnostics.Debug.Assert(mCreatingSession.mSessions.Count == 0);
            Effects.Clear();
        }
        private Thread.UAwaitSessionManager<Hash160, UEffect> mCreatingSession = new Thread.UAwaitSessionManager<Hash160, UEffect>();
        public Dictionary<Hash160, UEffect> Effects { get; } = new Dictionary<Hash160, UEffect>();
        public Dictionary<Hash160, NxRHI.UComputeEffect> ComputeEffects { get; } = new Dictionary<Hash160, NxRHI.UComputeEffect>();
        public NxRHI.UComputeEffect TryGetComputeEffect(Hash160 hash)
        {
            lock (Effects)
            {
                NxRHI.UComputeEffect result;
                if (ComputeEffects.TryGetValue(hash, out result))
                    return result;

                return null;
            }
        }
        public UEffect TryGetEffect(Hash160 hash)
        {
            lock (Effects)
            {
                UEffect result;
                if (Effects.TryGetValue(hash, out result))
                    return result;

                return null;
            }
        }
        public static Hash160 GetShaderHash(UShadingEnv shading, UMaterial material, TtMdfQueueBase mdf)
        {
            //var caps = new IRenderContextCaps();
            //UEngine.Instance.GfxDevice.RenderContext.mCoreObject.GetRenderContextCaps(ref caps);
            //return Hash160.CreateHash160($"{shading},{material.AssetName},{mdf},{caps}");
            return Hash160.CreateHash160($"{UEngine.Instance.GfxDevice.RenderContext.GlobalEnvHash},{shading},{material.AssetName},{mdf}");
        }
        public Dictionary<Hash160, UEffect> MaterialEditingEffects { get; } = new Dictionary<Hash160, UEffect>();
        public async Thread.Async.TtTask<UEffect> GetEffect(UShadingEnv shading, UMaterial material, TtMdfQueueBase mdf)
        {
            UEffect result = null;
            Hash160 hash = new Hash160();
            if (material.IsEditingMaterial)
            {
                hash = GetShaderHash(shading, material, mdf);
                if (MaterialEditingEffects.TryGetValue(hash, out result))
                {
                    if (result.Desc.MaterialHash != material.MaterialHash)
                    {
                        await result.RefreshEffect(material);
                    }
                    return result;
                }
            }
            result = await GetEffectImpl(shading, material, mdf);
            if (material.IsEditingMaterial)
            {
                UEffect nr = null;
                if (MaterialEditingEffects.TryGetValue(hash, out nr))
                {
                    return nr;
                }
                result = result.UnsafeCloneForEditor();
                MaterialEditingEffects.Add(hash, result);
                return result;
            }
            return result;
        }
        public void RemoveEditingMaterial(RName material)
        {
            var rmv = new List<Hash160>();
            foreach (var i in MaterialEditingEffects)
            {
                if (i.Value.Desc.MaterialName == material)
                {
                    rmv.Add(i.Key);
                }
            }
            foreach (var i in rmv)
            {
                MaterialEditingEffects.Remove(i);
            }
        }
        private async Thread.Async.TtTask<UEffect> GetEffectImpl(UShadingEnv shading, UMaterial material, TtMdfQueueBase mdf)
        {
            UEffect result = null;
            //if (material.IsEditingMaterial)
            //{
            //    result = await UEffect.CreateEffect(shading, shading.mCurrentPermutationId, material, mdf);
            //    return result;
            //}

            var hash = GetShaderHash(shading, material, mdf);
            result = TryGetEffect(hash);
            if (result != null)
            {
                if (result.Desc.MaterialHash != material.MaterialHash)
                {
                    await result.RefreshEffect(material);
                }
                return result;
            }

            bool isNewSession;
            var session = mCreatingSession.GetOrNewSession(hash, out isNewSession);
            if (isNewSession == false)
            {
                return await session.Await();
            }

            try
            {
                result = await UEffect.LoadEffect(hash, shading, material, mdf);
                if (result != null)
                {
                    if (Effects.ContainsKey(hash) == false)
                    {
                        lock (Effects)
                        {
                            Effects[hash] = result;
                        }
                        return result;
                    }
                    else
                    {
                        result = Effects[hash];
                        return result;
                    }
                }

                result = await UEffect.CreateEffect(shading, shading.mCurrentPermutationId, material, mdf);
                if (result != null)
                {
                    result.Desc.EffectHash = hash;
                    result.Desc.PermutationId = shading.mCurrentPermutationId;
                    //if (material.IsEditingMaterial == false)
                    {
                        await UEngine.Instance.EventPoster.Post((state) =>
                        {
                            result.SaveTo(hash);
                            return true;
                        }, Thread.Async.EAsyncTarget.AsyncIO);
                    }   

                    if (Effects.ContainsKey(hash) == false)
                    {
                        lock (Effects)
                        {
                            Effects[hash] = result;
                        }
                        return result;
                    }
                    else
                    {
                        result = Effects[hash];
                        return result;
                    }
                }
                else
                {
                    result = null;
                    return result;
                }
            }
            finally
            {
                mCreatingSession.FinishSession(hash, session, result);
            }
        }
        public async Thread.Async.TtTask<NxRHI.UComputeEffect> GetComputeEffect(RName shaderName, string entry, NxRHI.EShaderType type,
            Graphics.Pipeline.Shader.UShadingEnv shadingEnv, NxRHI.UShaderDefinitions defines, 
            Editor.ShaderCompiler.UHLSLInclude incProvider, string sm = null, bool bDebugShader = true)
        {
            var shader = shaderName.Address;
            var hashStr = shaderName.Address;
            hashStr += entry;
            if (shadingEnv != null)
                hashStr += shadingEnv.ToString();
            if (defines != null)
                hashStr += defines.ToString();
            hashStr += UEngine.Instance.GfxDevice.RenderContext.GlobalEnvHash.ToString();
            var hash = Hash160.CreateHash160(hashStr);
            var shadingCode = Editor.ShaderCompiler.TtShaderCodeManager.Instance.GetShaderCode(shaderName);
            NxRHI.UComputeEffect result;
            lock (Effects)
            {
                if (ComputeEffects.TryGetValue(hash, out result))
                    return result;
            }
            result = await UEngine.Instance.EventPoster.Post((state) =>
            {
                return NxRHI.UComputeEffect.Load(hash); ;
            }, Thread.Async.EAsyncTarget.AsyncIO);
            //result = NxRHI.UComputeEffect.Load(hash);
            if (result != null)
            {
                lock (Effects)
                {
                    if (ComputeEffects.TryGetValue(hash, out var nt))
                        return nt;
                    ComputeEffects.Add(hash, result);
                }
                return result;
            }
            var rc = UEngine.Instance.GfxDevice.RenderContext;
            var compiler = new Editor.ShaderCompiler.UHLSLCompiler();
            compiler.MdfQueue = null;
            if (defines == null)
            {
                defines = new NxRHI.UShaderDefinitions();
            }
            if (shadingEnv != null)
                shadingEnv.GetShaderDefines(shadingEnv.CurrentPermutationId, defines);
            var shaderDesc = await UEngine.Instance.EventPoster.Post((state) =>
            {
                return compiler.CompileShader(shader, entry, type, shadingEnv, null, null, defines, incProvider, sm, bDebugShader);
            }, Thread.Async.EAsyncTarget.AsyncIO);
            if (shaderDesc == null)
                return null;
            var csShader = rc.CreateShader(shaderDesc);
            if (csShader == null)
                return null;

            result = UEngine.Instance.GfxDevice.RenderContext.CreateComputeEffect(csShader);

            lock (Effects)
            {
                if (ComputeEffects.TryGetValue(hash, out var nt))
                    return nt;
                ComputeEffects.Add(hash, result);
            }

            UEngine.Instance.EventPoster.RunOn((state) =>
            {
                result.SaveTo(shaderName, hash);
                return true;
            }, Thread.Async.EAsyncTarget.AsyncIO);

            return result;
        }
    }
}

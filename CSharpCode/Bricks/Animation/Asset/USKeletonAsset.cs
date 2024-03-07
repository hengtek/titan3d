﻿using System;
using EngineNS.IO;
using System.Collections.Generic;
using System.Text;
using EngineNS.Animation.SkeletonAnimation.Skeleton;

namespace EngineNS.Animation.Asset
{
    [Rtti.Meta]
    public partial class USkeletonAssetAMeta : IO.IAssetMeta
    {
        public override string GetAssetExtType()
        {
            return USkeletonAsset.AssetExt;
        }
        public override async System.Threading.Tasks.Task<IO.IAsset> LoadAsset()
        {
            return await UEngine.Instance.AnimationModule.SkeletonAssetManager.GetSkeletonAsset(GetAssetName());
        }
        public override bool CanRefAssetType(IO.IAssetMeta ameta)
        {
            return true;
        }
        public override string GetAssetTypeName()
        {
            return "Skeleton";
        }
    }
    [Rtti.Meta]
    //[UFullSkeleton.Import]
    public partial class USkeletonAsset :IO.BaseSerializer, IO.IAsset
    {
        [Rtti.Meta]
        public USkinSkeleton Skeleton { get; set; } = new USkinSkeleton();
        #region IO.IAsset
        public const string AssetExt = ".skt";
        [Rtti.Meta]
        public RName AssetName { get; set; }

        public IAssetMeta CreateAMeta()
        {
            var result = new USkeletonAssetAMeta();
            return result;

        }

        public IAssetMeta GetAMeta()
        {
            return UEngine.Instance.AssetMetaManager.GetAssetMeta(AssetName);
        }

        public void SaveAssetTo(RName name)
        {
            AssetName = name;
            var typeStr = Rtti.UTypeDescManager.Instance.GetTypeStringFromType(this.GetType());
            var xnd = new IO.TtXndHolder(typeStr, 0, 0);
            using (var attr = xnd.NewAttribute("SkeletonAsset", 0, 0))
            {
                using (var ar = attr.GetWriter(512))
                {
                    ar.Write(this);
                }
                xnd.RootNode.AddAttribute(attr);
            }

            xnd.SaveXnd(name.Address);
            UEngine.Instance.SourceControlModule.AddFile(name.Address);
        }

        public void UpdateAMetaReferences(IAssetMeta ameta)
        {
            ameta.RefAssetRNames.Clear();
        }
        #endregion

        public static USkeletonAsset LoadXnd(USkeletonAssetManager manager, IO.TtXndNode node)
        {
            unsafe
            {
                IO.ISerializer result = null;
                var attr = node.TryGetAttribute("SkeletonAsset");
                if ((IntPtr)attr.CppPointer != IntPtr.Zero)
                {
                    using (var ar = attr.GetReader(manager))
                    {
                        ar.Read(out result, manager);
                    }
                }

                var mesh = result as USkeletonAsset;
                if (mesh != null)
                {
                    return mesh;
                }
                return null;
            }
        }
    }

    public partial class USkeletonAssetManager
    {
        public Dictionary<RName, USkeletonAsset> SkeletonAssets { get; } = new Dictionary<RName, USkeletonAsset>();
        public async System.Threading.Tasks.Task<USkeletonAsset> GetSkeletonAsset(RName name)
        {
            USkeletonAsset result;
            if (SkeletonAssets.TryGetValue(name, out result))
                return result;

            result = await UEngine.Instance.EventPoster.Post((state) =>
            {
                using (var xnd = IO.TtXndHolder.LoadXnd(name.Address))
                {
                    if (xnd != null)
                    {
                        var skeletonAsset = USkeletonAsset.LoadXnd(this, xnd.RootNode);
                        if (skeletonAsset == null)
                            return null;

                        skeletonAsset.AssetName = name;
                        return skeletonAsset;
                    }
                    else
                    {
                        return null;
                    }
                }
            }, Thread.Async.EAsyncTarget.AsyncIO);

            if (result != null)
            {
                SkeletonAssets[name] = result;
                return result;
            }

            return null;
        }
    }
}
namespace EngineNS.Animation
{
    public partial class UAnimationModule
    {
        public Asset.USkeletonAssetManager SkeletonAssetManager { get; } = new Asset.USkeletonAssetManager();
    }
}
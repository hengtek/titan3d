﻿using EngineNS.Animation.SkeletonAnimation.Skeleton.Limb;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Animation.SkeletonAnimation.Skeleton.Limb
{

    public class TtBoneDesc : IO.BaseSerializer, ILimbDesc
    {
        [Rtti.Meta]
        public string Name { get; set; }
        [Rtti.Meta]
        public uint NameHash { get; set; }
        [Rtti.Meta]
        public string ParentName { get; set; }
        [Rtti.Meta]
        public uint ParentHash { get; set; }
        [Rtti.Meta]
        public EngineNS.Matrix InitMatrix { get; set; }
        [Rtti.Meta]
        public EngineNS.Matrix InvInitMatrix { get; set; }
        [Rtti.Meta]
        public EngineNS.Vector3 InvPos { get; set; }
        [Rtti.Meta]
        public EngineNS.Vector3 InvScale { get; set; }
        [Rtti.Meta]
        public EngineNS.Quaternion InvQuat { get; set; }
    }
    class TtBone : IO.BaseSerializer, ILimb
    {
        public TtBone()
        {

        }
        public TtBone(TtBoneDesc boneDesc)
        {
            Desc = boneDesc;
        }

        ILimb GetChild(int index)
        {
            if (index > Children.Count)
                return null;
            return Children[index];
        }
        void AddChild(ILimb child)
        {
            Children.Add(child);
        }

        public AnimatablePose.IAnimatableLimbPose CreatePose()
        {
            return new AnimatablePose.TtAnimatableBonePose(mDesc);
        }
        private TtBoneDesc mDesc = null;
        [Rtti.Meta]
        public ILimbDesc Desc { get=>mDesc; set => mDesc = value as TtBoneDesc; }
        public IndexInSkeleton ParentIndex
        {
            get;
            set;
        } = IndexInSkeleton.Invalid;
        public IndexInSkeleton Index
        {
            get;
            set;
        } = IndexInSkeleton.Invalid;
        public List<ILimb> Children { get; set; } = new List<ILimb>();
        //std::vector<USHORT>			GrantChildren;

        
    }
}

﻿using EngineNS.Animation.SkeletonAnimation.Runtime.Control;
using EngineNS.Animation.SkeletonAnimation.Runtime.Pose;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Animation.SkeletonAnimation.Runtime.Control
{
    public class TtCrossfadePose : TtRuntimePoseControl
    {
        public Pose.TtLocalSpaceRuntimePose FromPose { get; set; }
        public Pose.TtLocalSpaceRuntimePose ToPose { get; set; }
        float mWeight = 0.0f;
        public float BlendTime { get; set; } = 0.1f;
        float mCurrentTime = 0.0f;
        public bool IsFinish = true;
        public override void Update(float elapseSecond)
        {
            if (BlendTime == 0)
                mWeight = 1;
            else
                mWeight = (mCurrentTime) / BlendTime;
            if (mWeight == 1)
            {
                TtRuntimePoseUtility.CopyPose(ref mOutPose, ToPose);
                IsFinish = true;
            }
            else
            {
                TtRuntimePoseUtility.BlendPoses(ref mOutPose, FromPose, ToPose, mWeight);
            }
            mCurrentTime += elapseSecond;
            if (mCurrentTime > BlendTime)
                mCurrentTime = BlendTime;
        }
        public void ResetTime()
        {
            mCurrentTime = 0.0f;
            mWeight = 0.0f;
            IsFinish = false;
        }
    }
}

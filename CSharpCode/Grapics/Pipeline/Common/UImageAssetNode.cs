﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Common
{
    [Bricks.CodeBuilder.ContextMenu("Image", "Image", Bricks.RenderPolicyEditor.UPolicyGraph.RGDEditorKeyword)]
    public class UImageAssetNode : TtRenderGraphNode
    {
        public TtRenderGraphPin ImagePinOut = TtRenderGraphPin.CreateOutput("Image", false, EPixelFormat.PXF_R8G8B8A8_UNORM);
        public UImageAssetNode()
        {
            Name = "ImageAssetNode";
        }
        public override void Dispose()
        {
            ImageSrv = null;
            base.Dispose();
        }
        public EPixelFormat ImageFormat 
        { 
            get
            {
                return ImagePinOut.Attachement.Format;
            }
        }
        public override void InitNodePins()
        {
            ImagePinOut.LifeMode = TtAttachBuffer.ELifeMode.Imported;
            AddOutput(ImagePinOut, NxRHI.EBufferType.BFT_SRV | NxRHI.EBufferType.BFT_UAV);

            if(ImageName == null)
                ImageName = RName.GetRName("texture/default_envmap.srv", RName.ERNameType.Engine);
        }
        public unsafe override void FrameBuild(Graphics.Pipeline.TtRenderPolicy policy)
        {
            var attachement = RenderGraph.AttachmentCache.ImportAttachment(ImagePinOut);
            attachement.Srv = ImageSrv;
            var desc = ImageSrv.mCoreObject.Desc;
            attachement.BufferDesc.Format = desc.Format;
            attachement.BufferDesc.Width = (uint)ImageSrv.PicDesc.Width;
            attachement.BufferDesc.Height = (uint)ImageSrv.PicDesc.Height;
        }
        public NxRHI.USrView ImageSrv;
        [Rtti.Meta]
        public RName ImageName
        {
            get
            {
                if (ImageSrv == null)
                    return null;
                return ImageSrv.AssetName;
            }
            set
            {
                System.Action action = async () =>
                {
                    ImageSrv = await TtEngine.Instance.GfxDevice.TextureManager.GetTexture(value);
                    if (ImageSrv != null)
                    {
                        ImagePinOut.Attachement.Format = ImageSrv.SrvFormat;
                        ImagePinOut.Attachement.Width = (uint)ImageSrv.PicDesc.Width;
                        ImagePinOut.Attachement.Height = (uint)ImageSrv.PicDesc.Height;
                    }
                };
                action();
            }
        }
    }
}

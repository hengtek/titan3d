﻿using System;
using System.Collections.Generic;
using EngineNS.Graphics.Pipeline;
using EngineNS.Graphics.Pipeline.Common;
using EngineNS.GamePlay;

namespace EngineNS.Bricks.GpuDriven
{
    #region CPU Raster
    public struct FRasterTriangle_Rect
    {
        public Vector2i A;
        public Vector2i AB;
        public Vector2i AC;
        public Vector2i Min;
        public Vector2i Max;
        public float AB_AB;
        public float AC_AC;
        public float AB_AC;
        public float DenominatorU;
        public float DenominatorV;

        public void Setup(in Vector2i a, in Vector2i B, in Vector2i C, in Vector2i Size)
        {
            A = a;
            AB = B - A;
            AC = C - A;

            Min = Vector2i.Minimize(Vector2i.Minimize(in A, in B), in C);
            Max = Vector2i.Maximize(Vector2i.Maximize(in A, in B), in C);

            Min = Vector2i.Maximize(in Min, in Vector2i.Zero);
            Max = Vector2i.Minimize(in Max, in Size);

            AB_AB = (float)Vector2i.Dot(in AB, in AB);
            AC_AC = (float)Vector2i.Dot(in AC, in AC);
            AB_AC = (float)Vector2i.Dot(in AB, in AC);

            DenominatorU = ((AC_AC) * (AB_AB) - (AB_AC) * (AB_AC));
            DenominatorV = ((AB_AC) * (AB_AC) - (AB_AB) * (AC_AC));
        }
        public Vector2 GetUV(in Vector2i P)
        {
            Vector2i AP = P - A;
            //u = [（AP·AC）(AB·AB)- (AP·AB)(AB·AC)]/[(AC·AC)(AB·AB) - (AC·AB)(AB·AC)]
            //v = [（AP·AC）(AC·AB)- (AP·AB)(AC·AC)]/[(AB·AC)(AC·AB) - (AB·AB)(AC·AC)]
            float AP_AC = Vector2i.Dot(AP, AC);//dp1
            float AP_AB = Vector2i.Dot(AP, AB);//dp2
            //dp3 x 2
            //div
            float u = ((AP_AC) * (AB_AB) - (AP_AB) * (AB_AC)) / DenominatorU;
            float v = ((AP_AC) * (AB_AC) - (AP_AB) * (AC_AC)) / DenominatorV;
            return new Vector2(u, v);
        }
        public void Rasterize(StbImageSharp.ImageResult image, Color clr, ref Vector2i a, ref Vector2i B, ref Vector2i C)
        {
            var Size = new Vector2i(image.Width, image.Height);
            Setup(in a, in B, in C, in Size);
            for (int y = Min.Y; y <= Max.Y; y++)
            {
                for (int x = Min.X; x <= Max.X; x++)
                {
                    var P = new Vector2i(x, y);
                    var uv = GetUV(in P);
                    if (Vector2.Less(in uv, in Vector2.Zero).Any() || Vector2.Great(in uv, in Vector2.One).Any() ||
                        (uv.X + uv.Y > 1))
                    {
                        continue;
                    }
                    //draw pixel
                    image.SetPixel(x, y, clr);
                }
            }
        }
    }

    public struct FRasterTriangle_Rect2
    {
        public void Rasterize(StbImageSharp.ImageResult image, Color clr, ref Vector2i A, ref Vector2i B, ref Vector2i C)
        {
            var Size = new Vector2i(image.Width, image.Height);
            var Min = Vector2i.Minimize(Vector2i.Minimize(in A, in B), in C);
            var Max = Vector2i.Maximize(Vector2i.Maximize(in A, in B), in C);

            Min = Vector2i.Maximize(in Min, in Vector2i.Zero);
            Max = Vector2i.Minimize(in Max, in Size);

            var yA_B = A.Y - B.Y;
            var xB_A = B.X - A.X;
            var ax_x_by = A.X * B.Y;
            var bx_x_ay = B.X * A.Y;
            var d1 = ax_x_by - bx_x_ay;

            var yA_C = A.Y - C.Y;
            var xC_A = C.X - A.X;
            var ax_x_cy = A.X * C.Y;
            var cx_x_ay = C.X * A.Y;
            var d2 = ax_x_cy - cx_x_ay;

            for (int y = Min.Y; y <= Max.Y; y++)
            {
                for (int x = Min.X; x <= Max.X; x++)
                {
                    var P = new Vector2i(x, y);
                    //var uv = Vector2i.Barycentric(in A, in B, in C, in P);
                    //var PP = A * (1.0f - uv.X - uv.Y) + C * uv.X + B * uv.Y;
                    //assert(PP == P);

                    var sigma = (float)(yA_B * P.X + xB_A * P.Y + d1) / (float)(yA_B * C.X + xB_A * C.Y + d1);
                    var gamma = (float)(yA_C * P.X + xC_A * P.Y + d2) / (float)(yA_C * B.X + xC_A * B.Y + d2);

                    if (sigma < 0 || gamma < 0 || sigma + gamma > 1)
                    {
                        continue;
                    }
                    //draw pixel
                    image.SetPixel(x, y, clr);
                }
            }
        }
    }

    public struct FRasterTriangle_Scanline
    {
        public void Rasterize(StbImageSharp.ImageResult image, Color clr, ref Vector2i v1, ref Vector2i v2, ref Vector2i v3)
        {
            var Size = new Vector2i(image.Width, image.Height);
            SortVertices(ref v1, ref v2, ref v3); //v1.Y <= v2.Y <= v3.Y

            if (v2.Y == v3.Y)
            {//Bottom triangle
                DrawBottomFlatTriangle(image, clr, in Size, v1, v2, v3);
            }
            else if (v1.Y == v2.Y)
            {//top triangle
                DrawTopFlatTriangle(image, clr, in Size, v1, v2, v3);
            }
            else
            {
                //Pappus Law
                var mid = new Vector2i((int)(v1.X + ((float)(v2.Y - v1.Y) / (float)(v3.Y - v1.Y)) * (float)(v3.X - v1.X)),
                                            v2.Y);
                DrawBottomFlatTriangle(image, clr, in Size, in v1, in v2, in mid);
                DrawTopFlatTriangle(image, clr, in Size, in v2, in mid, in v3);
            }
        }
        private void SortVertices(ref Vector2i v1, ref Vector2i v2, ref Vector2i v3)
        {
            if (v1.Y > v2.Y)
            {
                MathHelper.Swap(ref v1, ref v2);
            }
            if (v1.Y > v3.Y)
            {
                MathHelper.Swap(ref v1, ref v3);
            }
            if (v2.Y > v3.Y)
            {
                MathHelper.Swap(ref v2, ref v3);
            }
        }
        private void DrawTopFlatTriangle(StbImageSharp.ImageResult image, Color clr, in Vector2i Size, in Vector2i v1, in Vector2i v2, in Vector2i v3)
        {
            float slope1 = (float)(v3.X - v1.X) / (float)(v3.Y - v1.Y);
            float slope2 = (float)(v3.X - v2.X) / (float)(v3.Y - v2.Y);

            float startX = v3.X;
            float endX = v3.X;

            var cmpEndY = (int)MathHelper.Max(v1.Y, 0);
            for (int y = (int)v3.Y; y >= cmpEndY; y--)
            {
                if (y < Size.Y)
                {
                    var cmpStartX = (int)MathHelper.Max(startX, 0);
                    var cmpEndX = (int)MathHelper.Min(endX, Size.X);
                    for (int x = cmpStartX; x <= cmpEndX; x++)
                    {
                        image.SetPixel(x, y, clr);
                    }
                }

                startX -= slope1;
                endX -= slope2;
            }
        }

        private void DrawBottomFlatTriangle(StbImageSharp.ImageResult image, Color clr, in Vector2i Size, in Vector2i v1, in Vector2i v2, in Vector2i v3)
        {
            float slope1 = (float)(v2.X - v1.X) / (float)(v2.Y - v1.Y);
            float slope2 = (float)(v3.X - v1.X) / (float)(v3.Y - v1.Y);

            float startX = v1.X;
            float endX = v1.X;

            var cmpEndY = (int)MathHelper.Min(v2.Y, Size.Y);
            for (int y = (int)v1.Y; y <= cmpEndY; y++)
            {
                if (y > 0)
                {
                    var cmpStartX = (int)MathHelper.Max(startX, 0);
                    var cmpEndX = (int)MathHelper.Min(endX, Size.X);
                    for (int x = cmpStartX; x <= cmpEndX; x++)
                    {
                        image.SetPixel(x, y, clr);
                    }
                }

                startX += slope1;
                endX += slope2;
            }
        }
    }
    #endregion

    public class TtSwRasterizeShading : Graphics.Pipeline.Shader.UComputeShadingEnv
    {
        public override Vector3ui DispatchArg
        {
            get => new Vector3ui(64, 1, 1);
        }
        public TtSwRasterizeShading()
        {
            CodeName = RName.GetRName("Shaders/Bricks/GpuDriven/SWRasterizer.compute", RName.ERNameType.Engine);
            MainName = "CS_RasterClusterMain";

            this.UpdatePermutation();
        }
        protected override void EnvShadingDefines(in FPermutationId id, NxRHI.UShaderDefinitions defines)
        {
            base.EnvShadingDefines(in id, defines);
        }
        public override void OnDrawCall(NxRHI.UComputeDraw drawcall, Graphics.Pipeline.URenderPolicy policy)
        {
            var node = drawcall.TagObject as TtSwRasterizeNode;
        }
    }

    public class TtSwRasterizeNode : URenderGraphNode
    {
        public URenderGraphPin VisClutersPinIn = URenderGraphPin.CreateInput("VisClusters");
        public URenderGraphPin QuarkRTPinOut = URenderGraphPin.CreateOutput("QuarkRT", false, EPixelFormat.PXF_R32G32_UINT);
        public URenderGraphPin DepthStencilPinOut = URenderGraphPin.CreateOutput("DepthStencil", false, EPixelFormat.PXF_D24_UNORM_S8_UINT);

        public TtSwRasterizeNode()
        {
            Name = "SwRasterizeNode";
        }
        public override void InitNodePins()
        {
            AddInput(VisClutersPinIn, NxRHI.EBufferType.BFT_SRV);

            AddOutput(QuarkRTPinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddOutput(DepthStencilPinOut, NxRHI.EBufferType.BFT_DSV | NxRHI.EBufferType.BFT_SRV);

            base.InitNodePins();
        }
    }

    public class TtQuarkResolveShading : Graphics.Pipeline.Shader.UGraphicsShadingEnv
    {
        public TtQuarkResolveShading()
        {
            CodeName = RName.GetRName("Shaders/Bricks/GpuDriven/QuarkResolve.cginc", RName.ERNameType.Engine);

            this.UpdatePermutation();
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position,
                NxRHI.EVertexStreamType.VST_UV,};
        }
        protected override void EnvShadingDefines(in FPermutationId id, NxRHI.UShaderDefinitions defines)
        {
            
        }
        public override void OnDrawCall(URenderPolicy.EShadingType shadingType, NxRHI.UGraphicDraw drawcall, URenderPolicy policy, Graphics.Mesh.UMesh mesh)
        {
            var node = drawcall.TagObject as TtQuarkResolveNode;
            

            base.OnDrawCall(shadingType, drawcall, policy, mesh);
        }
    }
    public class TtQuarkResolveNode : USceenSpaceNode
    {
        public URenderGraphPin QuarkRTPinIn = URenderGraphPin.CreateInput("QuarkRT");
        public URenderGraphPin DepthStencilPinIn = URenderGraphPin.CreateInput("DepthStencil");
        public URenderGraphPin Rt0PinOut = URenderGraphPin.CreateOutput("MRT0", true, EPixelFormat.PXF_R16G16B16A16_FLOAT);//rgb - metallicty
        public URenderGraphPin Rt1PinOut = URenderGraphPin.CreateOutput("MRT1", true, EPixelFormat.PXF_R10G10B10A2_UNORM);//normal - Flags
        public URenderGraphPin Rt2PinOut = URenderGraphPin.CreateOutput("MRT2", true, EPixelFormat.PXF_R8G8B8A8_UNORM);//Roughness,Emissive,Specular,unused
        public URenderGraphPin Rt3PinOut = URenderGraphPin.CreateOutput("MRT3", true, EPixelFormat.PXF_R16G16_UNORM);//EPixelFormat.PXF_R10G10B10A2_UNORM//motionXY

        public TtQuarkResolveNode()
        {
            Name = "QuarkResolveNode";

        }
        public override void InitNodePins()
        {
            AddInput(QuarkRTPinIn, NxRHI.EBufferType.BFT_SRV);

            AddOutput(Rt0PinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddOutput(Rt1PinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddOutput(Rt2PinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddOutput(Rt3PinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);

            base.InitNodePins();
        }
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            await base.Initialize(policy, debugName);
            ScreenDrawPolicy.mBasePassShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<TtQuarkResolveShading>();
        }
    }
}

namespace EngineNS.UTest
{
    [UTest]
    public class UTest_TtSoftRaster
    {
        bool IgnorTest = false;

        public void UnitTestEntrance()
        {
            if (IgnorTest)
                return;

            var A = new Vector2i(100, 500);
            var B = new Vector2i(150, 100);
            var C = new Vector2i(700, 600);
            {
                var image = StbImageSharp.ImageResult.CreateImage(1024, 1024, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                image.Clear(Color.Black);
                var tri = new Bricks.GpuDriven.FRasterTriangle_Rect();
                tri.Rasterize(image, Color.Red, ref A, ref B, ref C);
                using (var memStream = new System.IO.FileStream(RName.GetRName("utest/TestSoftRaster.png").Address, System.IO.FileMode.OpenOrCreate))
                {
                    var writer = new StbImageWriteSharp.ImageWriter();
                    writer.WritePng(image.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, memStream);
                }
            }
            {
                var image = StbImageSharp.ImageResult.CreateImage(1024, 1024, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                image.Clear(Color.Black);
                var tri = new Bricks.GpuDriven.FRasterTriangle_Rect2();
                tri.Rasterize(image, Color.Red, ref A, ref B, ref C);
                using (var memStream = new System.IO.FileStream(RName.GetRName("utest/TestSoftRaster2.png").Address, System.IO.FileMode.OpenOrCreate))
                {
                    var writer = new StbImageWriteSharp.ImageWriter();
                    writer.WritePng(image.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, memStream);
                }
            }
            {
                var image = StbImageSharp.ImageResult.CreateImage(1024, 1024, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                image.Clear(Color.Black);
                var tri = new Bricks.GpuDriven.FRasterTriangle_Scanline();
                tri.Rasterize(image, Color.Red, ref A, ref B, ref C);
                using (var memStream = new System.IO.FileStream(RName.GetRName("utest/TestSoftRaster3.png").Address, System.IO.FileMode.OpenOrCreate))
                {
                    var writer = new StbImageWriteSharp.ImageWriter();
                    writer.WritePng(image.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, memStream);
                }
            }
        }
    }
}
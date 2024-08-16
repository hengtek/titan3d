#ifndef __FORWORD_OPAQUE_H__
#define __FORWORD_OPAQUE_H__

#include "../../Inc/VertexLayout.cginc"
#include "../../Inc/LightCommon.cginc"
#include "../../Inc/Math.cginc"
#include "../../Inc/ShadowCommon.cginc"
#include "../../Inc/FogCommon.cginc"
#include "../../Inc/MixUtility.cginc"
#include "../../Inc/SysFunction.cginc"
#include "../../Inc/GpuSceneCommon.cginc"

#include "Material"
#include "MdfQueue"

#include "../../Inc/SysFunctionDefImpl.cginc"

//WARNING:don't change vs_main or ps_main's parameters name cause we also use it in c++;It's an appointment;
PS_INPUT VS_Main(VS_INPUT input1)
{
	VS_MODIFIER input = VS_INPUT_TO_VS_MODIFIER(input1);
	PS_INPUT output = (PS_INPUT)0;
	Default_VSInput2PSInput(output, input);

	MTL_OUTPUT mtl = (MTL_OUTPUT)0;
	{
#ifdef MDFQUEUE_FUNCTION
		MdfQueueDoModifiers(output, input);
#endif

#ifdef DO_VS_MATERIAL
		DO_VS_MATERIAL(output, mtl);
#endif
	}

#if !defined(VS_NO_WorldTransform)
	output.vPosition.xyz += mtl.mVertexOffset;
	float4 wp4 = mul(float4(output.vPosition.xyz, 1), WorldMatrix);
	
    output.Set_vNormal(normalize(mul(float4(output.Get_vNormal().xyz, 0), WorldMatrix).xyz));
    output.Set_vTangent(normalize(mul(float4(output.Get_vTangent().xyz, 0), WorldMatrix).xyz));

#else
	float4 wp4 = float4(output.vPosition.xyz, 1);
#endif

    output.Set_vWorldPos(wp4.xyz);
	
	output.vPosition = mul(wp4, GetViewPrjMtx(false));

#if USE_PS_Custom0 == 1
#if ENV_DISABLE_POINTLIGHTS == 0
	output.psCustomUV0.xy = float2(output.vPosition.xy / output.vPosition.w) * float2(0.5f, -0.5f) + float2(0.5f, 0.5f);
#endif
	//output.psCustomUV0.z = float(output.vPosition.z / output.vPosition.w);
	output.psCustomUV0.w = output.vPosition.w;
#endif

	return output;
}

#include "ForwordBasePassPS.cginc"

PS_OUTPUT PS_Main(PS_INPUT input)
{	
	/*PS_OUTPUT output = (PS_OUTPUT)0;
	output.RT0 = float4(1, 1, 1, 1);
	return output;*/
	return PS_MobileBasePass(input);
}

#endif
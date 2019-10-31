Shader "VolumetricLightBeam/Beam_Level1"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}

		_ConeSlopeCosSin("Cone Slope Cos Sin", Vector) = (0,0,0,0)
		_ConeRadius("Cone Radius", Vector) = (0,0,0,0)
		_ConeApexOffsetZ("Cone Apex Offset Z", Float) = 0

		_Color("Color", Color) = (1,1,1,1)
		_Intensity("Intensity",float) = 1
		_AlphaInside("Alpha Inside", Range(0,1)) = 1
		_AlphaOutside("Alpha Outside", Range(0,1)) = 1

		_DistanceFadeStart("Distance Fade Start", Float) = 0
		_DistanceFadeEnd("Distance Fade End", Float) = 1

		_DistanceCamClipping("Camera Clipping Distance", Float) = 0.5

		_AttenuationLerpLinearQuad("Lerp between attenuation linear and quad", Float) = 0.5
		_DepthBlendDistance("Depth Blend Distance", Float) = 2

		_FresnelPow("Fresnel Pow", Range(0,15)) = 1

		_GlareFrontal("Glare Frontal", Range(0,1)) = 0.5
		_GlareBehind("Glare from Behind", Range(0,1)) = 0.5

		_NoiseLocal("Noise Local", Vector) = (0,0,0,0)
		_NoiseParam("Noise Param", Vector) = (0,0,0,0)

		_CameraParams("Camera Params", Vector) = (0,0,0,0)
		_ClippingPlaneWS("Clipping Plane WS", Vector) = (0,0,0,0)
		[KeywordEnum(off,front,back)] _Cull("cull face",int) = 1
}

Category
{
    Tags
    {
        "Queue"="Transparent"
		"RenderType" = "VolumetricLightBeam"
        "IgnoreProjector" = "True"
    } 
	Blend One One
    ZWrite Off
	SubShader
    {
		//VertexLit 
		LOD 100
	    Pass
        {
            Cull [_Cull]

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_LOD
            #pragma fragment frag
			//开启gpu instancing
			#pragma multi_compile_instancing
            #pragma multi_compile _ VLB_NOISE_3D

            #include "VolumetricLightBeamShared.cginc"
            half4 frag (v2f_LOD i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				//1 front 0back
				int outsideBeam = _Cull - 1;
				fixed4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				col.rgb *=col.a* UNITY_ACCESS_INSTANCED_PROP(Props, _Intensity);
				half4 text= tex2D(_MainTex, i.uv);
#if VLB_NOISE_3D
				half4 noise = tex2D(_MainTex, i.uv1);
				col *= text *noise;
#else
				col *= text;
#endif
				col*= lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaInside), UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaOutside), outsideBeam);
				
				return  col * i.fresnel;
            }
            ENDCG
        }
    }
}
}

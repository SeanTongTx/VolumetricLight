// The following comment prevents Unity from auto upgrading the shader. Please keep it to keep backward compatibility
// UNITY_SHADER_NO_UPGRADE

#ifndef _VOLUMETRIC_LIGHT_BEAM_SHARED_INCLUDED_
#define _VOLUMETRIC_LIGHT_BEAM_SHARED_INCLUDED_

#include "UnityCG.cginc"


//#define DEBUG_SHOW_DEPTH 1
//#define DEBUG_SHOW_NOISE3D 1
//#define DEBUG_BLEND_INSIDE_OUTSIDE 1

#if DEBUG_SHOW_DEPTH && !VLB_DEPTH_BLEND
#define VLB_DEPTH_BLEND 1
#endif

#if DEBUG_SHOW_NOISE3D && !VLB_NOISE_3D
#define VLB_NOISE_3D 1
#endif


#if UNITY_VERSION < 540
#define matWorldToObject _World2Object
#define matObjectToWorld _Object2World
inline float4 UnityObjectToClipPos(in float3 pos) { return mul(UNITY_MATRIX_MVP, float4(pos, 1.0)); }
inline float3 UnityObjectToViewPos(in float3 pos) { return mul(UNITY_MATRIX_MV, float4(pos, 1.0)).xyz; }
#else
#define matWorldToObject unity_WorldToObject
#define matObjectToWorld unity_ObjectToWorld
#endif

inline float3 UnityWorldToObjectPos(in float3 pos) { return mul(matWorldToObject, float4(pos, 1.0)).xyz; }
inline float3 UnityObjectToWorldPos(in float3 pos) { return mul(matObjectToWorld, float4(pos, 1.0)).xyz; }

#if VLB_DEPTH_BLEND
#ifndef UNITY_DECLARE_DEPTH_TEXTURE // handle Unity pre 5.6.0
#define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
#endif
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

inline float SampleSceneZ(float4 projPos)
{
	return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(projPos)));
}

float4 DepthFade_VS_ComputeProjPos(float4 vertex_in, float4 vertex_out)
{
	float4 projPos = ComputeScreenPos(vertex_out);
	projPos.z = -UnityObjectToViewPos(vertex_in).z; // = COMPUTE_EYEDEPTH
	return projPos;
}

float DepthFade_PS_BlendDistance(float4 projPos, float distance)
{
	float sceneZ = max(0, SampleSceneZ(projPos) - _ProjectionParams.g);
	float partZ = max(0, projPos.z - _ProjectionParams.g);
	return saturate((sceneZ - partZ) / distance);
}
#endif

inline float lerpClamped(float a, float b, float t) { return lerp(a, b, saturate(t)); }
inline float invLerp(float a, float b, float t) { return (t - a) / (b - a); }
inline float invLerpClamped(float a, float b, float t) { return saturate(invLerp(a, b, t)); }
inline float fromABtoCD_Clamped(float valueAB, float A, float B, float C, float D) { return lerpClamped(C, D, invLerpClamped(A, B, valueAB)); }


struct v2f
{
	float4 posClipSpace : SV_POSITION;
	float4 posObjectSpace : TEXCOORD0;
	float4 posWorldSpace : TEXCOORD1;
	float4 posViewSpaceAndIsCap : TEXCOORD2;
	UNITY_FOG_COORDS(3)
#if VLB_DEPTH_BLEND
		float4 projPos : TEXCOORD4;
#endif
#if VLB_NOISE_3D
	float4 uvgrab : TEXCOORD5;
#endif
	float3 normal : NORMAL;	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//修改为GPUInstanced


UNITY_INSTANCING_BUFFER_START(Props)

UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
UNITY_DEFINE_INSTANCED_PROP(half, _Intensity)
UNITY_DEFINE_INSTANCED_PROP(half, _AlphaInside)
UNITY_DEFINE_INSTANCED_PROP(half, _AlphaOutside)
UNITY_DEFINE_INSTANCED_PROP(float2, _ConeSlopeCosSin) // between -1 and +1
UNITY_DEFINE_INSTANCED_PROP(float2, _ConeRadius) // x = start radius ; y = end radius
UNITY_DEFINE_INSTANCED_PROP(float, _ConeApexOffsetZ) // > 0
UNITY_DEFINE_INSTANCED_PROP(float, _AttenuationLerpLinearQuad)
UNITY_DEFINE_INSTANCED_PROP(float, _DistanceFadeStart)
UNITY_DEFINE_INSTANCED_PROP(float, _DistanceFadeEnd)
UNITY_DEFINE_INSTANCED_PROP(float, _DistanceCamClipping)
UNITY_DEFINE_INSTANCED_PROP(float, _FresnelPow)
UNITY_DEFINE_INSTANCED_PROP(float, _GlareFrontal)
UNITY_DEFINE_INSTANCED_PROP(float, _GlareBehind)
UNITY_DEFINE_INSTANCED_PROP(float4, _CameraParams) // xyz: object space forward vector ; w: cameraIsInsideBeamFactor (-1 : +1)


#if VLB_CLIPPING_PLANE
UNITY_DEFINE_INSTANCED_PROP(float4, _ClippingPlaneWS)
#endif

#if VLB_DEPTH_BLEND
UNITY_DEFINE_INSTANCED_PROP(float, _DepthBlendDistance)
#endif

#if VLB_NOISE_3D
UNITY_DEFINE_INSTANCED_PROP(float4, _NoiseLocal)
UNITY_DEFINE_INSTANCED_PROP(float3, _NoiseParam)
#endif

UNITY_INSTANCING_BUFFER_END(Props)

#if VLB_NOISE_3D
uniform sampler3D _VLB_NoiseTex3D;
uniform float4 _VLB_NoiseGlobal;
#endif

#define CONST_PI 3.14159			
v2f vert(appdata_base v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	v2f o;
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	// compute the proper cone shape
	float4 vertex = v.vertex;
	vertex.xy *= lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _ConeRadius).x, UNITY_ACCESS_INSTANCED_PROP(Props, _ConeRadius).y, vertex.z);
	vertex.z *= UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeEnd);
	o.normal = v.normal;
	o.posClipSpace = UnityObjectToClipPos(vertex);
	o.posWorldSpace = mul(matObjectToWorld, vertex);

	o.posObjectSpace = vertex;
#if VLB_DEPTH_BLEND
	o.projPos = DepthFade_VS_ComputeProjPos(vertex, o.posClipSpace);
#endif

	float3 posViewSpace = UnityObjectToViewPos(vertex);
	o.posViewSpaceAndIsCap = float4(posViewSpace, 0);

#if VLB_NOISE_3D
#if UNITY_UV_STARTS_AT_TOP
	float scaleY = -1.0;
#else
	float scaleY = 1.0;
#endif
	o.uvgrab.xy = (float2(o.posClipSpace.x, o.posClipSpace.y * scaleY) + o.posClipSpace.w) * 0.5;
	o.uvgrab.zw = o.posClipSpace.zw;
#endif

	UNITY_TRANSFER_FOG(o, o.posClipSpace);
	return o;
}


float GetNoise3DFactor(float3 wpos)
{
#if VLB_NOISE_3D
	float intensity = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseParam).x;
	float3 velocity = lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseLocal).xyz, _VLB_NoiseGlobal.xyz, UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseParam).y);
	float scale = lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseLocal).w, _VLB_NoiseGlobal.w, UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseParam).z);
	float noise = tex3D(_VLB_NoiseTex3D, frac(wpos * scale + (_Time.y * velocity))).a;
	return lerp(1, noise, intensity);
#else
	return 1;
#endif
}

// Get signed distance of pos from the plane (normal ; d).
// Normal should be normalized.
// If we want to disable this feature, we could set normal and d to 0 (no discard in this case).
inline float DistanceToPlane(float3 pos, float3 normal, float d) { return dot(normal, pos) + d; }

half4 fragShared(v2f i, float outsideBeam)
{
	UNITY_SETUP_INSTANCE_ID(i);
#if VLB_CLIPPING_PLANE
	// clipping plane
	float distToClipPlane = DistanceToPlane(i.posWorldSpace.xyz, UNITY_ACCESS_INSTANCED_PROP(Props, _ClippingPlaneWS).xyz, UNITY_ACCESS_INSTANCED_PROP(Props, _ClippingPlaneWS).w);
	clip(distToClipPlane);
	float clipPlaneAlpha = lerp(1, smoothstep(0, 0.25, distToClipPlane), saturate(sign(distToClipPlane)));
#else
	float clipPlaneAlpha = 1;
#endif

#if DEBUG_SHOW_DEPTH
	return SampleSceneZ(i.projPos) * _ProjectionParams.w;
#endif

#if DEBUG_SHOW_NOISE3D
	return GetNoise3DFactor(i.posWorldSpace);
#endif

	float3 vecCamForwardOSN = UNITY_ACCESS_INSTANCED_PROP(Props, _CameraParams).xyz;
	float cameraIsInsideBeamFactor = UNITY_ACCESS_INSTANCED_PROP(Props, _CameraParams).w; // (-1 ; 1)

	half cameraIsOrtho = unity_OrthoParams.w; // w is 1.0 when camera is orthographic, 0.0 when perspective
	float3 posViewSpace = i.posViewSpaceAndIsCap.xyz;
	float isCap = i.posViewSpaceAndIsCap.w;
	float pixDistFromSource = length(i.posObjectSpace.z);

	// Camera Position in Object Space
	float3 camPosObjectSpace = UnityWorldToObjectPos(_WorldSpaceCameraPos);

	// Vector Camera to current Pixel, in object space and normalized
	float3 vecCamToPixOSN = normalize(i.posObjectSpace.xyz - camPosObjectSpace);

	// Deal with ortho camera:
	// With ortho camera, we don't want to change the fresnel according to camera position.
	// So instead of computing the proper vector "Camera to Pixel", we take account of the "Camera Forward" vector (which is not dependant on the pixel position)
	vecCamToPixOSN = lerp(vecCamToPixOSN, vecCamForwardOSN, cameraIsOrtho);

	// Compute normal
	float2 cosSinFlat = normalize(i.posObjectSpace.xy);
	float2 coneslopecosesin = UNITY_ACCESS_INSTANCED_PROP(Props, _ConeSlopeCosSin);
	float3 normalObjectSpace = normalize(float3(cosSinFlat.x * coneslopecosesin.x, cosSinFlat.y * coneslopecosesin.x, -coneslopecosesin.y));
	normalObjectSpace *= (outsideBeam * 2 - 1); // = outsideBeam ? 1 : -1;
	normalObjectSpace = lerp(normalObjectSpace, float3(0, 0, -1), isCap);

	// compute Boost factor

	float insideBoostDistance = lerp(0, UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeEnd), UNITY_ACCESS_INSTANCED_PROP(Props, _GlareFrontal));
	float boostFactor = 1 - smoothstep(0, 0 + insideBoostDistance + 0.001, pixDistFromSource); // 0 = no boost ; 1 = max boost
	boostFactor = lerp(boostFactor, 0, outsideBeam); // no boost for outside pass
	boostFactor = lerp(0, boostFactor, saturate(cameraIsInsideBeamFactor)); // no boost for outside pass
	boostFactor = lerp(boostFactor, 1, isCap); // cap is always at max boost

	// Attenuation
	float distFromSourceNormalized = invLerpClamped(UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeStart), UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeEnd), pixDistFromSource);
	// Almost simple linear attenuation between Fade Start and Fade End: Use smoothstep for a better fall to zero rendering
	float attLinear = smoothstep(0, 1, 1 - distFromSourceNormalized);
	// Unity's custom quadratic attenuation https://forum.unity.com/threads/light-attentuation-equation.16006/
	float attQuad = 1.0 / (1.0 + 25.0 * distFromSourceNormalized * distFromSourceNormalized);
	const float kAttQuadStartToFallToZero = 0.8;
	attQuad *= saturate(smoothstep(1.0, kAttQuadStartToFallToZero, distFromSourceNormalized)); // Near the light's range (fade end) we fade to 0 (because quadratic formula never falls to 0)
	float attenuation = lerp(attLinear, attQuad, UNITY_ACCESS_INSTANCED_PROP(Props, _AttenuationLerpLinearQuad));

	// Noise factor
	float noise3DFactor = GetNoise3DFactor(i.posWorldSpace);
	//   noise3DFactor = lerpClamped(noise3DFactor, 1, attenuation * 0.1);

	   // depth blend factor
#if VLB_DEPTH_BLEND
	// we disable blend factor when the pixel is near the light source,
	// to prevent from blending with the light source model geometry (like the flashlight model).
	float depthBlendStartDistFromSource = UNITY_ACCESS_INSTANCED_PROP(Props, _DepthBlendDistance);
	float depthBlendDist = UNITY_ACCESS_INSTANCED_PROP(Props, _DepthBlendDistance)  * invLerpClamped(0, depthBlendStartDistFromSource, pixDistFromSource);
	float depthBlendFactor = DepthFade_PS_BlendDistance(i.projPos, depthBlendDist);
	depthBlendFactor = lerp(depthBlendFactor, 1, step(UNITY_ACCESS_INSTANCED_PROP(Props, _DepthBlendDistance), 0));
	depthBlendFactor = lerp(depthBlendFactor, 1, cameraIsOrtho); // disable depth BlendState factor with ortho camera (temporary fix)
#else
	float depthBlendFactor = 1;
#endif

	// fade when too close factor
	float distCamClipping = lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceCamClipping), 0, boostFactor); // do not fade according to camera when we are in boost zone, to keep boost effect
	float camFadeDistStart = _ProjectionParams.y; // cam near place
	float camFadeDistEnd = camFadeDistStart + distCamClipping;
	float distCamToPixWS = abs(posViewSpace.z); // only check Z axis (instead of length(posViewSpace.xyz)) to have smoother transition with near plane (which is not curved)
	float fadeWhenTooClose = smoothstep(0, 1, invLerpClamped(camFadeDistStart, camFadeDistEnd, distCamToPixWS));
	fadeWhenTooClose = lerp(fadeWhenTooClose, 1, cameraIsOrtho); // fading according to camera eye position doesn't make sense with ortho camera

	float vecCamToPixDotZ = dot(vecCamToPixOSN, float3(0, 0, 1));
	float factorNearAxisZ = abs(vecCamToPixDotZ);

	// disable noise 3D when looking from behind or from inside because it makes the cone shape too much visible
	noise3DFactor = lerp(noise3DFactor, 1, pow(factorNearAxisZ, 10));

	// fresnel
	float fresnel = 0;

		// real fresnel factor
		float fresnelReal = dot(normalObjectSpace, -vecCamToPixOSN);

		// compute a fresnel factor to support long beams by projecting the viewDir vector
		// on the virtual plane formed by the normal and tangent
		float3 tangentPlaneNormal = normalize(i.posObjectSpace.xyz + float3(0, 0, UNITY_ACCESS_INSTANCED_PROP(Props, _ConeApexOffsetZ)));
		float distToPlane = dot(-vecCamToPixOSN, tangentPlaneNormal);
		float3 vec2D = normalize(-vecCamToPixOSN - distToPlane * tangentPlaneNormal);
		float fresnelProjOnTangentPlane = dot(normalObjectSpace, vec2D);

		// blend between the 2 fresnels
		fresnel = lerp(fresnelProjOnTangentPlane, fresnelReal, factorNearAxisZ);


	float fresnelPow = UNITY_ACCESS_INSTANCED_PROP(Props, _FresnelPow);

	// Lerp the fresnel pow to the glare factor according to how far we are from the axis Z
	const float kMaxGlarePow = 1.5;
	float glareFactor = kMaxGlarePow * (1 - lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _GlareFrontal), UNITY_ACCESS_INSTANCED_PROP(Props, _GlareBehind), outsideBeam));
	fresnelPow = lerpClamped(fresnelPow, min(fresnelPow, glareFactor), factorNearAxisZ);

	// Pow the fresnel
	fresnel = saturate(fresnel);
	fresnel = smoothstep(0, 1, fresnel);
	fresnel = saturate(pow(fresnel, fresnelPow));

	// Treat Cap a special way
	fresnel = lerp(fresnel, outsideBeam, isCap);
	outsideBeam = lerp(outsideBeam, 1 - outsideBeam, isCap);

	// Boost distance inside
	float boostFresnel = lerpClamped(fresnel, 1 + 0.001, boostFactor);
	fresnel = lerp(boostFresnel, fresnel, outsideBeam); // no boosted fresnel if outside
	
	// smooth blend between inside and outside geometry depending of View Direction
	//float factorFaceLightSourcePerPixN = saturate(-vecCamToPixDotZ); // transition is too hard, but UnitTest 'Angle' looks better...
	const float kFaceLightSmoothingLimit = 1;
	float factorFaceLightSourcePerPixN = saturate(smoothstep(kFaceLightSmoothingLimit, -kFaceLightSmoothingLimit, vecCamToPixDotZ)); // smoother transition

	float blendInsideWithOutside = lerp(factorFaceLightSourcePerPixN, 1 - factorFaceLightSourcePerPixN, outsideBeam);

#if DEBUG_BLEND_INSIDE_OUTSIDE
	return lerp(float4(1, 0, 0, 1), float4(0, 1, 0, 1), factorFaceLightSourcePerPixN);
#endif

	float intensity = UNITY_ACCESS_INSTANCED_PROP(Props, _Intensity)
		* clipPlaneAlpha
		* attenuation
		* fadeWhenTooClose
		* depthBlendFactor
		* fresnel
		* blendInsideWithOutside
		* noise3DFactor
		;

	half4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
	col.rgb *= UNITY_ACCESS_INSTANCED_PROP(Props, _Color).a * intensity;
	col.rgb *= lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaInside), UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaOutside), outsideBeam);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0)); // since we use this shader with Additive blending, fog color should be treating as black
	return  col;

}

///**************************LOD Level1*************************//

struct appdata
{
	float4 vertex : POSITION;
	fixed3 normal : NORMAL;
	half2 uv : TEXCOORD0;
	//GPUInstance
	UNITY_VERTEX_INPUT_INSTANCE_ID//instanced properties in fragment Shader.
};
struct v2f_LOD
{
	float4 vertex : SV_POSITION;
	float2 uv : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	half fresnel:TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

sampler2D _MainTex;
float4 _MainTex_ST;
int _Cull;

v2f_LOD vert_LOD(appdata v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	v2f_LOD o;
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	// compute the proper cone shape

	float4 vertex = v.vertex;
	float2 _Radius = UNITY_ACCESS_INSTANCED_PROP(Props, _ConeRadius);
	float distanc = UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeEnd);
	vertex.xy *= lerp(_Radius.x, _Radius.y, vertex.z);
	vertex.z *= distanc;
	
	//1 front 0back
	int outsideBeam = _Cull - 1;
	//计算法线
	half3 normalObjectSpace = lerp(-v.normal, v.normal, outsideBeam);

	// Attenuation
	float distFromSourceNormalized =lerp(1,0, UNITY_ACCESS_INSTANCED_PROP(Props, _DistanceFadeStart)/ distanc );


	const fixed2 center = float2(0.5, 0.5);
	fixed2 vertUV = (v.uv-0.5)*(1+distFromSourceNormalized)*0.6 +0.5;

#if VLB_NOISE_3D
	float ang =  _Time.y;
	fixed spd = 0.3;
	fixed c = cos(ang*spd);
	fixed s = sin(ang*spd);

	fixed2 uv = (mul(vertUV - center, float2x2(c, -s, s, c)) + center);
	c = cos(ang*-spd*0.5);
	s = sin(ang*-spd*0.5);
	fixed2 uv1 = (mul(vertUV - center, float2x2(c, -s, s, c)) + center);
	uv = TRANSFORM_TEX(uv, _MainTex);
	uv1 = TRANSFORM_TEX(uv1, _MainTex);

#else
	fixed2 uv= TRANSFORM_TEX(vertUV, _MainTex);
	fixed2 uv1 = uv1;
#endif
	o.uv = uv;
	o.uv1 = uv1;
	o.vertex = UnityObjectToClipPos(vertex);

	//视线方向
	half3 viewObjectSpace = normalize(ObjSpaceViewDir(vertex));
	half3 viewObjectSpaceRaw = normalize(ObjSpaceViewDir(v.vertex));


	// compute a fresnel factor to support long beams by projecting the viewDir vector
	// on the virtual plane formed by the normal and tangent

	half3 tangentPlaneNormal = normalize(vertex.xyz + float3(0, 0, UNITY_ACCESS_INSTANCED_PROP(Props, _ConeApexOffsetZ)));

	// fresnel
	fixed fresnel = 0;
	
		// real fresnel factor
		fixed distToPlane = dot(viewObjectSpace, tangentPlaneNormal);
		fixed3 vec2D = normalize(viewObjectSpace - distToPlane * tangentPlaneNormal);
		fixed fresnelProjOnTangentPlane = dot(normalObjectSpace, vec2D);
		fixed factorNearAxisZ = abs(dot(viewObjectSpaceRaw, float3(0, 0, 1)));
		// blend between the 2 fresnels
		fresnel = fresnelProjOnTangentPlane;

		fixed fresnelPow = UNITY_ACCESS_INSTANCED_PROP(Props, _FresnelPow);
		// Lerp the fresnel pow to the glare factor according to how far we are from the axis Z
		const fixed kMaxGlarePow = 1.5;
		fixed glareFactor = kMaxGlarePow * (1 - lerp(UNITY_ACCESS_INSTANCED_PROP(Props, _GlareFrontal), UNITY_ACCESS_INSTANCED_PROP(Props, _GlareBehind), outsideBeam));
		fresnelPow = lerpClamped(fresnelPow, min(fresnelPow, glareFactor), factorNearAxisZ);

		// Pow the fresnel
		fresnel = saturate(fresnel);
		fresnel = smoothstep(0, 1, fresnel);
		fresnel = max(0,saturate(pow(fresnel, fresnelPow)));
	
	o.fresnel =  fresnel;


	return o;
}


#endif

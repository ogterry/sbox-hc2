HEADER
{
	Description = "Template Shader for S&box";
}

MODES
{
    VrForward();
	Depth( S_MODE_DEPTH ); 
	Reflection( S_MODE_REFLECTIONS );
    ToolsVis( S_MODE_TOOLS_VIS );
    ToolsWireframe( S_MODE_TOOLS_WIREFRAME );
}

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#include "common/shared.hlsl"

	StaticCombo( S_MODE_DEPTH, 0..1, Sys(All ) );
    StaticCombo( S_MODE_REFLECTIONS, 0..1, Sys(All ) );
    StaticCombo( S_MODE_TOOLS_WIREFRAME, 0..1, Sys( ALL ) );

    StaticComboRule( Allow1( S_MODE_DEPTH, S_MODE_REFLECTIONS ) );
    StaticComboRule( Allow1( S_MODE_DEPTH, S_MODE_TOOLS_WIREFRAME ) );
    StaticComboRule( Allow1( S_MODE_REFLECTIONS, S_MODE_TOOLS_WIREFRAME ) );
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );

		// FinalizeVertex should process what's on vPositionWs to
		o.vPositionWs.xy += g_vCameraPositionWs.xy;
		o.vPositionWs.xy += g_vCameraDirWs.xy * 96.0f; // Roughly the distance from camera to player
		o.vPositionPs = Position3WsToPs( o.vPositionWs );

		o = FinalizeVertex( o );
		return o;
	}
}

//=========================================================================================================================

PS
{
    #include "common/pixel.hlsl"
	#include "raytracing/reflections.hlsl"

	// This is stupid and nonstandard, I want to get rid of it
    #if ( S_MODE_REFLECTIONS )
		#define FinalOutput ReflectionOutput
        #define Target
	#else
		#define FinalOutput float4
        #define Target : SV_Target0
	#endif

	#if ( S_MODE_TOOLS_WIREFRAME )
		RenderState( FillMode, WIREFRAME );
		RenderState( SlopeScaleDepthBias, 0.5 ); // Depth bias params tuned for plantation_source2 under DX11
		RenderState( DepthBiasClamp, 0.0005 );
	#endif

	FinalOutput MainPs( PixelInput i ) Target
	{
		#if ( S_MODE_TOOLS_WIREFRAME )
            return 1;
        #endif

		Material m = Material::Init();
		m.Roughness = 0.15f;
		m.Albedo = 0.001f;
		m.Normal = float3(0,0,1);
		
		#if S_MODE_REFLECTIONS
            return Reflections::From( i, m, 10 );
        #else
			return ShadingModelStandard::Shade( i, m );
		#endif
	}
}

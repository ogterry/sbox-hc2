HEADER
{
	Description = "Voxel Shader";
}

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    VrForward();
    Depth( S_MODE_DEPTH );
    ToolsVis( S_MODE_TOOLS_VIS );
    ToolsWireframe( S_MODE_TOOLS_WIREFRAME );
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"

	uint vData : TEXCOORD10 < Semantic( None ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	float g_flVoxelSize < Attribute( "VoxelSize" ); Default( 16.0 ); >;

	StructuredBuffer<float4> g_ColorPalette < Attribute( "ColorPalette" ); >;

	static const float3 normals[6] =
	{
		float3( 0, 0, 1 ),
		float3( 0, 0, -1 ),
		float3( 0, -1, 0 ),
		float3( 0, 1, 0 ),
		float3( -1, 0, 0 ),
		float3( 1, 0, 0 )
	};

	static const float3 tangents[6] = 
	{
		float3( 1, 0, 0 ),
		float3( 1, 0, 0 ),
		float3( 0, 0, -1 ),
		float3( 0, 0, -1 ),
		float3( 0, 0, -1 ),
		float3( 0, 0, -1 )
	};

	PixelInput MainVs( VertexInput i )
	{
		float3 position = float3(float(i.vData & 63), float((i.vData >> 6) & 63), float((i.vData >> 12) & 63)) * g_flVoxelSize;
		int paletteIndex = int((i.vData >> 24) & 255);
		int normalIndex = int((i.vData >> 21) & 7); 
		float brightness = float((i.vData >> 18) & 7) / 7.0;
		float3 color = g_ColorPalette[paletteIndex];

		i.vPositionOs = position;

		float3 vNormalOs = normals[normalIndex];
		float3 vTangentOs = tangents[normalIndex];
		float3 vBinormalOs = cross( vNormalOs, vTangentOs );

		i.vTexCoord = float2( dot( vBinormalOs, position ), dot( vTangentOs, position ) ) * ( 1.0 / 32.0 );

		PixelInput o = ProcessVertex( i );
		o.vNormalWs = vNormalOs;
		o.vTangentUWs = vBinormalOs;
		o.vTangentVWs = vTangentOs;
		o.vVertexColor = float4( color.rgb * brightness, 1.0 );
		return FinalizeVertex( o );
	}
}

PS
{
	StaticCombo( S_MODE_TOOLS_WIREFRAME, 0..1, Sys( ALL ) );
	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );

	#include "common/pixel.hlsl"

	#if ( S_MODE_TOOLS_WIREFRAME )
		RenderState( FillMode, WIREFRAME );
		RenderState( SlopeScaleDepthBias, 0.5 );
		RenderState( DepthBiasClamp, 0.0005 );
	#endif

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		#if ( S_MODE_TOOLS_WIREFRAME )
			return float4( 1, 1, 1, 1 );
		#endif
			
		#if ( S_MODE_DEPTH )
			return float4( 0.0, 0.0, 0.0, 1.0 );
		#endif

		Material m = Material::From( i );
		m.Albedo = i.vVertexColor.rgb;
		m.Roughness = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}

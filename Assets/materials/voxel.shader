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

	nointerpolation int TextureIndex : TEXCOORD10;
};

VS
{
	#include "common/vertex.hlsl"

	float g_flVoxelSize < Attribute( "VoxelSize" ); Default( 16.0 ); >;

	struct PaletteMaterial
	{
		float4 Color;
		int TextureIndex;
		int Pad1;
		int Pad2;
		int Pad3;
	};

	StructuredBuffer<PaletteMaterial> g_ColorPalette < Attribute( "ColorPalette" ); >;

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
		PaletteMaterial material = g_ColorPalette[paletteIndex];

		i.vPositionOs = position;

		float3 vNormalOs = normalize( normals[normalIndex] );
		float3 vTangentOs = normalize( tangents[normalIndex] );
		float3 vBinormalOs = normalize( cross( vNormalOs, vTangentOs ) );

		i.vTexCoord = float2( dot( vBinormalOs, position ), dot( vTangentOs, position ) ) * ( 1.0 / 64.0 );

		PixelInput o = ProcessVertex( i );
		o.vNormalWs = vNormalOs;
		o.vTangentUWs = vBinormalOs;
		o.vTangentVWs = vTangentOs;
		o.vVertexColor = float4( material.Color.rgb * brightness, 1.0 );
		o.TextureIndex = material.TextureIndex.x + 1;
		return FinalizeVertex( o );
	}
}

PS
{
	StaticCombo( S_MODE_TOOLS_WIREFRAME, 0..1, Sys( ALL ) );
	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );

	#include "common/pixel.hlsl"
	#include "common/Bindless.hlsl"

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

		Texture2D texture = GetBindlessTexture2D( NonUniformResourceIndex( i.TextureIndex ) );
		float4 textureSample = texture.Sample( g_sAniso, i.vTextureCoords.xy );

		Material m = Material::From( i );
		m.Albedo = i.vVertexColor.rgb * textureSample.rgb;
		m.Roughness = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}

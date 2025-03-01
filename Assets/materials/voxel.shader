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

	DynamicComboRule( Allow0( D_COMPRESSED_NORMALS_AND_TANGENTS ) );

	float g_flVoxelSize < Attribute( "VoxelSize" ); Default( 16.0 ); >;

	struct PaletteMaterial
	{
		float4 Color;
		float2 TextureSize;
		int TextureIndex;
		int Pad1;
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

		i.vNormalOs = float4( vNormalOs.xyz, 1.0 );
		i.vTangentUOs_flTangentVSign = float4( vTangentOs.xyz, 1.0 );

		PixelInput o = ProcessVertex( i );
		o.vVertexColor = float4( SrgbGammaToLinear( material.Color.rgb ) * brightness, 1.0 );
		o.TextureIndex = material.TextureIndex.x + 1;

		float u = dot( o.vTangentUWs.xyz, o.vPositionWs.xyz );
		float v = dot( o.vTangentVWs.xyz, o.vPositionWs.xyz );
		o.vTextureCoords = float2( u, v ) * ( 1.0 / material.TextureSize );

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
		float4 textureSample = texture.Sample( g_sPointWrap, i.vTextureCoords.xy );

		Material m = Material::From( i );
		m.Albedo = textureSample.rgb * i.vVertexColor.rgb;
		m.Normal = i.vNormalWs;
		m.Roughness = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}

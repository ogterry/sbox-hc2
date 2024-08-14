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
	float Height : COLOR0;
};

VS
{
	#include "common/vertex.hlsl"

	DynamicComboRule( Allow0( D_COMPRESSED_NORMALS_AND_TANGENTS ) );

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

		i.vNormalOs = float4( vNormalOs.xyz, 1.0 );
		i.vTangentUOs_flTangentVSign = float4( vTangentOs.xyz, 1.0 );

		PixelInput o = ProcessVertex( i );
		o.vVertexColor = float4( material.Color.rgb * brightness, 1.0 );
		o.TextureIndex = material.TextureIndex.x + 1;
		o.Height = o.vPositionWs.z / 128;

		float aspect = 3000 / 1980;
		float scale = 512 * 32;

		o.vTextureCoords = float2( 
			dot( o.vTangentUWs.xyz, o.vPositionWs.xyz ), 
			dot( o.vTangentVWs.xyz, o.vPositionWs.xyz ) ) * float2( 1.0 / scale, 1.0f / (scale * aspect) );

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

	float3 ColorRamp(float factor)
	{
		float3 color;

		// Defining the colors in the Color Ramp
		float3 orangeColor = float3(1.0f, 0.6f, 0.2f);
		float3 whiteColor = float3(1.0f, 1.0f, 1.0f);

		// We assume the positions are [0.0, 0.95, 1.0] as per the graph
		if (factor <= 0.95f) {
			// Interpolate between orange and orange
			color = orangeColor;
		} else {
			// Interpolate between orange and white
			float t = (factor - 0.95f) / (1.0f - 0.95f); // Normalize to 0 - 1 range
			color = lerp(orangeColor, whiteColor, t);
		}

		return color;
	}

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

		float2 uv = i.vTextureCoords.xy;
		float gradientFactor = saturate( i.Height );
		float3 gradientColor = ColorRamp(gradientFactor);

      	float3 vHsv = RgbToHsv( textureSample.rgb );
        vHsv.b *= 0.1;
        textureSample.rgb = HsvToRgb( vHsv );

		Material m = Material::From( i );
		m.Albedo = textureSample.rgb * gradientColor.rgb * i.vVertexColor.rgb;
		m.Roughness = 1;
		return ShadingModelStandard::Shade( i, m );
	}
}

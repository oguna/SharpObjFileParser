

struct VS_IN
{
	float3 pos : POSITION;
	float3 normal : NORMAL;
	float2 tex : TEXCOORD;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float2 tex : TEXCOORD0;
	float3 eye : TEXCOORD1;
};

float4x4 worldViewProj;
float4 localLightDirection; // ローカル座標系でのライトの向き
float4 eyePos;
float4 ambient;
float4 diffuse;
float4 specular;
float shineness;
float3 dummy;

Texture2D picture;
SamplerState pictureSampler;

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN) 0;

	output.pos = mul(float4(input.pos,1), worldViewProj);
	output.normal = input.normal;
	output.tex = input.tex;
	output.eye = eyePos.xyz - input.pos.xyz;
	return output;
}


float4 PS(PS_IN input) : SV_Target
{
	float3 L = localLightDirection.xyz;
	float3 H = normalize(L + normalize(input.eye));
	float3 N = normalize(input.normal);
	if (TEXTURED){
		return (ambient + diffuse * max(0, dot(input.normal, localLightDirection.xyz))) *picture.Sample(pictureSampler, input.tex) // 拡散環境光
			+ specular * pow(max(0, dot(N, H)), shineness); // 鏡面反射光
	}
	else {
		return ambient + diffuse * max(0, dot(input.normal, localLightDirection.xyz)) // 拡散環境光
			+ specular * pow(max(0, dot(N, H)), shineness); // 鏡面反射光
	}
}
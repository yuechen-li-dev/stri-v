struct VSInput
{
    float3 Position : POSITION;
};

struct VSOutput
{
    float4 Position : SV_Position;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;
    output.Position = float4(input.Position, 1.0);
    return output;
}

float4 PSMain(VSOutput input) : SV_Target
{
    return float4(1.0, 1.0, 1.0, 1.0);
}

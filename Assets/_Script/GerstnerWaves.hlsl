#ifndef GERSTNER_WAVES_INCLUDED
#define GERSTNER_WAVES_INCLUDED

#ifndef PI
#define PI 3.14159265359
#endif

// wave = (dirX, dirZ, steepness, wavelength) — same layout Waves.cs sends via _Wave0.._Wave3
void GerstnerWave_float(float4 wave, float3 p, inout float3 tangent, inout float3 binormal, inout float3 offset)
{
    float steepness = wave.z;
    float wavelength = wave.w;

    if (wavelength <= 0.0001)
        return;

    float k = 2.0 * PI / wavelength;
    float c = sqrt(9.81 / k);
    float2 d = normalize(wave.xy);
    float f = k * (dot(d, p.xz) - c * _Time.y);
    float a = steepness / k;

    offset.x += d.x * (a * cos(f));
    offset.z += d.y * (a * cos(f));
    offset.y += a * sin(f);

    tangent += float3(
        -d.x * d.x * (steepness * sin(f)),
        d.x * (steepness * cos(f)),
        -d.x * d.y * (steepness * sin(f))
    );

    binormal += float3(
        -d.x * d.y * (steepness * sin(f)),
        d.y * (steepness * cos(f)),
        -d.y * d.y * (steepness * sin(f))
    );
}

// Entry point for the Shader Graph Custom Function node.
// Position: world-space vertex position (feed a "Position (World)" node in).
// Wave0..Wave3: Vector4 properties fed from the blackboard, matching Waves.cs's _Wave0.._Wave3.
void WaterWaves_float(float3 Position, float4 Wave0, float4 Wave1, float4 Wave2, float4 Wave3,
                       out float3 DisplacedPosition, out float3 Normal)
{
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);
    float3 offset = float3(0, 0, 0);

    GerstnerWave_float(Wave0, Position, tangent, binormal, offset);
    GerstnerWave_float(Wave1, Position, tangent, binormal, offset);
    GerstnerWave_float(Wave2, Position, tangent, binormal, offset);
    GerstnerWave_float(Wave3, Position, tangent, binormal, offset);

    DisplacedPosition = Position + offset;
    Normal = normalize(cross(binormal, tangent));
}

#endif

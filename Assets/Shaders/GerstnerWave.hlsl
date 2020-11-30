float3 GerstnerWave(float3 pos, float phase, float time, float gravity, float depth, float3 dir, float amplitude) {
    float angularFreq = sqrt(gravity * length(dir) * tanh(length(dir) * depth));
    float theta = dir.x * pos.x +  dir.z * pos.z - angularFreq * time - phase;
    float x = -(dir.x / length(dir)) * (amplitude / tanh(length(dir) * depth)) * sin(theta);
    float y = amplitude * cos(theta);
    float z = -(dir.z / length(dir)) * (amplitude / tanh(length(dir) * depth)) * sin(theta);
    return float3(x, y, z);
}

float3 GerstnerWaveSum(float3 pos, float phase, float time, float gravity, float depth,
        float3 dirs[4], float speeds[4], float amplitudes[4]) {
    
    float3 p = pos;

    for(int i = 0; i < 4; i++) {
        p += GerstnerWave(pos, phase, time * speeds[i], gravity, depth, dirs[i], amplitudes[i]);
    }

    return p;
}

float3 CalculateNormal(float3 pos, float3 neighbour1, float3 neighbour2) {
    float3 d1 = normalize(neighbour1 - pos);
    float3 d2 = normalize(neighbour2 - pos);
    return normalize(cross(d1, d2));
}

void GetVertexPos_float(float3 pos, float3 islandPos, float phase, float time, float gravity, float depth, float neighbourDist,
        float3 dir1, float3 dir2, float3 dir3, float3 dir4, float speed1, float speed2, float speed3, float speed4,
        float amplitude1, float amplitude2, float amplitude3, float amplitude4, out float3 posOut, out float3 normalOut) {
    
    // Put parameters into arrays so they can be indexed
    float3 dirs[] = { dir1, dir2, dir3, dir4 };
    float speeds[] = { speed1, speed2, speed3, speed4 };
    float amplitudes[] = { amplitude1, amplitude2, amplitude3, amplitude4 };

    posOut = GerstnerWaveSum(pos, phase, time, gravity, depth, dirs, speeds, amplitudes);

    // float3 p = float3(posOut.x, 0, posOut.z);
    // float islandDist = length(p - islandPos);
    // if(islandDist < 30) posOut.y = posOut.y - 0.01f * pow(30 - islandDist, 2);

    float3 neighbour1 = GerstnerWaveSum(pos + float3(0, 0, neighbourDist), phase, time, gravity, depth, dirs, speeds, amplitudes);
    float3 neighbour2 = GerstnerWaveSum(pos + float3(neighbourDist, 0, 0), phase, time, gravity, depth, dirs, speeds, amplitudes);
    normalOut = CalculateNormal(posOut, neighbour1, neighbour2);
}

void GetColour_float(float height, float offset, float4 colour1, float4 colour2, float amplitude1, float amplitude2, float amplitude3, float amplitude4, out float4 colour) {
    float amp = amplitude1 + amplitude2 + amplitude3 + amplitude4;
    float t = (height + amp + offset) / (amp * 2);
    t = saturate(t);
    colour = lerp(colour1, colour2, t);
}
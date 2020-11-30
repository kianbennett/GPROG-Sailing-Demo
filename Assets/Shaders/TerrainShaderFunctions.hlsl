// Returns the percentage of value between a and b
float inverseLerp(float a, float b, float value) {
    // Saturate clamps the value between 0 and 1 (incase value is outside the range of a - b)
    return saturate((value - a) / (b - a));
}

// Shader graph doesn't support arrays so parameters must be specified individually
void GetColourFromHeight_float(float posY, float minHeight, float maxHeight, float regions,
        float4 colour1, float4 colour2, float4 colour3, float4 colour4, float4 colour5, float4 colour6,
        float height1, float height2, float height3, float height4, float height5, float height6,
        float blend1, float blend2, float blend3, float blend4, float blend5, float blend6,
        out float4 colourOut) {

    float height = inverseLerp(minHeight, maxHeight, posY);

    // Store values in arrays so they can be access by index
    float4 colours[] = { colour1, colour2, colour3, colour4, colour5, colour6 };
    float heights[] = { height1, height2, height3, height4, height5, height6 };
    float blends[] = { blend1, blend2, blend3, blend4, blend5, blend6 };

    int regionCount = (int) regions;

    // For each colour region set the colour, interpolate if the pixel lies within the blend area
    for(int i = 0; i < regionCount; i++) {
        // Make sure next and previous heights don't go beyond limits of array
        float heightPrev = 0, heightNext = 1;
        if(i > 0) heightPrev = heights[i - 1];
        if(i < regionCount - 1) heightNext = heights[i + 1];

        float4 colourPrev = colour1;
        if(i > 0) colourPrev = colours[i - 1];

        float distNext = heightNext - heights[i];
        float distPrev = heights[i] - heightPrev;

        // Check height is within limits of current region
        if(height >= heights[i] - distPrev / 2 && height <= heights[i] + distNext / 2) {
            if(height <= heights[i] - distPrev / 2 * blends[i]) {
                colourOut = colourPrev;
            } else if(height >= heights[i] - distPrev / 2 * blends[i] && height <= heights[i] + distNext / 2 * blends[i]) {
                // If height is within limits of blend amount, interpolate between current and previous colour
                colourOut = lerp(colourPrev, colours[i], inverseLerp(heights[i] - distPrev / 2 * blends[i], heights[i] + distPrev / 2 * blends[i], height));
            } else {
                colourOut = colours[i];
            }
            break;
        }

        // If the colour isn't within the limits of any region (and doesn't break the loop
        // then it will default to the last colour (to make sure the top of the terrain is completely coloured)
        colourOut = colours[i];
    }
}
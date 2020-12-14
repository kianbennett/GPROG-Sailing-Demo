using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ShipForces {

    public static Vector3 CalculateBuoyancyForce(TriangleData triangle, float waterDensity) {
        Vector3 force = -waterDensity * Physics.gravity.y * triangle.distToWater * triangle.area * triangle.normal;
        force.x = force.z = 0;
        return CheckForceIsValid(force);
    }

    public static Vector3 CalculateViscousWaterResistance(TriangleData triangle, float waterDensity, float coeff) {
        //We need the tangential velocity 
        //Projection of the velocity on the plane with the normal normalvec
        //http://www.euclideanspace.com/maths/geometry/elements/plane/lineOnPlane/
        Vector3 a = triangle.normal;
        Vector3 b = triangle.velocity;
        Vector3 velocityTangent = Vector3.Cross(a, (Vector3.Cross(b, a) / a.magnitude)) / a.magnitude;

        //The direction of the tangential velocity (-1 to get the flow which is in the opposite direction)
        Vector3 tangentialDirection = velocityTangent.normalized * -1f;

        //The speed of the triangle as if it was in the tangent's direction
        Vector3 velocity = triangle.velocity.magnitude * tangentialDirection;

        //The final resistance force
        Vector3 force = 0.5f * waterDensity * coeff * velocity.magnitude * velocity * triangle.area;
        return CheckForceIsValid(force);
    }

    public static Vector3 CalculatePressureDrag(TriangleData triangle, float pressureFalloff, float pressureCoeff1, float pressureCoeff2, float suctionFalloff, float suctionCoeff1, float suctionCoeff2) {
        //Modify for different turning behavior and planing forces
        //f_p and f_S - falloff power, should be smaller than 1

        float velocity = triangle.velocity.magnitude;
        //A reference speed used when modifying the parameters
        float velocityReference = velocity;
        velocity = velocity / velocityReference;

        Vector3 force = Vector3.zero;

        if (triangle.cosTheta > 0f) {
            force = -(pressureCoeff1 * velocity + pressureCoeff2 * (velocity * velocity)) * triangle.area * Mathf.Pow(triangle.cosTheta, pressureFalloff) * triangle.normal;
        } else {
            force = (suctionCoeff1 * velocity + suctionCoeff2 * (velocity * velocity)) * triangle.area * Mathf.Pow(Mathf.Abs(triangle.cosTheta), suctionFalloff) * triangle.normal;
        }

        return CheckForceIsValid(force);
    }

    public static Vector3 CalculateSlammingForce(TriangleData triangle, SlammingForceData slammingData, float slammingForceAmount, float bodyArea, float bodyMass) {
        //To capture the response of the fluid to sudden accelerations or penetrations

        //Add slamming if the normal is in the same direction as the velocity (the triangle is not receding from the water)
        //Also make sure thea area is not 0, which it sometimes is for some reason
        if (triangle.cosTheta < 0f || slammingData.originalArea <= 0f) {
            return Vector3.zero;
        }
        
        //Step 1 - Calculate acceleration
        //Volume of water swept per second
        Vector3 dV = slammingData.submergedArea * slammingData.velocity;
        Vector3 dV_previous = slammingData.previousSubmergedArea * slammingData.previousVelocity;

        //Calculate the acceleration of the center point of the original triangle (not the current underwater triangle)
        //But the triangle the underwater triangle is a part of
        Vector3 accVec = (dV - dV_previous) / (slammingData.originalArea * Time.fixedDeltaTime);

        //The magnitude of the acceleration
        float acc = accVec.magnitude;

        //Step 2 - Calculate slamming force
        Vector3 F_stop = bodyMass * triangle.velocity * ((2f * triangle.area) / bodyArea);

        float p = 2f;
        float acc_max = acc;
        Vector3 slammingForce = -Mathf.Pow(Mathf.Clamp01(acc / acc_max), p) * triangle.cosTheta * F_stop * slammingForceAmount;

        return CheckForceIsValid(slammingForce);
    }

    public static float ResistanceCoefficient(float velocity, float length) {
        float vicsocity = 0.000001f;
        //Reynolds number
        float Rn = (velocity * length) / vicsocity;
        float coeff = 0.075f / Mathf.Pow((Mathf.Log10(Rn) - 2f), 2f);
        return coeff;
    }

    //Check that a force is not NaN
    public static Vector3 CheckForceIsValid(Vector3 force) {
        return !float.IsNaN(force.x + force.y + force.z) ? force : Vector3.zero;
    }
}

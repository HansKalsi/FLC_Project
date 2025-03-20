using UnityEngine;

public class LawnmowerController : MonoBehaviour
{
    // Fuzzy Distance Thresholds
    private float closeThreshold = 1f;
    private float mediumThreshold = 3f;
    private float farThreshold = 5f;

    // Fuzzy Angle Thresholds
    private float smallAngleThreshold = 15f;
    private float largeAngleThreshold = 60f;

    // Speeds
    private float minSpeed = 1f;
    private float maxSpeed = 5f;

    // Turn Speeds
    private float minTurnSpeed = 0f;
    private float maxTurnSpeed = 90f;

    // Object Memory
    private GameObject currentGrassTarget;

    // Raycast Settings
    private int numRays = 100;
    private float rayLength = 10f;
    private float rayYOffset = 0.5f;
    private float maxRayAngle = 25f;

    void Update()
    {
        // Cast rays to detect grass
        CastRaysAndComputeCoverage(out float coverage, out float avgHitDistance);

        MoveTowardsTarget(coverage, avgHitDistance);
    }

    // TODO: Have two sets of raycasts, so the lawnmower can turn towards the most grass
    void CastRaysAndComputeCoverage(out float coverage, out float avgHitDistance) {
        int hits = 0;
        float distanceSum = 0f;

        Vector3 originPos = transform.position - new Vector3(0, rayYOffset, 0);

        // Calculate the distance between each raycast
        float angleStep = maxRayAngle * 2f / (numRays - 1);

        for (int i = 0; i < numRays; i++) {
            float angleOffset = -maxRayAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0f, angleOffset, 0f) * transform.forward;

            // Raycast
            if (Physics.Raycast(originPos, direction, out RaycastHit hit, rayLength)) {
                if (hit.collider.CompareTag("Grass")) {
                    hits++;
                    distanceSum += hit.distance;
                }
            }

            // TODO: Make this a toggle
            // Visualise the raycasts
            Debug.DrawRay(originPos, direction * rayLength, Color.red);
        }

        // Calculate Coverage
        if (hits > 0) {
            coverage = (float)hits / numRays;
            avgHitDistance = distanceSum / hits;
        } else {
            // No grass hit by any raycast
            coverage = 0f;
            avgHitDistance = 0f;
        }
    }

    // Fuzzy Movement
    void MoveTowardsTarget(float coverage, float avgHitDistance) {
        Debug.Log($"Coverage: {coverage}, Avg Hit Distance: {avgHitDistance}");
        // Get Fuzzy Memberships
        // Coverage Memberships
        float noneCovMem = FuzzyCoverageNone(coverage);
        float partialCovMem = FuzzyCoveragePartial(coverage);
        float fullCovMem = FuzzyCoverageFull(coverage);
        // Distance Memberships
        // Only delve into other memberships if grass is found
        float closeMem = avgHitDistance > 0f ? FuzzyDistanceClose(avgHitDistance) : 0f;
        float mediumMem = avgHitDistance > 0f ? FuzzyDistanceMedium(avgHitDistance) : 0f;
        float farMem = avgHitDistance > 0f ? FuzzyDistanceFar(avgHitDistance) : 0f;

        // Fuzzy Rules
        // Combine results to get the 'desired speed' and 'desired turn speed'

        // Rule 1 - If no coverage (aka no grass detected), speed is NONE & turn speed is MAX
        float rule1Strength = noneCovMem;
        float rule1Speed = 0f;
        float rule1TurnSpeed = maxTurnSpeed;

        // Rule 2 - If distance is FAR & coverage is PARTIAL, speed is MAX & turn speed is TINY
        float rule2Strength = farMem * partialCovMem;
        float rule2Speed = maxSpeed;
        float rule2TurnSpeed = maxTurnSpeed / 8;

        // Rule 3 - If distance is FAR & coverage is FULL, speed is MAX & turn speed is MIN
        float rule3Strength = farMem * fullCovMem;
        float rule3Speed = maxSpeed;
        float rule3TurnSpeed = minTurnSpeed;

        // Rule 4 - If distance is CLOSE & coverage is PARTIAL, speed is MIN & turn speed is MIN-MEDIUM
        float rule4Strength = closeMem * partialCovMem;
        float rule4Speed = minSpeed;
        float rule4TurnSpeed = maxTurnSpeed * 0.3f;

        // Rule 5 - If distance is MEDIUM & coverage is PARTIAL, speed is MEDIUM & turn speed is MEDIUM
        float rule5Strength = mediumMem * partialCovMem;
        float rule5Speed = maxSpeed / 2;
        float rule5TurnSpeed = maxTurnSpeed / 2;

        // Rule 6 - If distance is MEDIUM & coverage is FULL, speed is MEDIUM-MAX & turn speed is MIN
        float rule6Strength = mediumMem * fullCovMem;
        float rule6Speed = maxSpeed * 0.7f;
        float rule6TurnSpeed = minTurnSpeed;

        // Rule 7 - If distance is CLOSE & coverage is FULL, speed is MIN & turn speed is MIN
        float rule7Strength = fullCovMem * closeMem;
        float rule7Speed = minSpeed;
        float rule7TurnSpeed = minTurnSpeed;


        // Weighted Average Defuzzification
        float totalStrength = rule1Strength + rule2Strength + rule3Strength + rule4Strength + rule5Strength + rule6Strength + rule7Strength;
        // If no rules match, do nothing
        // FIXME: bad performance - above code shouldn't run if no grass
        if (Mathf.Abs(totalStrength) < 0.0001f) return;

        float weightedSpeedSum = 
            (rule1Strength * rule1Speed) +
            (rule2Strength * rule2Speed) +
            (rule3Strength * rule3Speed) +
            (rule4Strength * rule4Speed) +
            (rule5Strength * rule5Speed) +
            (rule6Strength * rule6Speed) +
            (rule7Strength * rule7Speed);

        float weightedTurnSpeedSum =
            (rule1Strength * rule1TurnSpeed) +
            (rule2Strength * rule2TurnSpeed) +
            (rule3Strength * rule3TurnSpeed) +
            (rule4Strength * rule4TurnSpeed) +
            (rule5Strength * rule5TurnSpeed) +
            (rule6Strength * rule6TurnSpeed) +
            (rule7Strength * rule7TurnSpeed);

        float finalSpeed = weightedSpeedSum / totalStrength;
        float finalTurnSpeed = weightedTurnSpeedSum / totalStrength;

        // Apply movement
        // Turn first, so we face the target
        // Rotate towards target
        transform.Rotate(Vector3.up, finalTurnSpeed * Time.deltaTime);
        // Move forward
        transform.Translate(finalSpeed * Time.deltaTime * Vector3.forward);
    }

    // Coverage Fuzzy Membership Functions
    float FuzzyCoverageNone(float value) {
        if (value <= 0f) return 1f;

        float range = 0.3f;
        if (value >= range) return 0f;
        return 1f - (value / range);
    }

    float FuzzyCoveragePartial(float value) {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 0f;

        float midpoint = 0.5f;
        if (value < midpoint) {
            return value / midpoint;
        }
        return (1f - value) / (1f - midpoint);
    }

    float FuzzyCoverageFull(float value) {
        float range = 0.7f;
        if (value <= range) return 0f;
        if (value >= 1f) return 1f;
        return (value - range) / (1f - range);
    }

    // Distance Fuzzy Membership Functions
    float FuzzyDistanceClose(float value) {
        // Full membership if distance is less than close threshold
        if (value <= closeThreshold) return 1f;
        // No membership if distance is greater than medium threshold
        if (value >= mediumThreshold) return 0f;
        // If in between, linearly interpolate
        return 1f - (value - closeThreshold) / (mediumThreshold - closeThreshold);
    }

    float FuzzyDistanceMedium(float value) {
        // No membersip if distance is less than close threshold or greater than far threshold
        if (value <= closeThreshold || value >= farThreshold) return 0f;
        // Calculate membership based on distance from the middle
        if (value < mediumThreshold) return (value - closeThreshold) / (mediumThreshold - closeThreshold);
        return 1f - (value - mediumThreshold) / (farThreshold - mediumThreshold);
    }

    float FuzzyDistanceFar(float value) {
        // Full membership if distance is greater than far threshold
        if (value >= farThreshold) return 1f;
        // No membership if distance is less than medium threshold
        if (value <= mediumThreshold) return 0f;
        // If in between, linearly interpolate
        return (value - mediumThreshold) / (farThreshold - mediumThreshold);
    }

    // Angle Fuzzy Membership Functions
    float FuzzyAngleSmall(float value) {
        // Full membership if angle is less than small threshold
        if (value <= smallAngleThreshold) return 1f;
        // No membership if angle is greater than large threshold
        if (value >= largeAngleThreshold) return 0f;
        // If in between, linearly interpolate
        return 1f - (value - smallAngleThreshold) / (largeAngleThreshold - smallAngleThreshold);
    }

    float FuzzyAngleLarge(float value) {
        // Full membership if angle is greater than large threshold
        if (value >= largeAngleThreshold) return 1f;
        // No membership if angle is less than small threshold
        if (value <= smallAngleThreshold) return 0f;
        // If in between, linearly interpolate
        return (value - smallAngleThreshold) / (largeAngleThreshold - smallAngleThreshold);
    }
}

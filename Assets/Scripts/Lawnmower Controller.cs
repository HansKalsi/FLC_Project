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

    // Raycast Settings
    private int numRays = 15;
    private float rayLength = 100f;
    private float rayYOffset = 0.5f;
    private float maxRayAngle = 30f;

    void Update()
    {
        // Cast rays to detect grass
        CastRaysAndComputeCoverage(out float leftCoverage, out float rightCoverage, out float avgHitDistance, out float obstacleDistance);

        MoveTowardsTarget(leftCoverage, rightCoverage, avgHitDistance, obstacleDistance);
    }

    void CastRaysAndComputeCoverage(out float leftCoverage, out float rightCoverage, out float avgGrassDistance, out float obstacleDistance) {
        // Default set as if no grass or obstacle hit by any raycast
        leftCoverage = -1f;
        rightCoverage = -1f;
        avgGrassDistance = -1f;
        obstacleDistance = -1f;
        int hitsLeft = 0;
        int hitsRight = 0;
        float distanceSum = 0f;
        float closestObstacleDistance = 0f;

        Vector3 originPos = transform.position - new Vector3(0, rayYOffset, 0);

        // Calculate the distance between each raycast
        float angleStep = maxRayAngle / (numRays - 1);

        for (int i = 0; i < numRays; i++) {
            float angleOffsetLeft = -maxRayAngle + angleStep * i;
            float angleOffsetRight = maxRayAngle - angleStep * i;
            Vector3 directionLeft = Quaternion.Euler(0f, angleOffsetLeft, 0f) * transform.forward;
            Vector3 directionRight = Quaternion.Euler(0f, angleOffsetRight, 0f) * transform.forward;

            // Raycast
            if (Physics.Raycast(originPos, directionLeft, out RaycastHit hitLeft, rayLength)) {
                if (hitLeft.collider.CompareTag("Grass")) {
                    hitsLeft++;
                    distanceSum += hitLeft.distance;
                }
                if (hitLeft.collider.CompareTag("Obstacle") && hitLeft.distance < closestObstacleDistance) {
                    closestObstacleDistance = hitLeft.distance;
                }
            }
            // Raycast opposite direction
            if (Physics.Raycast(originPos, directionRight, out RaycastHit hitRight, rayLength)) {
                if (hitRight.collider.CompareTag("Grass")) {
                    hitsRight++;
                    distanceSum += hitRight.distance;
                }
                if (hitRight.collider.CompareTag("Obstacle") && hitRight.distance < closestObstacleDistance) {
                    closestObstacleDistance = hitRight.distance;
                }
            }

            // TODO: Make this a toggle via the GUI
            // Visualise the raycasts
            Debug.DrawRay(originPos, directionLeft * rayLength, Color.red);
            Debug.DrawRay(originPos, directionRight * rayLength, Color.green);
        }

        // Calculate Coverage
        if (hitsLeft > 0) {
            leftCoverage = (float)hitsLeft / numRays;
        }
        if (hitsRight > 0) {
            rightCoverage = (float)hitsRight / numRays;
        }
        if (hitsLeft > 0 || hitsRight > 0) {
            avgGrassDistance = distanceSum / (hitsLeft + hitsRight);
        }
        if (closestObstacleDistance > 0) {
            obstacleDistance = closestObstacleDistance;
        }
    }

    // Fuzzy Movement
    void MoveTowardsTarget(float leftCoverage, float rightCoverage, float avgHitDistance, float obstacleDistance) {
        // Debug.Log($"leftCoverage: {leftCoverage}, rightCoverage: {rightCoverage}, Avg Hit Distance: {avgHitDistance}, Obstacle Distance: {obstacleDistance}");
        // Coverage Difference (more on left = positive, more on right = negative)
        float coverageDifference = -1f;
        if (leftCoverage > -1f && rightCoverage > -1f) {
            Debug.Log("Both coverage");
            coverageDifference = leftCoverage - rightCoverage;
        } else if (leftCoverage > -1f) {
            Debug.Log("Left coverage");
            coverageDifference = leftCoverage;
        } else if (rightCoverage > -1f) {
            Debug.Log("Right coverage");
            coverageDifference = -rightCoverage;
        }
        Debug.Log($"Coverage Difference: {coverageDifference}");

        // ---Fuzzy Memberships---
        // Coverage Memberships
        float noneCovMem = FuzzyCoverageNone(coverageDifference);
        float fullLeftCovMem = FuzzyCoverageFullLeft(coverageDifference);
        float partialLeftCovMem = FuzzyCoveragePartialLeft(coverageDifference);
        float balancedCovMem = FuzzyCoverageBalanced(coverageDifference);
        float partialRightCovMem = FuzzyCoveragePartialRight(coverageDifference);
        float fullRightCovMem = FuzzyCoverageFullRight(coverageDifference);
        Debug.Log("none Coverage membership: " + noneCovMem);
        Debug.Log("full left Coverage membership: " + fullLeftCovMem);
        Debug.Log("partial left Coverage membership: " + partialLeftCovMem);
        Debug.Log("balanced Coverage membership: " + balancedCovMem);
        Debug.Log("partial right Coverage membership: " + partialRightCovMem);
        Debug.Log("full right Coverage membership: " + fullRightCovMem);

        // Distance Memberships
        // To Grass
        float closeGrassMem = FuzzyDistanceClose(avgHitDistance);
        float mediumGrassMem = FuzzyDistanceMedium(avgHitDistance);
        float farGrassMem = FuzzyDistanceFar(avgHitDistance);

        // To Obstacle
        float closeObstacleMem = FuzzyDistanceClose(obstacleDistance);
        float mediumObstacleMem = FuzzyDistanceMedium(obstacleDistance);
        float farObstacleMem = FuzzyDistanceFar(obstacleDistance);

        // Fuzzy Rules
        // Combine results to get the 'desired speed' and 'desired turn speed'

        // Rule 1 - If no coverage (aka no grass detected), speed is NONE & turn speed is MAX (clockwise as default)
        float rule1Strength = noneCovMem;
        float rule1Speed = 0f;
        float rule1TurnSpeed = maxTurnSpeed;

        // Rule 2 - If left coverage is full and grass is close, speed is MIN & turn speed is NONE
        float rule2Strength = fullLeftCovMem * closeGrassMem;
        float rule2Speed = minSpeed;
        float rule2TurnSpeed = -maxTurnSpeed / 2;

        // Rule 3 - If left coverage is full and grass is medium, speed is MEDIUM & turn speed is NONE
        float rule3Strength = fullLeftCovMem * mediumGrassMem;
        float rule3Speed = maxSpeed / 2;
        float rule3TurnSpeed = -maxTurnSpeed / 2;

        // Rule 4 - If left coverage is full and grass is far, speed is MAX & turn speed is NONE
        float rule4Strength = fullLeftCovMem * farGrassMem;
        float rule4Speed = maxSpeed;
        float rule4TurnSpeed = -maxTurnSpeed / 2;

        // Rule 5 - If left coverage is partial and grass is close, speed is MIN & turn speed is HIGH
        float rule5Strength = partialLeftCovMem * closeGrassMem;
        float rule5Speed = minSpeed;
        float rule5TurnSpeed = -maxTurnSpeed;

        // Rule 6 - If left coverage is partial and grass is medium, speed is MEDIUM & turn speed is LOW-MEDIUM
        float rule6Strength = partialLeftCovMem * mediumGrassMem;
        float rule6Speed = maxSpeed / 2;
        float rule6TurnSpeed = -maxTurnSpeed;

        // Rule 7 - If left coverage is partial and grass is far, speed is MAX & turn speed is MEDIUM
        float rule7Strength = partialLeftCovMem * farGrassMem;
        float rule7Speed = maxSpeed;
        float rule7TurnSpeed = -maxTurnSpeed;

        // Rule 8 - If balanced coverage and grass is close, speed is MIN & turn speed is NONE
        float rule8Strength = balancedCovMem * closeGrassMem;
        float rule8Speed = minSpeed;
        float rule8TurnSpeed = 0f;

        // Rule 9 - If balanced coverage and grass is medium, speed is MEDIUM & turn speed is NONE
        float rule9Strength = balancedCovMem * mediumGrassMem;
        float rule9Speed = maxSpeed / 2;
        float rule9TurnSpeed = 0f;

        // Rule 10 - If balanced coverage and grass is far, speed is MAX & turn speed is NONE
        float rule10Strength = balancedCovMem * farGrassMem;
        float rule10Speed = maxSpeed;
        float rule10TurnSpeed = 0f;

        // Rule 11 - If right coverage is partial and grass is close, speed is MIN & turn speed is HIGH
        float rule11Strength = partialRightCovMem * closeGrassMem;
        float rule11Speed = minSpeed;
        float rule11TurnSpeed = maxTurnSpeed;

        // Rule 12 - If right coverage is partial and grass is medium, speed is MEDIUM & turn speed is LOW-MEDIUM
        float rule12Strength = partialRightCovMem * mediumGrassMem;
        float rule12Speed = maxSpeed / 2;
        float rule12TurnSpeed = maxTurnSpeed;

        // Rule 13 - If right coverage is partial and grass is far, speed is MAX & turn speed is MEDIUM
        float rule13Strength = partialRightCovMem * farGrassMem;
        float rule13Speed = maxSpeed;
        float rule13TurnSpeed = maxTurnSpeed;

        // Rule 14 - If right coverage is full and grass is close, speed is MIN & turn speed is NONE
        float rule14Strength = fullRightCovMem * closeGrassMem;
        float rule14Speed = minSpeed;
        float rule14TurnSpeed = maxTurnSpeed / 2;

        // Rule 15 - If right coverage is full and grass is medium, speed is MEDIUM & turn speed is NONE
        float rule15Strength = fullRightCovMem * mediumGrassMem;
        float rule15Speed = maxSpeed / 2;
        float rule15TurnSpeed = maxTurnSpeed / 2;

        // Rule 16 - If right coverage is full and grass is far, speed is MAX & turn speed is NONE
        float rule16Strength = fullRightCovMem * farGrassMem;
        float rule16Speed = maxSpeed;
        float rule16TurnSpeed = maxTurnSpeed / 2;

        // Rule 17 - If obstacle is close, speed is MIN & turn speed is MAX
        float rule17Strength = closeObstacleMem;
        float rule17Speed = minSpeed;
        float rule17TurnSpeed = maxTurnSpeed;

        // Rule 18 - If obstacle is medium, speed is MEDIUM & turn speed is NONE
        float rule18Strength = mediumObstacleMem;
        float rule18Speed = maxSpeed / 2;
        float rule18TurnSpeed = 0f;

        // Rule 19 - If obstacle is far, speed is MAX & turn speed is NONE
        float rule19Strength = farObstacleMem;
        float rule19Speed = maxSpeed;
        float rule19TurnSpeed = 0f;

        // Weighted Average Defuzzification
        float totalStrengthSpeed = rule1Strength + rule2Strength + rule3Strength + rule4Strength + rule5Strength + rule6Strength + rule7Strength + rule8Strength + rule9Strength + rule10Strength + rule11Strength + rule12Strength + rule13Strength + rule14Strength + rule15Strength + rule16Strength + rule17Strength + rule18Strength + rule19Strength;
        float totalStrengthTurn = rule1Strength + rule2Strength + rule3Strength + rule4Strength + rule5Strength + rule6Strength + rule7Strength + rule8Strength + rule9Strength + rule10Strength + rule11Strength + rule12Strength + rule13Strength + rule14Strength + rule15Strength + rule16Strength + rule17Strength + rule18Strength + rule19Strength;
        // FIXME: bad performance - above code shouldn't run if no grass
        if (Mathf.Abs(totalStrengthSpeed) < 0.0001f) {
            Debug.Log("No speed strength " + totalStrengthSpeed);
            return; 
        }
        if (Mathf.Abs(totalStrengthTurn) < 0.0001f) {
            Debug.Log("No turn strength " + totalStrengthTurn);
            return;
        }

        float weightedSpeedSum = 
            (rule1Strength * rule1Speed) +
            (rule2Strength * rule2Speed) +
            (rule3Strength * rule3Speed) +
            (rule4Strength * rule4Speed) +
            (rule5Strength * rule5Speed) +
            (rule6Strength * rule6Speed) +
            (rule7Strength * rule7Speed) +
            (rule8Strength * rule8Speed) +
            (rule9Strength * rule9Speed) +
            (rule10Strength * rule10Speed) +
            (rule11Strength * rule11Speed) +
            (rule12Strength * rule12Speed) +
            (rule13Strength * rule13Speed) +
            (rule14Strength * rule14Speed) +
            (rule15Strength * rule15Speed) +
            (rule16Strength * rule16Speed) +
            (rule17Strength * rule17Speed) +
            (rule18Strength * rule18Speed) +
            (rule19Strength * rule19Speed);

        float weightedTurnSpeedSum =
            (rule1Strength * rule1TurnSpeed) +
            (rule2Strength * rule2TurnSpeed) +
            (rule3Strength * rule3TurnSpeed) +
            (rule4Strength * rule4TurnSpeed) +
            (rule5Strength * rule5TurnSpeed) +
            (rule6Strength * rule6TurnSpeed) +
            (rule7Strength * rule7TurnSpeed) +
            (rule8Strength * rule8TurnSpeed) +
            (rule9Strength * rule9TurnSpeed) +
            (rule10Strength * rule10TurnSpeed) +
            (rule11Strength * rule11TurnSpeed) +
            (rule12Strength * rule12TurnSpeed) +
            (rule13Strength * rule13TurnSpeed) +
            (rule14Strength * rule14TurnSpeed) +
            (rule15Strength * rule15TurnSpeed) +
            (rule16Strength * rule16TurnSpeed) +
            (rule17Strength * rule17TurnSpeed) +
            (rule18Strength * rule18TurnSpeed) +
            (rule19Strength * rule19TurnSpeed);

        // Debug.Log($"Weighted Speed Sum: {weightedSpeedSum}, Weighted Turn Speed Sum: {weightedTurnSpeedSum}");

        float finalSpeed = weightedSpeedSum / totalStrengthSpeed;
        float finalTurnSpeed = weightedTurnSpeedSum / totalStrengthTurn;

        Debug.Log($"Final Speed: {finalSpeed}, Final Turn Speed: {finalTurnSpeed}");

        // Apply movement
        // Turn first, so we face the target
        // Rotate towards target
        transform.Rotate(Vector3.up, finalTurnSpeed * Time.deltaTime);
        // Move forward
        transform.Translate(finalSpeed * Time.deltaTime * Vector3.forward);
    }

    // Coverage Fuzzy Membership Functions
    float FuzzyCoverageNone(float value) {
        if (value == -1f) return 1f;
        return 0f;
    }

    float FuzzyCoverageFullLeft(float value) {
        float range = 0.7f;
        if (value <= range) return 0f;
        if (value >= 1f) return 1f;
        return (value - range) / (1f - range);
    }

    float FuzzyCoveragePartialLeft(float value) {
        if (value <= 0f) return 0f;
        if (value >= 1f) return 0f;

        float range = 0.7f;
        if (value < range) {
            return (1f - value) / range;
        }
        return value / (1f - range);
    }

    float FuzzyCoverageBalanced(float value) {
        if (value == 0f) return 1f;
        return 0f;
    }

    float FuzzyCoveragePartialRight(float value) {
        if (value <= -1f) return 0f;
        if (value >= 0f) return 0f;

        float range = -0.7f;
        if (value > range) {
            return (1f - value) / (1f + range);
        }
        return value / -range;
    }

    float FuzzyCoverageFullRight(float value) {
        if (value == -1f) return 0f;
        float range = -0.7f;
        if (value > -1f && value <= range) return 1f;
        if (value >= range) return 0f;
        return (value - range) / (1f - range);
    }

    // float FuzzyCoveragePartial(float value) {
    //     if (value <= 0f) return 0f;
    //     if (value >= 1f) return 0f;

    //     float midpoint = 0.5f;
    //     if (value < midpoint) {
    //         return value / midpoint;
    //     }
    //     return (1f - value) / (1f - midpoint);
    // }

    // float FuzzyCoverageFull(float value) {
    //     float range = 0.7f;
    //     if (value <= range) return 0f;
    //     if (value >= 1f) return 1f;
    //     return (value - range) / (1f - range);
    // }

    // Distance Fuzzy Membership Functions
    float FuzzyDistanceClose(float value) {
        if (value == -1f) return 0f;
        // Full membership if distance is less than close threshold
        if (value <= closeThreshold) return 1f;
        // No membership if distance is greater than medium threshold
        if (value >= mediumThreshold) return 0f;
        // If in between, linearly interpolate
        return 1f - (value - closeThreshold) / (mediumThreshold - closeThreshold);
    }

    float FuzzyDistanceMedium(float value) {
        if (value == -1f) return 0f;
        // No membersip if distance is less than close threshold or greater than far threshold
        if (value <= closeThreshold || value >= farThreshold) return 0f;
        // Calculate membership based on distance from the middle
        if (value < mediumThreshold) return (value - closeThreshold) / (mediumThreshold - closeThreshold);
        return 1f - (value - mediumThreshold) / (farThreshold - mediumThreshold);
    }

    float FuzzyDistanceFar(float value) {
        if (value == -1f) return 0f;
        // Full membership if distance is greater than far threshold
        if (value >= farThreshold) return 1f;
        // No membership if distance is less than medium threshold
        if (value <= mediumThreshold) return 0f;
        // If in between, linearly interpolate
        return (value - mediumThreshold) / (farThreshold - mediumThreshold);
    }

    // // Angle Fuzzy Membership Functions
    // float FuzzyAngleSmall(float value) {
    //     // Full membership if angle is less than small threshold
    //     if (value <= smallAngleThreshold) return 1f;
    //     // No membership if angle is greater than large threshold
    //     if (value >= largeAngleThreshold) return 0f;
    //     // If in between, linearly interpolate
    //     return 1f - (value - smallAngleThreshold) / (largeAngleThreshold - smallAngleThreshold);
    // }

    // float FuzzyAngleLarge(float value) {
    //     // Full membership if angle is greater than large threshold
    //     if (value >= largeAngleThreshold) return 1f;
    //     // No membership if angle is less than small threshold
    //     if (value <= smallAngleThreshold) return 0f;
    //     // If in between, linearly interpolate
    //     return (value - smallAngleThreshold) / (largeAngleThreshold - smallAngleThreshold);
    // }
}

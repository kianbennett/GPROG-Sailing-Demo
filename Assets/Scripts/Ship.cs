using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode]
public class Ship : MonoBehaviour {

    public new Rigidbody rigidbody;
    public float speed, sailForce, anchorForce, cannonBallForce;
    public Transform cameraPosition;
    public Transform wheel, rudder, capstan;
    public Transform mainMast;
    public Transform[] sails, sailSupports;

    [Header("Cannons")]
    public Cannonball cannonBallPrefab;
    public Transform[] cannonHolders;
    public Transform[] cannons;
    public Transform[] cannonBallSpawnPoints;
    public ParticleSystem[] cannonSmokeParticles;
    public ParticleSystem[] cannonSparkParticles;
    public Light[] cannonLights;
    public LineRenderer cannonGuideRenderer;
    public Transform cannonGuideHitSprite;

    [Header("Ropes")]
    public Transform mastRopePointF;
    public Transform mastRopePointB;
    public Transform sailRopePointL, sailRopePointR;
    public Transform hullRopePointL, hullRopePointR;
    public LineRenderer mastRopeRenderer, sailRopeRendererL, sailRopeRendererR;
    [Range(0f, 1f)]
    public float mastRopeSlack, sailRopeSlackL, sailRopeSlackR;

    [Header("Control Parameters")]
    public bool playerControls;
    [Range(-1f, 1f)]
    public float rudderTurnAmount;
    public float rudderTurnSpeed;
    public float turnTorque;
    [Range(-1f, 1f)]
    public float sailTurnAmount;
    public float sailTurnSpeed;
    [Range(0f, 1f)]
    public float sailLowerAmount;
    public float sailLowerSpeed;

    private float[] sailSupportsOriginY;
    private bool isAnchored;
    private bool isAiming;

    void Awake() {
        sailSupportsOriginY = new float[sailSupports.Length];
        for(int i = 0; i < sailSupports.Length; i++) {
            sailSupportsOriginY[i] = sailSupports[i].localPosition.y;
        }

        sailLowerAmount = 0;
    }

    void Update() {
        float sailTurnInput = 0;

        if(playerControls) {
            // Turn rudder
            float rudderTurnInput = Input.GetAxisRaw("Horizontal");
            rudderTurnAmount += rudderTurnInput * rudderTurnSpeed * Time.deltaTime;
            if(rudderTurnInput == 0 && rudderTurnAmount > -0.2f && rudderTurnAmount < 0.2f) {
                rudderTurnAmount = Mathf.Lerp(rudderTurnAmount, 0f, Time.deltaTime * 2);
            }
            rudderTurnAmount = Mathf.Clamp(rudderTurnAmount, -1f, 1f);

            // Turn sail
            if(Input.GetKey(KeyCode.Q)) sailTurnInput = -1;
            if(Input.GetKey(KeyCode.E)) sailTurnInput = 1;
            sailTurnAmount += sailTurnInput * sailTurnSpeed * Time.deltaTime;
            sailTurnAmount = Mathf.Clamp(sailTurnAmount, -1f, 1f);

            // Lower sail
            float sailLowerInput = Input.GetAxisRaw("Vertical");
            sailLowerAmount += -sailLowerInput * sailLowerSpeed * Time.deltaTime;
            sailLowerAmount = Mathf.Clamp(sailLowerAmount, 0f, 1f);
        }
        
        // Transform objects
        wheel.localRotation = Quaternion.Euler(0, 0, -180 * rudderTurnAmount);
        rudder.localRotation = Quaternion.Euler(0, -65 * rudderTurnAmount, 0);
        mainMast.localRotation = Quaternion.Euler(0, 75 * sailTurnAmount, 0);
        capstan.localRotation = Quaternion.Euler(0, Mathf.MoveTowards(capstan.localRotation.eulerAngles.y, isAnchored ? 350 : 0, Time.deltaTime * 400), 0);
        if(Application.isPlaying) { // Only lower the sails in play mode so the editor doesn't mess with sailSupportsOriginY
            // Adjust the sail lower amount so it's always between 0.1 and 1
            float sailLowerAmountAdjusted = 0.1f + 0.9f * sailLowerAmount;
            for(int i = 0; i < sailSupports.Length; i++) {
                sailSupports[i].localPosition = new Vector3(sailSupports[i].localPosition.x, sailSupportsOriginY[i] * sailLowerAmountAdjusted, sailSupports[i].localPosition.z);
            }
            for(int i = 0; i < sails.Length; i++) {
                sails[i].localScale = new Vector3(sails[i].localScale.x, sailLowerAmountAdjusted, sailLowerAmountAdjusted);
            }
        }

        // Update lights
        foreach(Light light in cannonLights) {
            light.intensity = Mathf.MoveTowards(light.intensity, 0, Time.deltaTime * 120);
        }

        // Update ropes
        sailRopeSlackL = Mathf.Lerp(sailRopeSlackL, sailTurnInput < 0 ? 0f : 0.34f, Time.deltaTime * 5);
        sailRopeSlackR = Mathf.Lerp(sailRopeSlackR, sailTurnInput > 0 ? 0f : 0.34f, Time.deltaTime * 5);

        CalculateRopePoints(mastRopePointF.position, mastRopePointB.position, mastRopeRenderer, mastRopeSlack);
        CalculateRopePoints(hullRopePointL.position, sailRopePointL.position, sailRopeRendererL, sailRopeSlackL);
        CalculateRopePoints(hullRopePointR.position, sailRopePointR.position, sailRopeRendererR, sailRopeSlackR);

        float cameraRotY = CameraController.instance.transform.rotation.eulerAngles.y;
        float cameraRotX = CameraController.instance.cameraContainer.localRotation.eulerAngles.x;
        // The angle the camera is facing relative to the ship
        float lookAngleY = transform.rotation.eulerAngles.y - cameraRotY;
        float lookAngleX = transform.rotation.eulerAngles.x - cameraRotX;
        if(lookAngleY < 0) lookAngleY += 360;
        if(lookAngleX > 180) lookAngleX -= 360;

        // Angle cannons
        int cannonActive = lookAngleY < 180 ? 0 : 1;
        // int cannonInactive = lookAngleY < 180 ? 1 : 0;
        // cannons[cannonActive].localRotation = Quaternion.Euler(Mathf.MoveTowardsAngle(cannons[cannonActive].localRotation.eulerAngles.x, -(lookAngleX + 30), Time.deltaTime * 30), 0, 0);
        // cannons[cannonInactive].localRotation = Quaternion.Euler(Mathf.MoveTowardsAngle(cannons[cannonInactive].localRotation.eulerAngles.x, 0, Time.deltaTime * 30), 0, 0);
        // cannonHolders[cannonActive].localRotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(cannonHolders[cannonActive].localRotation.eulerAngles.x, -lookAngleY, Time.deltaTime * 30), 0);
        // cannonHolders[cannonInactive].localRotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(cannonHolders[cannonInactive].localRotation.eulerAngles.x, 0, Time.deltaTime * 30), 0);
        for(int i = 0; i < 2; i++) {
            bool active = isAiming && cannonActive == i;
            float rotX = Mathf.MoveTowardsAngle(cannons[i].localRotation.eulerAngles.x, active ? -(lookAngleX + 30) : 0, Time.deltaTime * 200);
            float restingRotY = -90 + i * 180;
            float rotY = Mathf.MoveTowardsAngle(cannonHolders[i].localRotation.eulerAngles.y, active ? -lookAngleY : restingRotY, Time.deltaTime * 200);
            cannons[i].localRotation = Quaternion.Euler(rotX, 0, 0);
            cannonHolders[i].localRotation = Quaternion.Euler(0, rotY, 0);
        }
        if(isAiming) {
            calculateCannonGuideLines(cannonActive);
        }

        Time.timeScale = isAiming ? 0.5f : 1.0f;

        if(Cursor.lockState == CursorLockMode.Locked && playerControls) {
            // Set anchored
            if(Input.GetKeyDown(KeyCode.Space)) {
                isAnchored = !isAnchored;
                if(isAnchored) {
                    rigidbody.AddForce(-rigidbody.velocity);
                    rigidbody.AddTorque(transform.right * anchorForce * rigidbody.velocity.magnitude);
                }
            }
            // Aim cannons
            setAiming(Input.GetMouseButton(1));
            if(Input.GetMouseButtonDown(0) && isAiming) {
                fireCannon(cannonActive);
            }
        }
    }

    void FixedUpdate() {
        float power = speed * sailLowerAmount * (isAnchored ? 0 : 1);
        rigidbody.AddForce(transform.forward * power);
        // Vector3 move = transform.forward * power;
        // transform.Translate(move.x, 0, move.z);
        rigidbody.AddTorque(Vector3.up * turnTorque * power * rudderTurnAmount);

        foreach(Transform sail in sails) {
            rigidbody.AddForceAtPosition(transform.forward * power * sailForce, sail.position);
        }
    }

    // 0 = left cannon, 1 = right cannon
    private void fireCannon(int cannon) {
        Cannonball cannonBall = Instantiate(cannonBallPrefab, cannonBallSpawnPoints[cannon].position, cannons[cannon].transform.rotation);
        // Add opposite force to ship
        Vector3 dir = -cannons[cannon].transform.forward * cannonBallForce;
        dir.y = 0; // Take height out of force so it only rocks to the side
        rigidbody.AddForceAtPosition(dir, cannonBallSpawnPoints[cannon].position, ForceMode.Impulse);
        // Play particles
        cannonSmokeParticles[cannon].Play();
        cannonSparkParticles[cannon].Play();

        cannonLights[cannon].intensity = 20;
        CameraController.instance.Shake();
    }

    private void setAiming(bool aiming) {
        isAiming = aiming;
        cannonGuideHitSprite.gameObject.SetActive(aiming);
        cannonGuideRenderer.gameObject.SetActive(aiming);
        CameraController.instance.SetFovOffset(aiming ? -10 : 0);
    }

    // Calculate vertices along the line renderer for a rope connecting two points, defined by a catenary curve
    // From https://math.stackexchange.com/questions/3557767/how-to-construct-a-catenary-of-a-specified-length-through-two-specified-points#mjx-eqn-val1
    public void CalculateRopePoints(Vector3 pos1, Vector3 pos2, LineRenderer lineRenderer, float ropeSlack) {
        // For some reason some ropes can't handle a ropeSlack of zero, not sure why (this works for now)
        if(ropeSlack <= 0.001f) ropeSlack = 0.001f;

        int n = 20;
        Vector3[] positions = new Vector3[n];

        Vector3 horizontalDir = pos2 - pos1;
        horizontalDir.y = 0;

        float x1 = 0, x2 = 1;
        float y1 = pos1.y, y2 = pos2.y;

        float dx = x2 - x1;
        float dy = y2 - y1;
        
        float midX = (x1 + x2) / 2;
        float midY = (y1 + y2) / 2;

        float L = Mathf.Sqrt(dx * dx + dy * dy) + ropeSlack * ropeSlack;

        float r = Mathf.Sqrt(L * L - dy * dy) / dx;

        // Apply Newton's iteration to find A
        float A0 = 0;
        if(r < 3) {
            A0 = Mathf.Sqrt(6 * (r - 1));
        } else {
            A0 = Mathf.Log(2 * r) + Mathf.Log(Mathf.Log(2 * r));
        }

        float A = A0;

        int iter = 0;
        while(Mathf.Abs(r - (float) System.Math.Sinh(A) / A) > 0.00001f) {
            A = A - ((float) System.Math.Sinh(A) - r * A) / ((float) System.Math.Cosh(A) - r);
            iter++;
            if(iter > 10) {
                break;
            }
        }

        float a = dx / (2 * A);
        float b = midX - a * 0.5f * Mathf.Log((1 + dy / L) / (1 - dy / L));
        float c = midY - L / (2 * (float) System.Math.Tanh(A));

        for(int i = 0; i < n; i++) {
            float x = (float) i / (n - 1);
            float y = a * (float) System.Math.Cosh((x - b) / a) + c;
            positions[i] = pos1 + horizontalDir * x;
            positions[i].y = y;
        }

        lineRenderer.SetPositions(positions);
        lineRenderer.positionCount = n;
    }

    private void calculateCannonGuideLines(int cannon) {
        int n = cannonGuideRenderer.positionCount;

        // v = (F/m)t
        Vector3 velocity = (cannons[cannon].transform.forward * cannonBallPrefab.force / cannonBallPrefab.GetComponent<Rigidbody>().mass);
        Vector3 cannonPos = cannonBallSpawnPoints[cannon].position + velocity.normalized * 0.3f;

        cannonGuideHitSprite.gameObject.SetActive(false);
        bool hasHit = false;

        for(int i = 0; i < n; i++) {
            cannonGuideRenderer.SetPosition(i, cannonPos);
            float time = Time.fixedDeltaTime * 4;
            velocity.y += Physics.gravity.y * time;
            cannonPos += velocity * time;

            if(!hasHit) { // Make sure not to raycast more times than is needed
                RaycastHit hitInfo;
                bool hit = Physics.Raycast(cannonPos, velocity, out hitInfo, velocity.magnitude * time + 1, LayerMask.GetMask("Terrain"));
                // Debug.DrawRay(cannonPos, velocity * Time.fixedDeltaTime * 4, Color.green);
                if(hit) {
                    cannonGuideHitSprite.gameObject.SetActive(true);
                    cannonGuideHitSprite.position = hitInfo.point + hitInfo.normal * 0.2f;
                    cannonGuideHitSprite.rotation = Quaternion.LookRotation(hitInfo.normal, Vector3.up);
                    hasHit = true;
                }
            }
        }

        cannonGuideRenderer.sharedMaterial.SetTextureOffset("_BaseMap", Vector2.right * Time.time * -1.4f);
        cannonGuideRenderer.sharedMaterial.SetTextureScale("_BaseMap", new Vector2(n / 4, 1));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Singleton<CameraController> {

    public new Camera camera;
    public Transform cameraContainer;
    public Ship shipFocus;
    public float cameraSensitivity;
    public float minRotX;
    public float cameraDistMin, cameraDistMax;
    public float zoomSpeed;
    public float cameraSmoothTime;
    public float fovChangeSpeed;

    [Header("Shake")]
    public float maxShakeAmount;
    public float shakeNoiseScale, shakeDampSpeed, shakeSmoothAmount;

    private float cameraDist;
    private float rotX, rotY;
    private float fovInit, fovOffset;

    private Vector2 shakeTarget;
    private float shakeAmount;

    protected override void Awake() {
        base.Awake();
        cameraDist = camera.transform.localPosition.z;
        fovInit = camera.fieldOfView;
    }

    void Update() {
        // Rotate the camera if cursor is locked
        if(Cursor.lockState == CursorLockMode.Locked) {
            Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            float deltaRotX = -mouseDelta.y * Time.deltaTime * cameraSensitivity;
            float deltaRotY = mouseDelta.x * Time.deltaTime * cameraSensitivity;
            if(rotX + deltaRotX >= minRotX && rotX + deltaRotX <= 90) {
                rotX += deltaRotX;
            }
            rotY += deltaRotY;
            float scrollDelta = -Input.mouseScrollDelta.y;
            zoom(scrollDelta * zoomSpeed * Time.deltaTime);
        }

        if(shipFocus) {
            transform.position = shipFocus.cameraPosition.transform.position;
        }

        shakeTarget = new Vector2(-0.5f + Mathf.PerlinNoise(Time.time * shakeNoiseScale, 0), -0.5f + Mathf.PerlinNoise(0, Time.time * shakeNoiseScale));
        shakeAmount = Mathf.MoveTowards(shakeAmount, 0, Time.deltaTime * shakeDampSpeed);
        Vector2 shakeOffset = shakeAmount * Vector2.Lerp(camera.transform.localPosition, shakeTarget, Time.deltaTime * shakeSmoothAmount);

        camera.transform.localPosition = new Vector3(shakeOffset.x, shakeOffset.y, Mathf.Lerp(camera.transform.localPosition.z, cameraDist, Time.deltaTime * 10));
        float currentRotX = cameraContainer.transform.localRotation.eulerAngles.x;
        float currentRotY = transform.localRotation.eulerAngles.y;
        float velocity = 0f;
        cameraContainer.transform.localRotation = Quaternion.Euler(Mathf.SmoothDampAngle(currentRotX, rotX, ref velocity, cameraSmoothTime), 0, 0);
        transform.localRotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(currentRotY, rotY, ref velocity, cameraSmoothTime), 0);

        camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, fovInit + fovOffset, Time.deltaTime * fovChangeSpeed);

        if(Input.GetMouseButtonDown(0)) {
            setCameraLocked(true);
        }
        if(Input.GetKeyDown(KeyCode.Escape)) {
            setCameraLocked(false);
        }
    }

    private void setCameraLocked(bool locked) {
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void zoom(float dist) {
        dist *= (cameraDist / 4);

        if(cameraDist + dist > cameraDistMin && cameraDist + dist < cameraDistMax) {
            cameraDist += dist;
        }
    }

    public void SetFovOffset(float offset) {
        fovOffset = offset;
    }

    public void Shake() {
        shakeAmount = maxShakeAmount;
    }
}
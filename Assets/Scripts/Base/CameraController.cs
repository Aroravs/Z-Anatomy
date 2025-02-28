using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    public static float MAX_DISTANCE = 10;
    public static float MIN_DISTANCE = 0.005f;

    public GameObject target;
    public GameObject defaultCenter;
    [HideInInspector]
    public Transform cameraCenter;

    [HideInInspector]
    public GameObject defaultTarget;
    [HideInInspector]
    public float distance = 10.0f;
    public float defaulDistance;
    [HideInInspector]
    public float xSpeed = 250.0f;
    [HideInInspector]
    public float ySpeed = 250.0f;
    [HideInInspector]
    public float yMinLimit = -90;
    [HideInInspector]
    public float yMaxLimit = 90;
    public float cameraZoomAnimationTime = 100;
    public float borderOffset;

    [Space]
    [Header("Movement values")]
    public float zoomVelocity;
    public float rotationVelocity;

    public AnimationCurve distanceCurve;
    public AnimationCurve positionCurve;
    public AnimationCurve rotationCurve;

    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    private EventSystem eventSys;

    [HideInInspector]
    public bool movingCenter;

    Camera cam;

    Vector2 lastMousePosition;
    private bool onRotateCoroutine;
    private IEnumerator lerpPosCoroutine;
    private IEnumerator lerpPosHorizontalCoroutine;

    bool lerping;

    [HideInInspector]
    public bool movementIsBlocked;

    [HideInInspector]
    public PhysicsRaycaster raycaster;

    private Bounds bounds;
    public Vector3 offset;
    public Vector3 rotationOffset;

    int layer_mask1;
    int layer_mask2;
    int layer_mask3;
    LayerMask finalmask;

    [HideInInspector]
    public Vector3 pivot;

    private Transform trans;

    private void Awake()
    {
        instance = this;
        raycaster = GetComponent<PhysicsRaycaster>();
        layer_mask1 = LayerMask.GetMask("Body");
        layer_mask2 = LayerMask.GetMask("Outline");
        layer_mask3 = LayerMask.GetMask("HighlightedOutline");
        finalmask = layer_mask1 | layer_mask2 | layer_mask3;

        cam = GetComponent<Camera>();
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        defaultTarget = target;
        cameraCenter = new GameObject().transform;
        cameraCenter.position = defaultCenter.transform.position;
        eventSys = EventSystem.current;
        distance = defaulDistance;
        lastMousePosition = cameraCenter.position;

        trans = transform;

    }


    private void Update()
    {
        if (ActionControl.blockedInput)
            return;

        //ZOOM
        if (Mouse.current.scroll.ReadValue().y != 0 && !eventSys.IsPointerOverGameObject() && ActionControl.canZoom)
        {
            distance -= Mouse.current.scroll.ReadValue().y * cam.orthographicSize * zoomVelocity * 0.001f;

            if(ActionControl.zoomToMouse)
            {
                Vector3 desiredPosition = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

                float dist = Vector3.Distance(desiredPosition, trans.position);
                Vector3 direction = Vector3.Normalize(desiredPosition - trans.position) * (dist * Mouse.current.scroll.ReadValue().y * zoomVelocity * 0.001f);

                if (RaycastObject.instance.bodyPartScript != null)
                    pivot = RaycastObject.instance.bodyPartScript.center;

                if (distance < MAX_DISTANCE)
                    trans.position += direction;
               
            }

            if (distance > MAX_DISTANCE) distance = MAX_DISTANCE;
            if (distance < MIN_DISTANCE) distance = MIN_DISTANCE;
        }

        //MOVE
        if ((Mouse.current.middleButton.isPressed || ActionControl.draggingMoveIcon) && !lerping && !movementIsBlocked )//&& IsPointInCameraView())
        {
            Vector3 A = cam.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0));
            Vector3 B = cam.ScreenToWorldPoint(new Vector3(lastMousePosition.x, lastMousePosition.y, 0));
            Vector3 mouseDirection = B - A;

            cameraCenter.Translate(mouseDirection, Space.World);
            trans.Translate(mouseDirection, Space.World);
        }

        lastMousePosition = Mouse.current.position.ReadValue();

        //ROTATE
        if ((Mouse.current.rightButton.isPressed || ActionControl.draggingGizmo || ActionControl.draggingRotateIcon) && !lerping)
        {
            var mouseDelta = Mouse.current.delta;

            x += mouseDelta.x.ReadValue() * xSpeed * rotationVelocity * 0.001f;

            if (z != 0)
            {
                if (y == 90)
                    x = -z;
                else
                    x = z;
                z = 0;
            }

            y -= mouseDelta.y.ReadValue() * ySpeed * rotationVelocity * 0.001f;

            if(ActionControl.limitRotation)
                y = ClampAngle(y, yMinLimit, yMaxLimit);

            if (mouseDelta.x.ReadValue() > 0 || mouseDelta.y.ReadValue() > 0)
                CubeBehaviour.instance.HasRotated();

            var rotation = Quaternion.Euler(y, x, z);
            var position = rotation * rotationOffset + pivot;

            trans.rotation = rotation;
            trans.position = position;
        }
    }

    private void LateUpdate()
    {
        cam.orthographicSize = distance;
        rotationOffset = trans.InverseTransformDirection(trans.position - pivot);
    }

    private bool IsPointInCameraView()
    {
        Vector3 viewportPointMaxY = cam.WorldToViewportPoint(trans.rotation * new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y - borderOffset * cam.orthographicSize, bounds.center.z));
        Vector3 viewportPointMinY = cam.WorldToViewportPoint(trans.rotation * new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y + borderOffset * cam.orthographicSize, bounds.center.z));
        Vector3 viewportPointMaxX = cam.WorldToViewportPoint(trans.rotation * new Vector3(bounds.center.x + bounds.extents.x - borderOffset * cam.orthographicSize, bounds.center.y, bounds.center.z));
        Vector3 viewportPointMinX = cam.WorldToViewportPoint(trans.rotation * new Vector3(bounds.center.x - bounds.extents.x + borderOffset * cam.orthographicSize, bounds.center.y, bounds.center.z));

        float deltaX = Mouse.current.delta.x.ReadValue();
        float deltaY = Mouse.current.delta.y.ReadValue();

        return (viewportPointMinY.y < 1 || deltaY < 0) && (viewportPointMaxY.y > 0 || deltaY > 0) 
            && (viewportPointMinX.x < 1 || deltaX < 0) && (viewportPointMaxX.x > 0 || deltaX > 0);
    }

    private void UpdatePosition()
    {
        if (ActionControl.limitRotation)
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        var rotation = Quaternion.Euler(y, x, z);
        trans.rotation = rotation;
        SetCameraPosition(rotation);
    }

    IEnumerator LerpCameraPos(float prevDistance, float newDistance)
    {
        lerping = true;
        var rot = Quaternion.Euler(y, x, 0);
        Vector3 po;
        Vector3 prevPosition = transform.position;
        float time = 0;
        while (time < cameraZoomAnimationTime)
        {
            float t = time / cameraZoomAnimationTime;
            //Smooth step
            // t = t * t * t * (t * (6f * t - 15f) + 10f);
            distance = Mathf.Lerp(prevDistance, newDistance, distanceCurve.Evaluate(t));
            po = rot * offset * distance + cameraCenter.position;
            trans.position = Vector3.Lerp(prevPosition, po, positionCurve.Evaluate(t));
            time += Time.deltaTime;
            yield return null;
        }
        distance = newDistance;
        lerping = false;
    }


    IEnumerator LerpCameraPosHorizontal(float animationTime)
    {
        lerping = true;
        var rot = Quaternion.Euler(y, x, 0);
        Vector3 po;
        Vector3 prevPosition = trans.position;
        float time = 0;
        while (time < animationTime)
        {
            float t = time / animationTime;
            //Smooth step
            // t = t * t * t * (t * (6f * t - 15f) + 10f);
            po = rot * offset * distance + cameraCenter.position;
            trans.position = Vector3.Lerp(prevPosition, new Vector3(po.x, transform.position.y, po.z), positionCurve.Evaluate(t));
            time += Time.deltaTime;
            yield return null;
        }
        lerping = false;
    }

    public void LerpHorizontal(float animationTime)
    {
        if (lerpPosHorizontalCoroutine != null)
            StopCoroutine(lerpPosHorizontalCoroutine);
        lerpPosHorizontalCoroutine = LerpCameraPosHorizontal(animationTime);
        StartCoroutine(lerpPosHorizontalCoroutine);
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, -90, 90);
    }

    public void UpdateCameraPos(float newDistance)
    {
        if(!movingCenter)
        {
            if (lerpPosCoroutine != null)
                StopCoroutine(lerpPosCoroutine);
            lerpPosCoroutine = LerpCameraPos(distance, newDistance);
            StartCoroutine(lerpPosCoroutine);
        }
    }

    public void ResetCamera()
    {
        SetTarget(null);
        cameraCenter.position = defaultCenter.transform.position;
        pivot = defaultCenter.transform.position;
        x = 0;
        y = 0;
        z = 0;
        distance = defaulDistance;
        cam.orthographicSize = distance;
        UpdatePosition();
    }

    public void SetCameraPosition(Quaternion rotation)
    {
        var position = rotation * offset * distance + cameraCenter.position;
        trans.position = position;
    }

    public void SetCameraRotation(Vector3 rot)
    {
        if(!onRotateCoroutine)
            StartCoroutine(LerpRotation(rot, 0.25f));
    }

    IEnumerator LerpRotation(Vector3 rot, float duration)
    {
        onRotateCoroutine = true;
        float time = 0;
        Quaternion startValue = trans.rotation;
        Quaternion endValue = Quaternion.Euler(rot.x, rot.y, 0);
        while (time < duration)
        {
            float t = time / duration;
            //Smooth step
            //t = t * t * t * (t * (6f * t - 15f) + 10f);
            trans.rotation = Quaternion.Lerp(startValue, endValue, rotationCurve.Evaluate(t));
            trans.position = trans.rotation * rotationOffset + pivot;
            time += Time.deltaTime;
            yield return null;
        }
        trans.rotation = endValue;
        x = rot.y;
        y = rot.x;
        z = 0;
        onRotateCoroutine = false;
    }

    //Center without interpolation
    public void CenterImmediate()
    {
        SelectedObjectsManagement.Instance.GetActiveObjects();

        float newDistance = bounds.extents.magnitude;
        if (newDistance > defaulDistance)
            newDistance = defaulDistance;
        distance = newDistance;

        if (ActionControl.limitRotation)
            y = ClampAngle(y, yMinLimit, yMaxLimit);

        var rotation = Quaternion.Euler(y, x, 0);
        trans.rotation = rotation;
        trans.position = rotation * offset * distance + cameraCenter.position;
    }

    public void CenterView(bool updateDistance)
    {
        SelectedObjectsManagement.Instance.GetActiveObjects();
        if (SelectedObjectsManagement.Instance.activeObjects.Count == 0)
            return;
        if (lerpPosCoroutine != null)
            StopCoroutine(lerpPosCoroutine);

        //No objets selected-> center is the medium point of all active objects
        if (SelectedObjectsManagement.Instance.selectedObjects.Count == 0)// && SelectedObjectsManagement.instance.deletedObjects.Count > 0)
        {
            if (bounds.extents.magnitude == 0)
                return;

            SetTarget(null);

            float newDistance = bounds.extents.magnitude * 1.25f;
            cameraCenter.position = bounds.center;
            pivot = bounds.center;

            if (newDistance > defaulDistance)
                newDistance = defaulDistance;
            if (updateDistance)
                UpdateCameraPos(newDistance);
            else
                UpdateCameraPos(distance);
        }
        else if (SelectedObjectsManagement.Instance.selectedObjects.Count == 1)
        {
            BodyPart script = SelectedObjectsManagement.Instance.selectedObjects[0].GetComponent<BodyPart>();
            Label labelScript = SelectedObjectsManagement.Instance.selectedObjects[0].GetComponent<Label>();

            if (script == null && labelScript == null)
                return;

            float newDistance;

            if (labelScript == null)
            {
                newDistance = script.distanceToCamera;
                if (newDistance == 0)
                    return;
                cameraCenter.position = script.center;
                pivot = script.center;
            }
            else
            {
                newDistance = labelScript.parent.distanceToCamera;
                if (newDistance == 0)
                    return;
                cameraCenter.position = labelScript.transform.position;
                pivot = labelScript.parent.center;
            }

            if (newDistance > defaulDistance)
                newDistance = defaulDistance;
            if (updateDistance)
                UpdateCameraPos(newDistance);
            else
                UpdateCameraPos(distance);
        }
        //Some objects selected -> the center is the medium point of all selected objects
        else
        {
            //Calculate bounds for all objects
            Bounds bounds = StaticMethods.GetBounds(SelectedObjectsManagement.Instance.selectedObjects);

            if (bounds.extents.magnitude == 0)
                return;

            float newDistance = bounds.extents.magnitude * 1.12f;
            cameraCenter.position = bounds.center;
            pivot = bounds.center;

            if (updateDistance)
                UpdateCameraPos(newDistance);
            else
                UpdateCameraPos(distance);
        }

        rotationOffset = new Vector3();
    }

    public void CenterRotation(Label label)
    {
        var rotation = Quaternion.LookRotation(label.lineDirection).eulerAngles;
        SetCameraRotation(rotation);
    }

    public void UpdateBounds()
    {
        bounds = StaticMethods.GetBounds(SelectedObjectsManagement.Instance.activeObjects);
    }

    public void SetTarget(GameObject t)
    {
        if (t != null)
            pivot = t.transform.position;
        target = t;
    }

    public bool CanRotate()
    {
        return !onRotateCoroutine;
    }

    public void SetZoomFromGizmo()
    {
        distance -= Mouse.current.delta.ReadValue().y * cam.orthographicSize * zoomVelocity * 0.001f;

        Vector3 desiredPosition = pivot;

        float dist = Vector3.Distance(desiredPosition, trans.position);
        Vector3 direction = Vector3.Normalize(desiredPosition - trans.position) * (dist * Mouse.current.delta.ReadValue().y * zoomVelocity * 0.001f);

        if (distance < MAX_DISTANCE)
            trans.position += direction;

        if (distance > MAX_DISTANCE) distance = MAX_DISTANCE;
        if (distance < MIN_DISTANCE) distance = MIN_DISTANCE;     
    }
}

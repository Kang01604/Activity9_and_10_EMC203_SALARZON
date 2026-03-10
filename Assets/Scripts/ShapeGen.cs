using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem; // Required for new Input System

public class ShapeGen : MonoBehaviour
{
    [Header("Rendering")]
    public Material material; // Unlit material used by GL to draw lines

    [Header("Pyramid Settings")]
    public Vector3 pyramidPosition = new Vector3(-4f, 0f, 5f); // World position of the pyramid
    public float pyramidSize = 1f;                              // Half-size of the pyramid (base and height)

    [Header("Sphere Settings")]
    public Vector3 spherePosition = new Vector3(0f, 0f, 5f);   // World position of the sphere
    public float sphereRadius = 1f;                             // Radius of the sphere
    [Range(6, 32)]
    public int sphereSegments = 12; // Higher = rounder sphere, lower = faster performance

    [Header("Capsule Settings")]
    public Vector3 capsulePosition = new Vector3(4f, 0f, 5f);  // World position of the capsule
    public float capsuleRadius = 0.6f;                          // Radius of the cylinder and hemisphere caps
    public float capsuleHeight = 2f;                            // Length of the cylinder shaft only (not including caps)
    [Range(6, 32)]
    public int capsuleSegments = 12; // Higher = smoother capsule

    [Header("Auto Rotation")]
    public float autoRotateSpeed = 40f; // Degrees per second — set to 0 to disable auto-spin

    [Header("Mouse Drag Rotation")]
    public float mouseSensitivity = 0.3f; // How fast shapes rotate per pixel dragged

    [Header("Screensaver Mode")]
    // Toggle in the Inspector or press Tab at runtime to switch modes
    public bool screensaverMode = false;
    // Independent spin speeds for screensaver — X rotates fully with no clamp
    public float screensaverSpeedY = 40f; // Degrees per second on Y axis
    public float screensaverSpeedX = 20f; // Degrees per second on X axis — full 360, no clamping

    // Current Y rotation in degrees — driven by auto-spin and horizontal mouse drag
    private float _rotationY = 0f;

    // Current X tilt in degrees — driven by vertical mouse drag only, clamped to +-80 in normal mode
    private float _rotationX = 0f;

    // Current Z roll in degrees — not driven by any input by default, set in code as needed
    private float _rotationZ = 0f;

    // Tracks whether the left mouse button is currently held down
    private bool _isDragging = false;

    // URP Render Hook
    // Subscribe/unsubscribe to URP's per-camera render event.
    // OnPostRender does NOT work in URP (using 2D Universal for this project)
    private void OnEnable()
    {
        // Start listening for URP camera render events
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        // Always unsubscribe to avoid memory leaks or ghost draw calls
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    // Called by URP after each camera finishes rendering
    private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        // Only draw for the main camera — ignore other cameras (e.g. reflection probes)
        if (cam != Camera.main) return;
        if (material == null) { Debug.LogError("[ShapeGen] No material assigned."); return; }

        // GL draw block — everything between PushMatrix/PopMatrix is one draw pass
        GL.PushMatrix();
        material.SetPass(0);  // Activate the first shader pass on the material
        GL.Begin(GL.LINES);   // Tell GL we are drawing lines (pairs of vertices)

        DrawPyramid(pyramidPosition, pyramidSize);
        DrawSphere(spherePosition, sphereRadius, sphereSegments);
        DrawCapsule(capsulePosition, capsuleRadius, capsuleHeight, capsuleSegments);

        GL.End();        // Finish the line batch
        GL.PopMatrix();  // Restore the previous matrix state
    }

    // Update: Rotation Logic
    private void Update()
    {
        var keyboard = Keyboard.current;

        // Tab toggles screensaver mode at runtime — also toggleable via Inspector checkbox
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            screensaverMode = !screensaverMode;

            // Reset X tilt when leaving screensaver so the clamp doesn't snap harshly
            if (!screensaverMode)
                _rotationX = Mathf.Clamp(_rotationX, -80f, 80f);
        }

        if (screensaverMode)
        {
            UpdateScreensaver();
        }
        else
        {
            UpdateNormal();
        }

        // Keep Y rotation within 0-360 range (shared by both modes)
        _rotationY %= 360f;
    }

    // Normal mode: Y auto-spins, mouse drag controls X (clamped) and Y
    private void UpdateNormal()
    {
        var mouse = Mouse.current;
        if (mouse == null) return; // No mouse connected, skip

        // Detect left mouse button press and release
        if (mouse.leftButton.wasPressedThisFrame)  _isDragging = true;
        if (mouse.leftButton.wasReleasedThisFrame) _isDragging = false;

        // Auto-spin on Y axis — pauses while the user is dragging so inputs don't fight
        if (!_isDragging)
            _rotationY += autoRotateSpeed * Time.deltaTime;

        if (_isDragging)
        {
            // mouse.delta is the pixel movement since the last frame (no manual tracking needed)
            Vector2 delta = mouse.delta.ReadValue();

            // Horizontal drag rotates around Y axis (left/right spin)
            _rotationY += delta.x * mouseSensitivity;

            // Vertical drag tilts around X axis (up/down tilt)
            _rotationX -= delta.y * mouseSensitivity;

            // Clamp X tilt so shapes never flip fully upside down
            _rotationX = Mathf.Clamp(_rotationX, -80f, 80f);
        }
    }

    // Screensaver mode: both X and Y spin freely and continuously — no mouse input, no X clamp
    private void UpdateScreensaver()
    {
        _isDragging = false; // Ensure drag state is clean when screensaver takes over

        _rotationY += screensaverSpeedY * Time.deltaTime;

        // X rotates fully — wraps around like Y instead of clamping
        _rotationX += screensaverSpeedX * Time.deltaTime;
        _rotationX %= 360f;
    }

    // Rotation Math
    // Standard 3D rotation matrix applied per-vertex before projection.
    // Each shape rotates around its own center point, not the world origin.
    // Rotates a point around a pivot on the Y axis (left/right spin)
    private Vector3 RotateY(Vector3 point, Vector3 pivot, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        Vector3 p = point - pivot; // Translate to origin before rotating
        return new Vector3(p.x * cos + p.z * sin, p.y, -p.x * sin + p.z * cos) + pivot;
    }

    // Rotates a point around a pivot on the X axis (up/down tilt)
    private Vector3 RotateX(Vector3 point, Vector3 pivot, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        Vector3 p = point - pivot; // Translate to origin before rotating
        return new Vector3(p.x, p.y * cos - p.z * sin, p.y * sin + p.z * cos) + pivot;
    }

    // Rotates a point around a pivot on the Z axis (clockwise/counter roll)
    private Vector3 RotateZ(Vector3 point, Vector3 pivot, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        Vector3 p = point - pivot; // Translate to origin before rotating
        return new Vector3(p.x * cos - p.y * sin, p.x * sin + p.y * cos, p.z) + pivot;
    }

    // Applies Y rotation first, then X tilt, then Z roll — order matters here
    private Vector3 Rotate(Vector3 point, Vector3 center)
    {
        point = RotateY(point, center, _rotationY);
        point = RotateX(point, center, _rotationX);
        point = RotateZ(point, center, _rotationZ);
        return point;
    }

    // Projection
    // Converts a 3D point to 2D screen space using the perspective camera.
    // Points further away (higher Z) are scaled down, simulating depth.
    private Vector2 Project(Vector3 point)
    {
        float scale = PerspectiveCamera.Instance.GetPerspective(point.z); // focalLength / (focalLength + z)
        return new Vector2(point.x * scale, point.y * scale);
    }

    // Rotates both endpoints around their shape center, projects them, then draws a GL line
    private void DrawLine3D(Vector3 a, Vector3 b, Vector3 center)
    {
        Vector2 pa = Project(Rotate(a, center));
        Vector2 pb = Project(Rotate(b, center));
        GL.Vertex3(pa.x, pa.y, 0f);
        GL.Vertex3(pb.x, pb.y, 0f);
    }

    // PYRAMID
    // 4 base corners on the XZ plane + 1 apex above center = 5 vertices
    // 4 base edges + 4 lateral edges to apex = 8 lines total
    private void DrawPyramid(Vector3 center, float size)
    {
        // Base corners (Y = -size = bottom)
        Vector3 fl = center + new Vector3(-size, -size,  size); // front-left
        Vector3 fr = center + new Vector3( size, -size,  size); // front-right
        Vector3 br = center + new Vector3( size, -size, -size); // back-right
        Vector3 bl = center + new Vector3(-size, -size, -size); // back-left
        Vector3 ap = center + new Vector3( 0f,    size,  0f  ); // apex (top)

        // Base square edges
        DrawLine3D(fl, fr, center); DrawLine3D(fr, br, center);
        DrawLine3D(br, bl, center); DrawLine3D(bl, fl, center);

        // Lateral edges connecting base corners to the apex
        DrawLine3D(fl, ap, center); DrawLine3D(fr, ap, center);
        DrawLine3D(br, ap, center); DrawLine3D(bl, ap, center);
    }

    // SPHERE
    // Built from latitude rings (horizontal) + longitude arcs (vertical).
    // Uses spherical coordinates: x = r*sin(phi)*cos(theta), y = r*cos(phi), z = r*sin(phi)*sin(theta)
    private void DrawSphere(Vector3 center, float radius, int segments)
    {
        // Latitude rings — horizontal circles stacked from top pole to bottom pole
        for (int lat = 1; lat < segments; lat++)
        {
            float phi = Mathf.PI * lat / segments; // Angle from top pole (0) to bottom pole (PI)
            float rr  = Mathf.Sin(phi) * radius;   // Ring radius shrinks near poles
            float ry  = Mathf.Cos(phi) * radius;   // Ring height on Y axis

            for (int lon = 0; lon < segments; lon++)
            {
                float t1 = 2f * Mathf.PI * lon       / segments;
                float t2 = 2f * Mathf.PI * (lon + 1) / segments;
                DrawLine3D(center + new Vector3(rr * Mathf.Cos(t1), ry, rr * Mathf.Sin(t1)),
                           center + new Vector3(rr * Mathf.Cos(t2), ry, rr * Mathf.Sin(t2)), center);
            }
        }

        // Longitude arcs — vertical arcs running from top pole to bottom pole
        for (int lon = 0; lon < segments; lon++)
        {
            float theta = 2f * Mathf.PI * lon / segments; // Angle around the Y axis

            for (int lat = 0; lat < segments; lat++)
            {
                float p1 = Mathf.PI * lat       / segments;
                float p2 = Mathf.PI * (lat + 1) / segments;
                DrawLine3D(center + new Vector3(Mathf.Sin(p1)*Mathf.Cos(theta)*radius, Mathf.Cos(p1)*radius, Mathf.Sin(p1)*Mathf.Sin(theta)*radius),
                           center + new Vector3(Mathf.Sin(p2)*Mathf.Cos(theta)*radius, Mathf.Cos(p2)*radius, Mathf.Sin(p2)*Mathf.Sin(theta)*radius), center);
            }
        }
    }

    // CAPSULE
    // Three parts: top hemisphere + cylinder shaft + bottom hemisphere.
    // capsuleHeight = shaft length only. Total height = height + 2 * radius.
    private void DrawCapsule(Vector3 center, float radius, float height, int segments)
    {
        float   halfH = height * 0.5f;
        Vector3 tc    = center + new Vector3(0f,  halfH, 0f); // Top of shaft
        Vector3 bc    = center + new Vector3(0f, -halfH, 0f); // Bottom of shaft

        // Cylinder: top ring, bottom ring, and vertical struts connecting them
        for (int i = 0; i < segments; i++)
        {
            float t1 = 2f * Mathf.PI * i       / segments;
            float t2 = 2f * Mathf.PI * (i + 1) / segments;

            Vector3 tA = tc + new Vector3(radius * Mathf.Cos(t1), 0f, radius * Mathf.Sin(t1));
            Vector3 tB = tc + new Vector3(radius * Mathf.Cos(t2), 0f, radius * Mathf.Sin(t2));
            Vector3 bA = bc + new Vector3(radius * Mathf.Cos(t1), 0f, radius * Mathf.Sin(t1));
            Vector3 bB = bc + new Vector3(radius * Mathf.Cos(t2), 0f, radius * Mathf.Sin(t2));

            DrawLine3D(tA, tB, center); // Top ring segment
            DrawLine3D(bA, bB, center); // Bottom ring segment
            DrawLine3D(tA, bA, center); // Vertical strut
        }

        // Caps: top hemisphere curves upward, bottom curves downward
        DrawHemisphere(tc, radius, segments, false, center); // false = opens downward (top cap)
        DrawHemisphere(bc, radius, segments, true,  center); // true  = opens upward  (bottom cap)
    }

    // Draws half a sphere — reused for both capsule caps
    // flipped = false → top cap (curves upward), flipped = true → bottom cap (curves downward)
    private void DrawHemisphere(Vector3 hc, float radius, int segments, bool flipped, Vector3 sc)
    {
        int   hs    = Mathf.Max(segments / 2, 1); // Half the segments — only need half the latitude range
        float ySign = flipped ? -1f : 1f;          // Flip Y direction for the bottom cap

        // Latitude rings on the hemisphere (from equator toward the pole)
        for (int lat = 1; lat < hs; lat++)
        {
            float phi = Mathf.PI * 0.5f * lat / hs; // 0 to PI/2 (equator to pole)
            float ry  = Mathf.Cos(phi) * radius * ySign;
            float rr  = Mathf.Sin(phi) * radius;

            for (int lon = 0; lon < segments; lon++)
            {
                float t1 = 2f * Mathf.PI * lon       / segments;
                float t2 = 2f * Mathf.PI * (lon + 1) / segments;
                DrawLine3D(hc + new Vector3(rr * Mathf.Cos(t1), ry, rr * Mathf.Sin(t1)),
                           hc + new Vector3(rr * Mathf.Cos(t2), ry, rr * Mathf.Sin(t2)), sc);
            }
        }

        // Longitude arcs on the hemisphere (vertical lines from equator to pole)
        for (int lon = 0; lon < segments; lon++)
        {
            float theta = 2f * Mathf.PI * lon / segments;

            for (int lat = 0; lat < hs; lat++)
            {
                float p1 = Mathf.PI * 0.5f * lat       / hs;
                float p2 = Mathf.PI * 0.5f * (lat + 1) / hs;
                DrawLine3D(hc + new Vector3(Mathf.Sin(p1)*Mathf.Cos(theta)*radius, Mathf.Cos(p1)*radius*ySign, Mathf.Sin(p1)*Mathf.Sin(theta)*radius),
                           hc + new Vector3(Mathf.Sin(p2)*Mathf.Cos(theta)*radius, Mathf.Cos(p2)*radius*ySign, Mathf.Sin(p2)*Mathf.Sin(theta)*radius), sc);
            }
        }
    }
}
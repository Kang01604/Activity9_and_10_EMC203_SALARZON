// used as the foundation logic for ShapeGen
// Foundation script — established the GL drawing pipeline and perspective projection approach that ShapeGen is built upon

using UnityEngine;
using UnityEngine.Rendering;

public class LineGen : MonoBehaviour
{
    public Material material;
    public float cubeSize;
    public Vector2 cubePos;

    public Vector2 cubePos2;
    public float zPos2;
    public float zPos;
    public float zRot = 0;

    // URP Render Hook

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == Camera.main)
            DrawLine();
    }

    // Drawing

    public void DrawLine()
    {
        if (material == null)
        {
            Debug.LogError("You need to add a material");
            return;
        }

        GL.PushMatrix();
        GL.Begin(GL.LINES);
        material.SetPass(0);

        var frontSquare = GetCube(cubePos);
        var frontZ = PerspectiveCamera.Instance.GetPerspective(zPos + cubeSize * .5f);
        var backSquare = GetCube(cubePos);
        var backZ = PerspectiveCamera.Instance.GetPerspective(zPos - cubeSize * .5f);

        var cubeDimensions1 = new CubeDimensions()
        {
            minX = -1 * cubeSize + cubePos.x,
            minY = -1 * cubeSize + cubePos.y,
            minZ = -1 * cubeSize + zPos,
            maxX =  1 * cubeSize + cubePos.x,
            maxY =  1 * cubeSize + cubePos.y,
            maxZ =  1 * cubeSize + zPos,
        };

        var frontSquare2 = GetCube(cubePos2);
        var frontZ2 = PerspectiveCamera.Instance.GetPerspective(zPos2 + cubeSize * .5f);
        var backSquare2 = GetCube(cubePos2);
        var backZ2 = PerspectiveCamera.Instance.GetPerspective(zPos2 - cubeSize * .5f);

        var cubeDimensions2 = new CubeDimensions()
        {
            minX = -1 * cubeSize + cubePos2.x,
            minY = -1 * cubeSize + cubePos2.y,
            minZ = -1 * cubeSize + zPos2,
            maxX =  1 * cubeSize + cubePos2.x,
            maxY =  1 * cubeSize + cubePos2.y,
            maxZ =  1 * cubeSize + zPos2,
        };

        var computedFront  = RenderSquare(frontSquare,  frontZ);
        var computedBack   = RenderSquare(backSquare,   backZ);
        var computedFront2 = RenderSquare(frontSquare2, frontZ2);
        var computedBack2  = RenderSquare(backSquare2,  backZ2);

        for (int i = 0; i < 4; i++)
        {
            GL.Vertex(computedFront[i]);
            GL.Vertex(computedBack[i]);
            GL.Vertex(computedFront2[i]);
            GL.Vertex(computedBack2[i]);
        }

        Debug.Log(cubeDimensions1.Collide(cubeDimensions2) ? "Hit" : "No Hit");

        GL.End();
        GL.PopMatrix();
    }

    public Vector2[] GetCube(Vector2 pos)
    {
        var faceArray = new Vector2[]
        {
            new Vector2( 1f,  1f),
            new Vector2(-1f,  1f),
            new Vector2(-1f, -1f),
            new Vector2( 1f, -1f),
        };

        for (var i = 0; i < faceArray.Length; i++)
            faceArray[i] = new Vector2(pos.x + faceArray[i].x, pos.y + faceArray[i].y) * cubeSize;

        return faceArray;
    }

    private Vector2[] RenderSquare(Vector2[] squareElements, float perspective)
    {
        var computedSquare = new Vector2[squareElements.Length];
        for (var i = 0; i < squareElements.Length; i++)
        {
            computedSquare[i] = squareElements[i] * perspective;
            GL.Vertex(squareElements[i] * perspective);
            GL.Vertex(squareElements[(i + 1) % squareElements.Length] * perspective);
        }
        return computedSquare;
    }

    private void RotationalMatrixComputation(ref Vector2[] squareElements)
    {
        var convertedRadians = zRot * Mathf.Deg2Rad;
        for (var i = 0; i < squareElements.Length; i++)
        {
            squareElements[i] = new Vector2(
                squareElements[i].x * Mathf.Cos(convertedRadians) - squareElements[i].y * Mathf.Sin(convertedRadians),
                squareElements[i].y * Mathf.Cos(convertedRadians) + squareElements[i].x * Mathf.Sin(convertedRadians)
            );
        }
    }

    // Structs

    public struct CubeDimensions
    {
        public float minX, minY, minZ;
        public float maxX, maxY, maxZ;

        public bool Collide(CubeDimensions otherCube)
        {
            bool overlapX = (minX <= otherCube.maxX && maxX >= otherCube.minX);
            bool overlapY = (minY <= otherCube.maxY && maxY >= otherCube.minY);
            bool overlapZ = (minZ <= otherCube.maxZ && maxZ >= otherCube.minZ);
            return overlapX && overlapY && overlapZ;
        }
    }

    public struct Circle
    {
        Vector3 Pos;
        float radius;

        public bool Collides(CubeDimensions cube)
        {
            // Clamp pos X w/ minX and maxX
            // Clamp pos Y w/ minY and maxY
            // Clamp pos Z w/ minZ and maxZ
            // Vector3 compare radius and the length of the new point
            return true;
        }
    }
}
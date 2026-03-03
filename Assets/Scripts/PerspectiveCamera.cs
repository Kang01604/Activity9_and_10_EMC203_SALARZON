using UnityEngine;

public class PerspectiveCamera : MonoBehaviour
{
    public static PerspectiveCamera Instance;

    public float focalLength = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(this);
    }

    public float GetPerspective(float zPos)
    {
        return focalLength / Mathf.Max((focalLength + zPos), 0);
    }


}

using UnityEngine;

namespace NavalCommand.Utils
{
    public class GridOverlay : MonoBehaviour
    {
        public bool ShowGrid = true;
        public int GridSize = 100;
        public float SmallStep = 5f;
        public float LargeStep = 25f;
        public Color SmallColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public Color LargeColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        
        private Material lineMaterial;

        // Automatically add this to the scene at runtime
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (FindObjectOfType<GridOverlay>() == null)
            {
                GameObject gridObj = new GameObject("DebugGrid_Auto");
                gridObj.AddComponent<GridOverlay>();
                DontDestroyOnLoad(gridObj);
            }
        }
        private void OnDrawGizmos()
        {
            if (!ShowGrid) return;
            // Draw Gizmos for Editor view
            DrawGridLines(transform.position);
        }

        // Simple GL drawing for Game View (requires script on Camera or OnPostRender)
        // Since we might not be on the camera, let's use a simple Mesh or LineRenderer approach? 
        // Actually, OnPostRender only works if attached to Camera. 
        // Let's stick to a simple GameObject that instantiates a large Plane with a grid shader? 
        // Or just use Debug.DrawLine for runtime visualization if Gizmos are on.
        
        // Better approach for "Game View" without assets: 
        // Create a large Quad and assign a material? No, we don't have a grid texture.
        // Let's use GL lines in OnRenderObject which works for any object.
        
        private void OnRenderObject()
        {
            if (!ShowGrid) return;
            
            CreateLineMaterial();
            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            
            DrawGLLines(transform.position);

            GL.End();
            GL.PopMatrix();
        }

        private void DrawGLLines(Vector3 center)
        {
            // Snap center to grid
            float startX = Mathf.Floor(center.x / SmallStep) * SmallStep - GridSize;
            float startZ = Mathf.Floor(center.z / SmallStep) * SmallStep - GridSize;
            float endX = startX + GridSize * 2;
            float endZ = startZ + GridSize * 2;

            for (float x = startX; x <= endX; x += SmallStep)
            {
                GL.Color((Mathf.Abs(x % LargeStep) < 0.1f) ? LargeColor : SmallColor);
                GL.Vertex3(x, 0, startZ);
                GL.Vertex3(x, 0, endZ);
            }

            for (float z = startZ; z <= endZ; z += SmallStep)
            {
                GL.Color((Mathf.Abs(z % LargeStep) < 0.1f) ? LargeColor : SmallColor);
                GL.Vertex3(startX, 0, z);
                GL.Vertex3(endX, 0, z);
            }
        }

        private void DrawGridLines(Vector3 center)
        {
            // Similar logic for Gizmos
            float startX = Mathf.Floor(center.x / SmallStep) * SmallStep - GridSize;
            float startZ = Mathf.Floor(center.z / SmallStep) * SmallStep - GridSize;
            float endX = startX + GridSize * 2;
            float endZ = startZ + GridSize * 2;

            for (float x = startX; x <= endX; x += SmallStep)
            {
                Gizmos.color = (Mathf.Abs(x % LargeStep) < 0.1f) ? LargeColor : SmallColor;
                Gizmos.DrawLine(new Vector3(x, 0, startZ), new Vector3(x, 0, endZ));
            }

            for (float z = startZ; z <= endZ; z += SmallStep)
            {
                Gizmos.color = (Mathf.Abs(z % LargeStep) < 0.1f) ? LargeColor : SmallColor;
                Gizmos.DrawLine(new Vector3(startX, 0, z), new Vector3(endX, 0, z));
            }
        }

        private void CreateLineMaterial()
        {
            if (!lineMaterial)
            {
                // Unity has a built-in shader that is good for drawing lines
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                lineMaterial.SetInt("_ZWrite", 0);
            }
        }
    }
}

using UnityEngine;

namespace NavalCommand.Environment
{
    public class WaterSetup : MonoBehaviour
    {
        [Header("Water Settings")]
        public int Width = 100;
        public int Height = 100;
        public float CellSize = 10f; // Size of each quad
        public Material WaterMaterial;

        // Automatically add this to the scene at runtime
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (FindObjectOfType<WaterSetup>() == null)
            {
                GameObject waterObj = new GameObject("WaterSystem_Auto");
                waterObj.AddComponent<WaterSetup>();
                // Don't DontDestroyOnLoad for environment usually, but for this test it's fine.
                // Actually, environment should probably stick around or be per-scene. 
                // Let's keep it simple.
            }
        }

        private void Start()
        {
            CreateWaterSurface();
        }

        [ContextMenu("Generate Water")]
        public void CreateWaterSurface()
        {
            // Check if water already exists
            Transform existingWater = transform.Find("WaterSurface");
            if (existingWater != null)
            {
                // If it exists, we can just return, or destroy and recreate if we want to update
                return; 
            }

            GameObject waterObj = new GameObject("WaterSurface");
            waterObj.transform.SetParent(transform);
            waterObj.transform.localPosition = Vector3.zero; // Center at (0,0,0)

            // Add Mesh Filter and Renderer
            MeshFilter mf = waterObj.AddComponent<MeshFilter>();
            MeshRenderer mr = waterObj.AddComponent<MeshRenderer>();

            // Create a large plane mesh
            mf.mesh = GeneratePlaneMesh();

            // Assign Material
            if (WaterMaterial == null)
            {
                // Try to find the shader we created
                Shader waterShader = Shader.Find("NavalCommand/ToonWater");
                if (waterShader != null)
                {
                    WaterMaterial = new Material(waterShader);
                    WaterMaterial.color = new Color(0f, 0.4f, 0.8f); // Deep Blue
                }
                else
                {
                    // Fallback to standard
                    WaterMaterial = new Material(Shader.Find("Standard"));
                    WaterMaterial.color = Color.blue;
                }
            }
            mr.material = WaterMaterial;
        }

        private Mesh GeneratePlaneMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "WaterMesh";

            // Simple large quad for now, or a grid if we want vertex waves to work well
            // For vertex waves to look good, we need vertices!
            
            // Increased resolution for better wave details
            int xSegments = 200;
            int zSegments = 200;
            float width = 500f;
            float depth = 500f;

            Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
            int[] triangles = new int[xSegments * zSegments * 6];
            Vector2[] uv = new Vector2[vertices.Length];

            for (int z = 0; z <= zSegments; z++)
            {
                for (int x = 0; x <= xSegments; x++)
                {
                    int index = z * (xSegments + 1) + x;
                    float xPos = (x / (float)xSegments) * width - (width / 2f);
                    float zPos = (z / (float)zSegments) * depth - (depth / 2f);
                    
                    vertices[index] = new Vector3(xPos, -2f, zPos); // Lower it slightly (-2) so ships float
                    uv[index] = new Vector2(x / (float)xSegments, z / (float)zSegments);
                }
            }

            int triIndex = 0;
            for (int z = 0; z < zSegments; z++)
            {
                for (int x = 0; x < xSegments; x++)
                {
                    int vertIndex = z * (xSegments + 1) + x;

                    triangles[triIndex + 0] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + xSegments + 1;
                    triangles[triIndex + 2] = vertIndex + 1;

                    triangles[triIndex + 3] = vertIndex + 1;
                    triangles[triIndex + 4] = vertIndex + xSegments + 1;
                    triangles[triIndex + 5] = vertIndex + xSegments + 2;

                    triIndex += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}

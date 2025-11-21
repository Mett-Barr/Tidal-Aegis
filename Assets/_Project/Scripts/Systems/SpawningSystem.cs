using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;

namespace NavalCommand.Systems
{
    public class SpawningSystem : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject EnemyPrefab; // Optional fallback
        public float SpawnRadius = 4000f; // Increased from 50
        public float SpawnInterval = 2f; // Decreased from 5

        private float spawnTimer;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SpawnInterval)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            // Calculate random position on circle
            Vector2 randomCircle = Random.insideUnitCircle.normalized * SpawnRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y);

            // Offset by player position if available
            if (GameManager.Instance.PlayerFlagship != null)
            {
                spawnPos += GameManager.Instance.PlayerFlagship.transform.position;
            }

            CreateModularEnemy(spawnPos);
        }

        private void CreateModularEnemy(Vector3 position)
        {
            // Create Enemy Container
            GameObject enemyObj = new GameObject("Enemy_Kamikaze");
            enemyObj.transform.position = position;
            enemyObj.layer = LayerMask.NameToLayer("Enemy"); // Ensure layer exists or default

            // Add Components
            Rigidbody rb = enemyObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            BoxCollider col = enemyObj.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 2f, 8f); // Approx Light Hull size
            col.center = new Vector3(0, 1f, 1.5f); // Centered to cover Body (-2.5 to 2.5) and Bow (2.5 to 5.5)

            NavalCommand.Entities.Units.KamikazeController kamikaze = enemyObj.AddComponent<NavalCommand.Entities.Units.KamikazeController>();
            kamikaze.UnitTeam = Team.Enemy;
            kamikaze.MaxHP = 30f; // Low HP for light ship

            // Generate Visuals (Light Hull)
            GenerateLightHullVisuals(enemyObj.transform);
        }

        private void GenerateLightHullVisuals(Transform parent)
        {
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(parent);
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localRotation = Quaternion.identity;

            // Light Hull Config
            float width = 3f;
            float height = 1.5f;
            float bodyLength = 5f;
            float bowLength = 3f;
            Color hullColor = new Color(0.6f, 0.2f, 0.2f); // Reddish for enemy

            // Mesh
            GameObject meshObj = new GameObject("HullMesh");
            meshObj.transform.SetParent(visuals.transform);
            meshObj.transform.localPosition = Vector3.zero;
            
            MeshFilter mf = meshObj.AddComponent<MeshFilter>();
            MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard"));
            mr.material.color = hullColor;
            
            // Reuse the pentagon generation logic (simplified)
            mf.mesh = GeneratePentagonalPrism(width, height, bodyLength, bowLength);

            // Add Bridge (Simple Cube)
            GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.transform.SetParent(visuals.transform);
            bridge.transform.localPosition = new Vector3(0, height + 0.5f, -1f); // Rear
            bridge.transform.localScale = new Vector3(2f, 1f, 2f);
            bridge.GetComponent<Renderer>().material.color = Color.black;
            Destroy(bridge.GetComponent<Collider>());
        }

        // Copied from ShipBuilder for consistency
        private Mesh GeneratePentagonalPrism(float width, float height, float bodyLength, float bowLength)
        {
            Mesh mesh = new Mesh();
            float halfWidth = width / 2f;
            
            // Bottom Face (y=0)
            Vector3 b_rl = new Vector3(-halfWidth, 0, -bodyLength/2); 
            Vector3 b_rr = new Vector3(halfWidth, 0, -bodyLength/2);  
            Vector3 b_fl = new Vector3(-halfWidth, 0, bodyLength/2); 
            Vector3 b_fr = new Vector3(halfWidth, 0, bodyLength/2);  
            Vector3 b_tip = new Vector3(0, 0, bodyLength/2 + bowLength); 

            // Top Face (y=height)
            Vector3 t_rl = new Vector3(-halfWidth, height, -bodyLength/2);
            Vector3 t_rr = new Vector3(halfWidth, height, -bodyLength/2);
            Vector3 t_fl = new Vector3(-halfWidth, height, bodyLength/2);
            Vector3 t_fr = new Vector3(halfWidth, height, bodyLength/2);
            Vector3 t_tip = new Vector3(0, height, bodyLength/2 + bowLength);

            System.Collections.Generic.List<Vector3> verts = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<int> tris = new System.Collections.Generic.List<int>();

            void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
            {
                int baseIndex = verts.Count;
                verts.Add(v1); verts.Add(v2); verts.Add(v3); verts.Add(v4);
                tris.Add(baseIndex); tris.Add(baseIndex + 1); tris.Add(baseIndex + 2);
                tris.Add(baseIndex); tris.Add(baseIndex + 2); tris.Add(baseIndex + 3);
            }
            
            void AddTri(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int baseIndex = verts.Count;
                verts.Add(v1); verts.Add(v2); verts.Add(v3);
                tris.Add(baseIndex); tris.Add(baseIndex + 1); tris.Add(baseIndex + 2);
            }

            AddQuad(b_rr, b_rl, t_rl, t_rr); // Rear
            AddQuad(b_rl, b_fl, t_fl, t_rl); // Left
            AddQuad(b_fr, b_rr, t_rr, t_fr); // Right
            AddQuad(b_fl, b_tip, t_tip, t_fl); // Bow Left
            AddQuad(b_tip, b_fr, t_fr, t_tip); // Bow Right
            
            // Top
            AddQuad(t_rl, t_fl, t_fr, t_rr);
            AddTri(t_fl, t_tip, t_fr);

            // Bottom
            AddQuad(b_rl, b_rr, b_fr, b_fl);
            AddTri(b_fl, b_fr, b_tip);

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}

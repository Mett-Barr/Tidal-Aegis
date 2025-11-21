using UnityEngine;
using NavalCommand.Entities.Components;
using NavalCommand.Entities.Units;
using NavalCommand.Data;

namespace NavalCommand.Utils
{
    public class ShipBuilder : MonoBehaviour
    {
        [Header("Ship Dimensions")]
        public Vector3 HullSize = new Vector3(4, 2, 12);
        public Vector3 BridgeSize = new Vector3(3, 2, 3);
        public Color ShipColor = Color.gray;

        [Header("Turret Configuration")]
        public int TurretCount = 2;
        public WeaponStatsSO DefaultWeaponStats;

        [Header("Actions")]
        public bool BuildTrigger = false; // Check this to build

        private void OnValidate()
        {
            if (BuildTrigger)
            {
                BuildTrigger = false;
                BuildShip();
            }
        }

        private void Reset()
        {
#if UNITY_EDITOR
            // Try to find TestCannon on Reset
            string[] guids = UnityEditor.AssetDatabase.FindAssets("TestCannon t:WeaponStatsSO");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                DefaultWeaponStats = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(path);
            }
#endif
        }

        [ContextMenu("Build Ship")]
        public void BuildShip()
        {
            // 1. Find or Create Visual Container
            Transform visualContainer = transform.Find("ShipVisuals");
            if (visualContainer != null)
            {
                DestroyImmediate(visualContainer.gameObject);
            }
            
            GameObject containerObj = new GameObject("ShipVisuals");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            containerObj.transform.localRotation = Quaternion.identity;
            visualContainer = containerObj.transform;

            // 2. Create Modular Hull (Flagship = Heavy)
            GameObject hullModule = CreateHullModule(WeightClass.Heavy);
            hullModule.transform.SetParent(visualContainer);
            hullModule.transform.localPosition = Vector3.zero;
            hullModule.transform.localRotation = Quaternion.identity;

            // 3. Find Mount Points and Attach Weapons
            // We look for children named "MountPoint_*"
            Transform[] allChildren = hullModule.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name.StartsWith("MountPoint_"))
                {
                    // Create Weapon Module
                    // Use DefaultWeaponStats type or default to FlagshipGun
                    if (DefaultWeaponStats == null)
                    {
#if UNITY_EDITOR
                        // Try to find TestCannon
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("TestCannon t:WeaponStatsSO");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            DefaultWeaponStats = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(path);
                            Debug.Log($"ShipBuilder: DefaultWeaponStats was null, auto-assigned {DefaultWeaponStats.name}");
                        }
                        else
                        {
                            Debug.LogError("ShipBuilder: Could not find 'TestCannon' asset! Please assign DefaultWeaponStats manually.");
                        }
#endif
                    }

                    WeaponType weaponType = (DefaultWeaponStats != null) ? DefaultWeaponStats.Type : WeaponType.FlagshipGun;
                    
                    GameObject weaponVisual = CreateWeaponModule(weaponType);
                    weaponVisual.transform.SetParent(child);
                    weaponVisual.transform.localPosition = Vector3.zero;
                    weaponVisual.transform.localRotation = Quaternion.identity;

                    // Setup Weapon Logic
                    // The weapon logic script usually goes on the turret base.
                    // Our CreateWeaponModule returns a container with visuals.
                    // We should add the controller to that container.
                    
                    // Setup FirePoint
                    // We need to find the "Barrel" or create a FirePoint.
                    // In CreateWeaponModule, we didn't explicitly name a "FirePoint" for all types, 
                    // but we can try to find a good spot or just use the weapon root + offset.
                    
                    Transform firePoint = weaponVisual.transform.Find("FirePoint");
                    if (firePoint == null)
                    {
                        firePoint = new GameObject("FirePoint").transform;
                        firePoint.SetParent(weaponVisual.transform);
                        firePoint.localPosition = new Vector3(0, 1.5f, 1.5f); // Generic forward offset
                    }

                    WeaponController wc = weaponVisual.AddComponent<WeaponController>();
                    wc.FirePoint = firePoint;
                    wc.WeaponStats = DefaultWeaponStats;
                    wc.OwnerTeam = Core.Team.Player;
                }
            }

            // 4. Add Components to Root if missing
            if (GetComponent<FlagshipController>() == null)
            {
                gameObject.AddComponent<FlagshipController>();
            }
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.drag = 1f;
                rb.angularDrag = 1f;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // Update Collider on Root
            // We need to approximate the size based on the Heavy Hull
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            // Heavy Hull is approx 5 wide, 2.5 high, ~20 long
            col.size = new Vector3(5f, 3f, 20f); 
            col.center = new Vector3(0, 1.5f, 0);
        }

        // Force Recompile Check 2
        private void OnGUI()
        {
            if (GUILayout.Button("Generate Modular Assets (Debug)"))
            {
                GenerateAllModularAssets();
            }
        }

        [ContextMenu("Generate All Modular Assets")]
        public void GenerateAllModularAssets()
        {
            // Create root container
            string rootName = "ModularAssets_Generated";
            GameObject existingRoot = GameObject.Find(rootName);
            if (existingRoot != null) DestroyImmediate(existingRoot);

            GameObject root = new GameObject(rootName);
            root.transform.position = Vector3.zero;

            // 1. Generate Weapon Modules
            float xSpacing = 5f;
            float currentX = 0f;
            
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
            {
                GameObject weapon = CreateWeaponModule(type);
                weapon.transform.SetParent(root.transform);
                weapon.transform.localPosition = new Vector3(currentX, 0, 0);
                currentX += xSpacing;
            }

            // 2. Generate Ship Hull Modules
            currentX = 0f;
            float zSpacing = 15f;
            
            foreach (WeightClass weight in System.Enum.GetValues(typeof(WeightClass)))
            {
                GameObject hull = CreateHullModule(weight);
                hull.transform.SetParent(root.transform);
                hull.transform.localPosition = new Vector3(currentX, 0, zSpacing);
                currentX += 15f; // More space for ships
            }
        }

        public GameObject CreateWeaponModule(WeaponType type)
        {
            GameObject container = new GameObject($"Weapon_{type}");
            
            // Naval Colors
            Color weaponColor = new Color(0.45f, 0.45f, 0.5f); // Darker Gunmetal
            
            switch (type)
            {
                case WeaponType.FlagshipGun:
                    // Standard Turret
                    GameObject baseObj = CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(1.5f, 0.5f, 1.5f), Vector3.zero);
                    baseObj.GetComponent<Renderer>().material.color = weaponColor;
                    
                    GameObject barrel = CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2.5f, 0.3f), new Vector3(0, 0.25f, 1.25f), new Vector3(90, 0, 0));
                    barrel.GetComponent<Renderer>().material.color = weaponColor;
                    
                    // Add FirePoint
                    GameObject fp = new GameObject("FirePoint");
                    fp.transform.SetParent(container.transform);
                    fp.transform.localPosition = new Vector3(0, 0.25f, 2.5f); // Tip of barrel
                    break;

                case WeaponType.Autocannon:
                    // Boxy Turret with dual barrels
                    GameObject box = CreatePrimitive(container, PrimitiveType.Cube, new Vector3(1f, 0.6f, 1f), Vector3.zero);
                    box.GetComponent<Renderer>().material.color = weaponColor;
                    
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.15f, 1.2f, 0.15f), new Vector3(-0.2f, 0, 0.6f), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = weaponColor;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.15f, 1.2f, 0.15f), new Vector3(0.2f, 0, 0.6f), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = weaponColor;
                    break;

                case WeaponType.CIWS:
                    // Phalanx style
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(0.8f, 1.2f, 0.8f), new Vector3(0, 0.6f, 0)).GetComponent<Renderer>().material.color = Color.white; // CIWS often white
                    CreatePrimitive(container, PrimitiveType.Sphere, new Vector3(0.7f, 0.7f, 0.7f), new Vector3(0, 1.2f, 0)).GetComponent<Renderer>().material.color = Color.white;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.2f, 1f, 0.2f), new Vector3(0, 1.0f, 0.6f), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = Color.black; // Barrel
                    break;

                case WeaponType.Missile:
                    // Box Launcher
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(1.2f, 0.4f, 1.2f), Vector3.zero).GetComponent<Renderer>().material.color = weaponColor;
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(1f, 0.8f, 1.5f), new Vector3(0, 0.5f, 0), new Vector3(-15, 0, 0)).GetComponent<Renderer>().material.color = weaponColor;
                    break;

                case WeaponType.Torpedo:
                    // Triple Tube
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(1f, 0.2f, 1f), Vector3.zero).GetComponent<Renderer>().material.color = weaponColor;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(-0.35f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = Color.black;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(0f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = Color.black;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(0.35f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().material.color = Color.black;
                    break;
            }

            return container;
        }

        public GameObject CreateHullModule(WeightClass weight)
        {
            GameObject container = new GameObject($"Hull_{weight}");
            
            // Configuration
            // Unified Gray
            Color hullColor = Color.gray;
            Color deckColor = new Color(0.3f, 0.3f, 0.3f);    // Dark Deck Gray
            
            int mountCount = 0;

            switch (weight)
            {
                case WeightClass.Light:
                    mountCount = 1;
                    break;
                case WeightClass.Medium:
                    mountCount = 2;
                    break;
                case WeightClass.Heavy:
                    mountCount = 3;
                    break;
            }

            // Dimensions
            float mountDiameter = 2.0f;
            float padding = 0.5f;
            float bridgeLength = 3.0f; // Bridge size
            float hullWidth = mountDiameter + (padding * 2);
            float hullHeight = 1.5f;

            // Layout Strategy:
            // We build from Rear (Z=0) to Front (Z+)
            // Light (1):  [Rear Pad] [Bridge] [Pad] [Mount] [Pad] [Bow]
            // Medium (2): [Rear Pad] [Mount] [Pad] [Bridge] [Pad] [Mount] [Pad] [Bow]
            // Heavy (3):  [Rear Pad] [Mount] [Pad] [Bridge] [Pad] [Mount] [Pad] [Mount] [Pad] [Bow]
            
            System.Collections.Generic.List<int> slots = new System.Collections.Generic.List<int>();
            
            if (weight == WeightClass.Light)
            {
                slots.Add(1); // Bridge Rear
                slots.Add(0); // Mount Front
            }
            else if (weight == WeightClass.Medium)
            {
                slots.Add(0); // Mount Rear
                slots.Add(1); // Bridge Mid
                slots.Add(0); // Mount Front
            }
            else if (weight == WeightClass.Heavy)
            {
                slots.Add(0); // Mount Rear
                slots.Add(1); // Bridge Mid
                slots.Add(0); // Mount Front
                slots.Add(0); // Mount Front
            }

            // Calculate Total Body Length
            float currentZ = padding; // Start with rear padding
            float bodyLength = padding; // Initial padding

            foreach (int slot in slots)
            {
                if (slot == 0) bodyLength += mountDiameter + padding;
                else if (slot == 1) bodyLength += bridgeLength + padding;
            }
            
            float bowLength = hullWidth; // Bow length
            float totalLength = bodyLength + bowLength;

            // 1. Generate Hull Mesh
            GameObject hullMeshObj = new GameObject("MainHull_Mesh");
            hullMeshObj.transform.SetParent(container.transform);
            hullMeshObj.transform.localPosition = Vector3.zero;
            
            MeshFilter mf = hullMeshObj.AddComponent<MeshFilter>();
            MeshRenderer mr = hullMeshObj.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard"));
            mr.material.color = hullColor;

            mf.mesh = GeneratePentagonalPrism(hullWidth, hullHeight, bodyLength, bowLength);

            // 2. Place Components
            currentZ = padding; // Reset to start placing
            int mountIndex = 1;

            foreach (int slot in slots)
            {
                if (slot == 0) // Mount
                {
                    float centerZ = currentZ + (mountDiameter / 2f);
                    
                    GameObject mount = CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(mountDiameter, 0.1f, mountDiameter), new Vector3(0, hullHeight, centerZ));
                    mount.name = $"MountPoint_{mountIndex}";
                    mount.GetComponent<Renderer>().material.color = deckColor;
                    
                    currentZ += mountDiameter + padding;
                    mountIndex++;
                }
                else if (slot == 1) // Bridge
                {
                    float centerZ = currentZ + (bridgeLength / 2f);
                    
                    GameObject bridge = CreatePrimitive(container, PrimitiveType.Cube, new Vector3(hullWidth * 0.8f, 1.2f, bridgeLength), new Vector3(0, hullHeight + 0.6f, centerZ));
                    bridge.name = "Bridge";
                    bridge.GetComponent<Renderer>().material.color = hullColor; // Unified Color
                    
                    // Bridge Windows/Detail (Optional)
                    GameObject window = CreatePrimitive(bridge, PrimitiveType.Cube, new Vector3(0.8f, 0.3f, 0.1f), new Vector3(0, 0.2f, 0.5f)); // Front window
                    window.GetComponent<Renderer>().material.color = Color.black; // Dark windows
                    
                    currentZ += bridgeLength + padding;
                }
            }

            return container;
        }

        private Mesh GeneratePentagonalPrism(float width, float height, float bodyLength, float bowLength)
        {
            Mesh mesh = new Mesh();
            mesh.name = "PentagonalHull";

            float halfWidth = width / 2f;
            
            // Vertices
            // We need a "House" shape in Plan View (Top-down).
            // So the footprint is a rectangle + triangle.
            // Cross-section is a Rectangle.
            
            // Bottom Face (y=0)
            Vector3 b_rl = new Vector3(-halfWidth, 0, 0); // Rear Left
            Vector3 b_rr = new Vector3(halfWidth, 0, 0);  // Rear Right
            Vector3 b_fl = new Vector3(-halfWidth, 0, bodyLength); // Front Left (start of bow)
            Vector3 b_fr = new Vector3(halfWidth, 0, bodyLength);  // Front Right (start of bow)
            Vector3 b_tip = new Vector3(0, 0, bodyLength + bowLength); // Bow Tip

            // Top Face (y=height)
            Vector3 t_rl = new Vector3(-halfWidth, height, 0);
            Vector3 t_rr = new Vector3(halfWidth, height, 0);
            Vector3 t_fl = new Vector3(-halfWidth, height, bodyLength);
            Vector3 t_fr = new Vector3(halfWidth, height, bodyLength);
            Vector3 t_tip = new Vector3(0, height, bodyLength + bowLength);

            // We'll build separate faces for flat shading look (hard edges)
            // 1. Rear Face (Quad)
            // 2. Left Side (Quad)
            // 3. Right Side (Quad)
            // 4. Top Deck (Pentagon -> 3 tris)
            // 5. Bottom (Pentagon -> 3 tris)
            // 6. Bow Left (Quad)
            // 7. Bow Right (Quad)
            
            // Actually, let's just list vertices and build tris.
            // For flat shading, we need unique vertices for each face.
            
            System.Collections.Generic.List<Vector3> verts = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<int> tris = new System.Collections.Generic.List<int>();

            // Helper to add quad
            void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
            {
                int baseIndex = verts.Count;
                verts.Add(v1); verts.Add(v2); verts.Add(v3); verts.Add(v4);
                tris.Add(baseIndex); tris.Add(baseIndex + 1); tris.Add(baseIndex + 2);
                tris.Add(baseIndex); tris.Add(baseIndex + 2); tris.Add(baseIndex + 3);
            }
            
            // Helper to add tri
            void AddTri(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int baseIndex = verts.Count;
                verts.Add(v1); verts.Add(v2); verts.Add(v3);
                tris.Add(baseIndex); tris.Add(baseIndex + 1); tris.Add(baseIndex + 2);
            }

            // Rear Face
            AddQuad(b_rr, b_rl, t_rl, t_rr);

            // Left Side Body
            AddQuad(b_rl, b_fl, t_fl, t_rl);

            // Right Side Body
            AddQuad(b_fr, b_rr, t_rr, t_fr);

            // Bow Left
            AddQuad(b_fl, b_tip, t_tip, t_fl);

            // Bow Right
            AddQuad(b_tip, b_fr, t_fr, t_tip);

            // Top Deck (Pentagon split into 3 tris: Quad body + Tri bow)
            AddQuad(t_rl, t_fl, t_fr, t_rr); // Body
            AddTri(t_fl, t_tip, t_fr);       // Bow

            // Bottom (Pentagon) - Winding reversed
            AddQuad(b_rl, b_rr, b_fr, b_fl); // Body
            AddTri(b_fl, b_fr, b_tip);       // Bow

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            
            return mesh;
        }

        private GameObject CreatePrimitive(GameObject parent, PrimitiveType type, Vector3 scale, Vector3 localPos, Vector3 localRot = default)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.transform.SetParent(parent.transform);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(localRot);
            obj.transform.localScale = scale;
            
            if (parent.name.StartsWith("Weapon") || parent.name.StartsWith("Hull"))
            {
                DestroyImmediate(obj.GetComponent<Collider>());
            }

            return obj;
        }
    }
}

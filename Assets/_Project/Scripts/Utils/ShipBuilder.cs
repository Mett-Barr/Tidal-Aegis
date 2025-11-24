using UnityEngine;
using NavalCommand.Entities.Components;
using NavalCommand.Entities.Units;
using NavalCommand.Data;

namespace NavalCommand.Utils
{
    public class ShipBuilder : MonoBehaviour
    {
        [Header("Ship Dimensions")]
        public WeightClass ShipClass = WeightClass.SuperHeavy; // Default to Super Flagship
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

            // 2. Create Modular Hull
            Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (defaultMat == null) defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = Color.gray;

            GameObject hullModule = CreateHullModule(ShipClass, defaultMat);
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
                    
                    GameObject weaponVisual = CreateWeaponModule(weaponType, defaultMat);
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
            // Update Collider on Root
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            // Dynamic Collider Size based on Hull Dimensions
            // Note: W, H_hull, H_super, L are local to CreateHullModule.
            // For now, using hardcoded values or class members if available.
            // To make this truly dynamic, CreateHullModule would need to return these dimensions.
            col.size = new Vector3(5f, 3f, 20f); // Placeholder, ideally derived from hullModule
            col.center = new Vector3(0, 1.5f, 0); // Placeholder
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
            Material debugMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); 
            if (debugMat == null) debugMat = new Material(Shader.Find("Standard"));
            debugMat.color = Color.gray;
            
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
            {
                GameObject weapon = CreateWeaponModule(type, debugMat);
                weapon.transform.SetParent(root.transform);
                weapon.transform.localPosition = new Vector3(currentX, 0, 0);
                currentX += xSpacing;
            }

            // 2. Generate Ship Hull Modules
            currentX = 0f;
            float zSpacing = 15f;
            
            foreach (WeightClass weight in System.Enum.GetValues(typeof(WeightClass)))
            {
                GameObject hull = CreateHullModule(weight, debugMat);
                hull.transform.SetParent(root.transform);
                hull.transform.localPosition = new Vector3(currentX, 0, zSpacing);
                currentX += 15f; // More space for ships
            }
        }

        public GameObject CreateWeaponModule(WeaponType type, Material sharedMat)
        {
            GameObject container = new GameObject($"Weapon_{type}");
            
            // Use the shared material for everything to ensure consistency
            // If we want different colors, we should use different materials, but user requested unification.
            // However, barrels usually are darker. 
            // User said: "Can we unify using the hull's material?"
            // So we will use sharedMat for the main body.
            
            switch (type)
            {
                case WeaponType.FlagshipGun:
                    // Standard Turret
                    GameObject baseObj = CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(1.5f, 0.5f, 1.5f), Vector3.zero);
                    baseObj.GetComponent<Renderer>().sharedMaterial = sharedMat;
                    
                    GameObject barrel = CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.15f, 1.2f, 0.15f), new Vector3(0.2f, 0, 0.6f), new Vector3(90, 0, 0));
                    barrel.GetComponent<Renderer>().sharedMaterial = sharedMat; 
                    // If we want black barrels, we'd need a separate material. 
                    // But "Unify using hull's material" implies single material usage or at least consistent base.
                    // Let's stick to sharedMat for now to be safe.
                    break;

                case WeaponType.CIWS:
                    // Phalanx style
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(0.8f, 1.2f, 0.8f), new Vector3(0, 0.6f, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Sphere, new Vector3(0.7f, 0.7f, 0.7f), new Vector3(0, 1.2f, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.2f, 1f, 0.2f), new Vector3(0, 1.0f, 0.6f), new Vector3(90, 0, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    break;

                case WeaponType.Autocannon:
                    // Single Barrel Quick Firing
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(0.6f, 0.6f, 0.6f), new Vector3(0, 0.3f, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.1f, 1.0f, 0.1f), new Vector3(0, 0.5f, 0.5f), new Vector3(90, 0, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    break;

                case WeaponType.Missile:
                    // VLS (Vertical Launch System)
                    // 1. Base (Flat Block)
                    CreatePrimitive(container, PrimitiveType.Cube, new Vector3(2.0f, 0.5f, 3.0f), Vector3.zero).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    
                    // 2. Cells (Visual Detail)
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            CreatePrimitive(container, PrimitiveType.Cube, new Vector3(0.4f, 0.1f, 0.4f), new Vector3(x * 0.6f, 0.3f, z * 0.6f)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                        }
                    }

                    // 3. FirePoint
                    GameObject fp = new GameObject("FirePoint");
                    fp.transform.SetParent(container.transform);
                    fp.transform.localPosition = new Vector3(0, 1.0f, 0); 
                    fp.transform.localRotation = Quaternion.Euler(-90, 0, 0); 
                    break;

                case WeaponType.Torpedo:
                    // Triple Tube
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(1f, 0.2f, 1f), Vector3.zero).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(-0.35f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(0f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    CreatePrimitive(container, PrimitiveType.Cylinder, new Vector3(0.3f, 2f, 0.3f), new Vector3(0.35f, 0.3f, 0), new Vector3(90, 0, 0)).GetComponent<Renderer>().sharedMaterial = sharedMat;
                    break;
            }

            return container;
        }

        private Material GetOrSaveMaterial(string name, Color color)
        {
#if UNITY_EDITOR
            string matFolder = "Assets/_Project/Generated/Materials";
            if (!System.IO.Directory.Exists(matFolder)) System.IO.Directory.CreateDirectory(matFolder);
            
            string matPath = $"{matFolder}/{name}.mat";
            Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            if (mat == null)
            {
                mat = new Material(GetShader());
                mat.color = color;
                // URP Support
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                
                UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                // Update color just in case
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                UnityEditor.EditorUtility.SetDirty(mat);
            }
            return mat;
#else
            Material mat = new Material(GetShader());
            mat.color = color;
             if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
#endif
        }

        public GameObject CreateHullModule(WeightClass weight, Material hullMat)
        {
            GameObject container = new GameObject($"Hull_{weight}");
            
            // Configuration
            Color deckColor = new Color(0.3f, 0.3f, 0.3f);
            
            // Dimensions based on Spec
            // W (Beam)
            float W = 4.0f; 
            // H_hull (Depth below deck)
            float H_hull = 1.5f;
            // H_super (Superstructure height reference)
            float H_super = 2.0f;

            float L = 0f;
            
            // Layout Definitions (z_norm: 0=Bow, 1=Stern)
            // We now store Vector3 for mount positions to allow side mounts
            System.Collections.Generic.List<Vector3> mountNormPositions = new System.Collections.Generic.List<Vector3>(); 
            float islandNormPosition = 0.5f;
            
            switch (weight)
            {
                case WeightClass.Light: // S Class
                    // L:W ~ 3.75
                    L = W * 3.75f;
                    mountNormPositions.Add(new Vector3(0, 0, 0.25f));
                    islandNormPosition = 0.60f;
                    break;
                    
                case WeightClass.Medium: // M Class
                    // L:W ~ 4.75
                    L = W * 4.75f;
                    mountNormPositions.Add(new Vector3(0, 0, 0.22f));
                    islandNormPosition = 0.50f;
                    mountNormPositions.Add(new Vector3(0, 0, 0.80f));
                    break;
                    
                case WeightClass.Heavy: // L Class
                    // L:W ~ 6.0
                    L = W * 6.0f;
                    mountNormPositions.Add(new Vector3(0, 0, 0.18f));
                    mountNormPositions.Add(new Vector3(0, 0, 0.40f));
                    mountNormPositions.Add(new Vector3(0, 0, 0.88f));
                    islandNormPosition = 0.63f;
                    break;

                case WeightClass.SuperHeavy: // XL Class (Super Flagship)
                    // Huge dimensions: L ~ 80m, W ~ 10m
                    W = 10f;
                    L = 80f;
                    H_hull = 4f;
                    H_super = 5f;

                    // 3 Centerline Mounts
                    mountNormPositions.Add(new Vector3(0, 0, 0.1f)); // Bow
                    mountNormPositions.Add(new Vector3(0, 0, 0.25f)); // Mid-Bow
                    mountNormPositions.Add(new Vector3(0, 0, 0.9f)); // Stern

                    // 10 Side Mounts (5 Port, 5 Starboard)
                    // Spaced along the mid-section
                    // float sideX = 0.35f; // Unused
                    // Actually, let's store normalized X where 1.0 is edge. 
                    // But our logic below uses absolute X? 
                    // Let's stick to the pattern: Vector3(x_norm, y_unused, z_norm). 
                    // x_norm: -0.5 to 0.5 (relative to W)
                    
                    float[] sideZ = new float[] { 0.35f, 0.45f, 0.55f, 0.65f, 0.75f };
                    
                    foreach (float z in sideZ)
                    {
                        mountNormPositions.Add(new Vector3(-0.4f, 0, z)); // Port
                        mountNormPositions.Add(new Vector3(0.4f, 0, z));  // Starboard
                    }

                    // Extra CIWS Mounts (Front and Back Coverage)
                    // Front Side
                    mountNormPositions.Add(new Vector3(-0.4f, 0, 0.15f)); 
                    mountNormPositions.Add(new Vector3(0.4f, 0, 0.15f));
                    // Back Side
                    mountNormPositions.Add(new Vector3(-0.4f, 0, 0.85f));
                    mountNormPositions.Add(new Vector3(0.4f, 0, 0.85f));

                    islandNormPosition = 0.5f;
                    break;


            }

            // Coordinate Conversion
            // z_norm = 0 => Z = +L/2 (Bow)
            // z_norm = 1 => Z = -L/2 (Stern)
            // Z = (L/2) - (z_norm * L)
            float GetZ(float z_norm) => (L / 2f) - (z_norm * L);
            
            // x_norm = 0 => X = 0
            // X = x_norm * W
            float GetX(float x_norm) => x_norm * W;

            // 1. Generate Hull Mesh
            // Hull goes from Y = -H_hull to Y = 0 (Main Deck)
            GameObject hullMeshObj = new GameObject("MainHull_Mesh");
            hullMeshObj.transform.SetParent(container.transform);
            hullMeshObj.transform.localPosition = Vector3.zero;
            
            MeshFilter mf = hullMeshObj.AddComponent<MeshFilter>();
            MeshRenderer mr = hullMeshObj.AddComponent<MeshRenderer>();

            mr.sharedMaterial = hullMat;

            // CRITICAL: Use sharedMesh for Asset assignment
            mf.sharedMesh = GenerateHexagonalHull(W, H_hull, L);

            // 2. Place Components
            // All components sit on Main Deck (Y=0)
            
            float mountDiameter = W * 0.65f; // Spec: 0.6 ~ 0.7 * B
            if (weight == WeightClass.SuperHeavy) mountDiameter = W * 0.4f; // Smaller relative mounts for SuperHeavy

            float islandWidth = W * 0.8f;    // Spec: 0.8 * B
            float islandLength = mountDiameter * 1.0f; // Spec: 0.8 ~ 1.2 * MountDiameter
            if (weight == WeightClass.SuperHeavy) islandLength = L * 0.2f; // Longer island for SuperHeavy

            // Place Mounts
            int mountIndex = 1;
            foreach (Vector3 normPos in mountNormPositions)
            {
                float z = GetZ(normPos.z);
                float x = GetX(normPos.x);
                
                // Visual Representation of Mount Base
                // Base sits at Y=0. Height is small (e.g., 0.1 * H_super ~ 0.2m)
                float baseHeight = 0.2f;
                
                // Create Base Container (The "Hardpoint") - Scale (1,1,1)
                GameObject mountBase = new GameObject($"MountPoint_{mountIndex}");
                mountBase.transform.SetParent(container.transform);
                mountBase.transform.localPosition = new Vector3(x, baseHeight / 2f, z);
                mountBase.transform.localRotation = Quaternion.identity;
                mountBase.transform.localScale = Vector3.one;

                // Create Visual Cylinder as Child
                GameObject mountVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mountVisual.transform.SetParent(mountBase.transform);
                mountVisual.transform.localPosition = Vector3.zero;
                mountVisual.transform.localRotation = Quaternion.identity;
                mountVisual.transform.localScale = new Vector3(mountDiameter, baseHeight, mountDiameter);
                mountVisual.GetComponent<Renderer>().sharedMaterial.color = deckColor;
                DestroyImmediate(mountVisual.GetComponent<Collider>());

                // Rotate Side Mounts
                if (normPos.x < -0.1f) // Port
                {
                    mountBase.transform.localRotation = Quaternion.Euler(0, -90, 0);
                }
                else if (normPos.x > 0.1f) // Starboard
                {
                    mountBase.transform.localRotation = Quaternion.Euler(0, 90, 0);
                }
                else if (normPos.z > 0.8f) // Stern (Rear)
                {
                    mountBase.transform.localRotation = Quaternion.Euler(0, 180, 0);
                }
                
                mountIndex++;
            }

            // Place Island
            float islandZ = GetZ(islandNormPosition);
            // Island Base at Y=0. Top at H_super.
            // Center Y = H_super / 2.
            GameObject island = CreatePrimitive(container, PrimitiveType.Cube, new Vector3(islandWidth, H_super, islandLength), new Vector3(0, H_super / 2f, islandZ));
            island.name = "Island";
            island.GetComponent<Renderer>().sharedMaterial = hullMat;
            
            // Island Detail (Bridge Window / Mast)
            // Mast top at H_super (already there). 
            // Let's add a small mast extending slightly higher to mark it as highest point.
            GameObject mast = CreatePrimitive(island, PrimitiveType.Cylinder, new Vector3(0.5f, 1.0f, 0.5f), new Vector3(0, 0.5f + 0.2f, 0)); // Relative to island center
            mast.transform.localPosition = new Vector3(0, 0.5f + 0.2f, 0); // On top of island block
            mast.GetComponent<Renderer>().sharedMaterial = hullMat;

            return container;
        }

        private Mesh GenerateHexagonalHull(float width, float depth, float length)
        {
            Mesh mesh = new Mesh();
            mesh.name = "HexHull";

            float halfW = width / 2f;
            float halfL = length / 2f;
            
            // Y range: -depth to 0
            float yBottom = -depth;
            float yTop = 0f;

            // Chamfer length for Bow and Stern
            float chamfer = width * 0.8f; 
            
            // Profile (Top-down)
            Vector3[] profile = new Vector3[]
            {
                new Vector3(0, 0, halfL),                     // Front Tip
                new Vector3(halfW, 0, halfL - chamfer),       // Front Right
                new Vector3(halfW, 0, -(halfL - chamfer)),    // Rear Right
                new Vector3(0, 0, -halfL),                    // Rear Tip
                new Vector3(-halfW, 0, -(halfL - chamfer)),   // Rear Left
                new Vector3(-halfW, 0, halfL - chamfer)       // Front Left
            };

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

            // Top Deck (Y = 0)
            Vector3 topCenter = new Vector3(0, yTop, 0);
            for (int i = 0; i < 6; i++)
            {
                Vector3 p1 = profile[i]; p1.y = yTop;
                Vector3 p2 = profile[(i + 1) % 6]; p2.y = yTop;
                AddTri(topCenter, p1, p2);
            }

            // Bottom Hull (Y = -depth)
            // User requested a Long Hexagonal Prism (No Taper)
            Vector3 bottomCenter = new Vector3(0, yBottom, 0);
            
            for (int i = 0; i < 6; i++)
            {
                Vector3 p1 = profile[i]; p1.y = yBottom;
                Vector3 p2 = profile[(i + 1) % 6]; p2.y = yBottom;
                AddTri(bottomCenter, p2, p1); // Reverse winding for bottom face (Normal points down)
            }

            // Sides
            for (int i = 0; i < 6; i++)
            {
                Vector3 b1 = profile[i]; b1.y = yBottom;
                Vector3 b2 = profile[(i + 1) % 6]; b2.y = yBottom;
                Vector3 t1 = profile[i]; t1.y = yTop;
                Vector3 t2 = profile[(i + 1) % 6]; t2.y = yTop;
                
                // Winding: Bottom-Current -> Top-Current -> Top-Next -> Bottom-Next
                // This creates an outward facing normal
                AddQuad(b1, t1, t2, b2);
            }


            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            
#if UNITY_EDITOR
            // Save Mesh as Asset to persist in Prefabs
            string folderPath = "Assets/_Project/Generated/Meshes";
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string meshName = $"HullMesh_{System.DateTime.Now.Ticks}";
            string assetPath = $"{folderPath}/{meshName}.asset";
            
            UnityEditor.AssetDatabase.CreateAsset(mesh, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh(); // FORCE REFRESH
            
            Debug.Log($"[ShipBuilder] Saved Hull Mesh to {assetPath}");
            
            // CRITICAL: Return the ASSET, not the runtime mesh
            Mesh loadedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (loadedMesh == null)
            {
                Debug.LogError("[ShipBuilder] Failed to load saved mesh! Returning runtime mesh.");
                return mesh;
            }
            return loadedMesh;
#else
            return mesh;
#endif
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

        private Shader GetShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }
    }
}

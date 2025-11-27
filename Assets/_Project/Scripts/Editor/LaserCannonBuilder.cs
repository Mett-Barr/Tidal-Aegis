using UnityEngine;
using UnityEditor;
using NavalCommand.Entities.Components;
using NavalCommand.Data;

public class LaserCannonBuilder : EditorWindow
{
    [MenuItem("Tools/Build Laser Cannon (New)")]
    public static void BuildLaserCannon()
    {
        CreateLaserCannon(Vector3.zero, Quaternion.identity, null);
    }

    [MenuItem("Tools/Replace Selected with Laser Cannon")]
    public static void ReplaceSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select the old turret to replace.", "OK");
            return;
        }

        // Capture state
        Vector3 pos = selected.transform.localPosition;
        Quaternion rot = selected.transform.localRotation;
        Transform parent = selected.transform.parent;
        string name = selected.name;

        // Create new
        GameObject newCannon = CreateLaserCannon(pos, rot, parent);
        newCannon.name = name + "_Refit";

        // Disable old
        Undo.RecordObject(selected, "Disable Old Turret");
        selected.SetActive(false);
        selected.name += "_OLD";

        // Select new
        Selection.activeGameObject = newCannon;
    }

    private static GameObject CreateLaserCannon(Vector3 localPos, Quaternion localRot, Transform parent)
    {
        // 1. Root
        GameObject root = new GameObject("LaserCannon_Spherical");
        Undo.RegisterCreatedObjectUndo(root, "Create Laser Cannon");
        
        if (parent != null) root.transform.SetParent(parent);
        root.transform.localPosition = localPos;
        root.transform.localRotation = localRot;

        // 2. Components
        var weaponCtrl = root.AddComponent<WeaponController>();
        var rotator = root.AddComponent<TurretRotator>();

        // 3. Visual Hierarchy
        // Material (Default standard)
        Material grayMat = new Material(Shader.Find("Standard"));
        grayMat.color = new Color(0.3f, 0.3f, 0.35f); // Dark Gray Metal

        Material darkMat = new Material(Shader.Find("Standard"));
        darkMat.color = new Color(0.1f, 0.1f, 0.15f); // Darker accents

        Material lensMat = new Material(Shader.Find("Standard"));
        lensMat.color = Color.cyan;
        lensMat.EnableKeyword("_EMISSION");
        lensMat.SetColor("_EmissionColor", Color.cyan * 2f);

        // --- Base ---
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(root.transform);
        baseObj.transform.localPosition = new Vector3(0, 0.25f, 0);
        baseObj.transform.localScale = new Vector3(2.0f, 0.5f, 2.0f);
        baseObj.GetComponent<Renderer>().sharedMaterial = grayMat;

        // --- Yoke (Azimuth) ---
        GameObject yokePivot = new GameObject("Yoke_Pivot");
        yokePivot.transform.SetParent(root.transform);
        yokePivot.transform.localPosition = new Vector3(0, 0.5f, 0); // Sit on top of base
        yokePivot.transform.localRotation = Quaternion.identity;

        // Yoke Crossbar
        GameObject yokeCross = GameObject.CreatePrimitive(PrimitiveType.Cube);
        yokeCross.name = "Yoke_Crossbar";
        yokeCross.transform.SetParent(yokePivot.transform);
        yokeCross.transform.localPosition = new Vector3(0, 0.2f, 0);
        yokeCross.transform.localScale = new Vector3(2.2f, 0.4f, 0.8f);
        yokeCross.GetComponent<Renderer>().sharedMaterial = grayMat;

        // Yoke Arms
        CreateYokeArm(yokePivot.transform, new Vector3(-0.9f, 1.0f, 0), grayMat);
        CreateYokeArm(yokePivot.transform, new Vector3(0.9f, 1.0f, 0), grayMat);

        // --- Sphere (Elevation) ---
        GameObject spherePivot = new GameObject("Sphere_Pivot");
        spherePivot.transform.SetParent(yokePivot.transform);
        spherePivot.transform.localPosition = new Vector3(0, 1.2f, 0); // Center of arms
        spherePivot.transform.localRotation = Quaternion.identity;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Turret_Sphere";
        sphere.transform.SetParent(spherePivot.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 1.6f;
        sphere.GetComponent<Renderer>().sharedMaterial = grayMat;

        // --- Lens/Muzzle ---
        GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        muzzle.name = "Muzzle_Housing";
        muzzle.transform.SetParent(spherePivot.transform);
        muzzle.transform.localRotation = Quaternion.Euler(90, 0, 0); // Face forward
        muzzle.transform.localPosition = new Vector3(0, 0, 0.6f);
        muzzle.transform.localScale = new Vector3(1.0f, 0.2f, 1.0f);
        muzzle.GetComponent<Renderer>().sharedMaterial = darkMat;

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lens.name = "Lens";
        lens.transform.SetParent(muzzle.transform);
        lens.transform.localRotation = Quaternion.identity;
        lens.transform.localPosition = new Vector3(0, 1.0f, 0); // Protrude slightly
        lens.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
        lens.GetComponent<Renderer>().sharedMaterial = lensMat;

        // --- FirePoint ---
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(spherePivot.transform);
        firePoint.transform.localPosition = new Vector3(0, 0, 1.5f); // In front of sphere
        firePoint.transform.localRotation = Quaternion.identity;

        // 4. Configuration
        rotator.Initialize(yokePivot.transform, spherePivot.transform, firePoint.transform);
        rotator.MinPitch = -20f;
        rotator.MaxPitch = 85f;

        weaponCtrl.FirePoint = firePoint.transform;
        
        // Create WeaponStatsSO instance
        WeaponStatsSO stats = ScriptableObject.CreateInstance<WeaponStatsSO>();
        stats.name = "LaserCIWS_Generated";
        stats.Type = WeaponType.LaserCIWS;
        stats.Mode = FiringMode.Beam;
        stats.TargetType = TargetCapability.Aircraft | TargetCapability.Missile;
        stats.ImpactProfile = new NavalCommand.Systems.VFX.ImpactProfile(NavalCommand.Systems.VFX.ImpactCategory.Energy, NavalCommand.Systems.VFX.ImpactSize.Small);
        
        stats.SetBaseRange(2000f);
        stats.SetBaseDamage(100f); // High DPS
        stats.SetBaseCooldown(0.1f);
        stats.SetBaseRotationSpeed(150f);
        stats.SetBaseRotationAcceleration(2000f);
        stats.SetBaseFiringAngleTolerance(2.0f);
        stats.SetBaseCanRotate(true);
        stats.SetBaseAimingLogicName("Direct");
        stats.ProjectileColor = Color.cyan;

        weaponCtrl.WeaponStats = stats;

        return root;
    }

    private static void CreateYokeArm(Transform parent, Vector3 pos, Material mat)
    {
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Yoke_Arm";
        arm.transform.SetParent(parent);
        arm.transform.localPosition = pos;
        arm.transform.localScale = new Vector3(0.4f, 1.8f, 0.8f);
        arm.GetComponent<Renderer>().sharedMaterial = mat;
    }
}

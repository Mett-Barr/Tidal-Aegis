#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using NavalCommand.Data;
using NavalCommand.Entities.Projectiles;
using NavalCommand.Entities.Components;
using NavalCommand.Entities.Units;
using System.IO;

namespace NavalCommand.Utils
{
    public class ContentGenerator : MonoBehaviour
    {
        [MenuItem("NavalCommand/Generate Basic Content")]
        public static void GenerateContent()
        {
            EnsureDirectories();
            GenerateProjectiles();
            GenerateWeaponStats();
            GenerateShips();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Basic Content Generated Successfully!");
        }

        private static void EnsureDirectories()
        {
            CreateDir("Assets/_Project/Prefabs/Projectiles");
            CreateDir("Assets/_Project/Prefabs/Enemies");
            CreateDir("Assets/_Project/Data/Weapons");
        }

        private static void CreateDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void GenerateProjectiles()
        {
            CreateProjectile("Projectile_FlagshipGun", Color.yellow, ProjectileType.Ballistic, 30f, 20f);
            CreateProjectile("Projectile_Missile", Color.red, ProjectileType.Homing, 15f, 50f);
            CreateProjectile("Projectile_Torpedo", Color.blue, ProjectileType.Straight, 10f, 80f); // Slow, high damage
            CreateProjectile("Projectile_Autocannon", new Color(1f, 0.5f, 0f), ProjectileType.Straight, 60f, 5f);
            CreateProjectile("Projectile_CIWS", Color.white, ProjectileType.Straight, 80f, 2f);
        }

        private static void CreateProjectile(string name, Color color, ProjectileType type, float speed, float damage)
        {
            string path = $"Assets/_Project/Prefabs/Projectiles/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = Vector3.one * 0.5f;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.sharedMaterial.color = color;

            // Physics
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = (type == ProjectileType.Ballistic);
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Behavior
            var proj = go.AddComponent<ProjectileBehavior>();
            proj.BehaviorType = type;
            proj.Speed = speed;
            proj.Damage = damage;
            proj.GravityMultiplier = (type == ProjectileType.Ballistic) ? 1f : 0f;

            // Collider
            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private static void GenerateWeaponStats()
        {
            CreateWeaponStats("Weapon_FlagshipGun_Basic", "Flagship Gun", WeaponType.FlagshipGun, 100f, 2f, 20f, "Projectile_FlagshipGun");
            CreateWeaponStats("Weapon_Missile_Basic", "Missile Launcher", WeaponType.Missile, 200f, 5f, 50f, "Projectile_Missile");
            CreateWeaponStats("Weapon_Torpedo_Basic", "Torpedo Tube", WeaponType.Torpedo, 150f, 8f, 80f, "Projectile_Torpedo");
            CreateWeaponStats("Weapon_Autocannon_Basic", "Autocannon", WeaponType.Autocannon, 50f, 0.2f, 5f, "Projectile_Autocannon");
            CreateWeaponStats("Weapon_CIWS_Basic", "CIWS", WeaponType.CIWS, 30f, 0.05f, 2f, "Projectile_CIWS");
        }

        private static void CreateWeaponStats(string name, string displayName, WeaponType type, float range, float cooldown, float damage, string projectileName)
        {
            string path = $"Assets/_Project/Data/Weapons/{name}.asset";
            WeaponStatsSO so = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(path);
            
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<WeaponStatsSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.DisplayName = displayName;
            so.Type = type;
            so.Range = range;
            so.Cooldown = cooldown;
            so.Damage = damage;
            
            string projPath = $"Assets/_Project/Prefabs/Projectiles/{projectileName}.prefab";
            so.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
            
            // Default mask (Everything)
            so.TargetMask = ~0; 

            EditorUtility.SetDirty(so);
        }

        private static void GenerateShips()
        {
            CreateShip("Ship_Light_FlagshipGun", "Weapon_FlagshipGun_Basic", Color.gray);
            CreateShip("Ship_Light_Missile", "Weapon_Missile_Basic", new Color(0.4f, 0.4f, 0.4f));
            CreateShip("Ship_Light_Torpedo", "Weapon_Torpedo_Basic", new Color(0.3f, 0.3f, 0.5f));
            CreateShip("Ship_Light_Autocannon", "Weapon_Autocannon_Basic", new Color(0.5f, 0.5f, 0.3f));
            CreateShip("Ship_Light_CIWS", "Weapon_CIWS_Basic", new Color(0.6f, 0.6f, 0.6f));
        }

        private static void CreateShip(string name, string weaponStatsName, Color color)
        {
            string path = $"Assets/_Project/Prefabs/Enemies/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            // Lay flat
            go.transform.rotation = Quaternion.Euler(90, 0, 0); 
            go.transform.localScale = new Vector3(2, 1, 1); // Elongated

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.sharedMaterial.color = color;

            // Physics
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 1f;
            rb.angularDrag = 1f;

            // Unit Controller
            var unit = go.AddComponent<EnemyUnit>();
            // Basic stats
            // unit.MaxHP = 100f; // Assuming BaseUnit has this, if not we rely on defaults

            // Weapon
            GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turret.name = "Turret";
            turret.transform.SetParent(go.transform);
            turret.transform.localPosition = new Vector3(0, 0.5f, 0); // On top
            turret.transform.localScale = Vector3.one * 0.5f;
            DestroyImmediate(turret.GetComponent<BoxCollider>()); // Remove collider from visual

            var weapon = turret.AddComponent<WeaponController>();
            string statsPath = $"Assets/_Project/Data/Weapons/{weaponStatsName}.asset";
            weapon.WeaponStats = AssetDatabase.LoadAssetAtPath<WeaponStatsSO>(statsPath);
            weapon.FirePoint = turret.transform; // Fire from center for now
            weapon.OwnerTeam = NavalCommand.Core.Team.Enemy;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }
    }
}
#endif

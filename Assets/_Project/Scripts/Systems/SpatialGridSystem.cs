using System.Collections.Generic;
using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Data; // Added for TargetCapability

namespace NavalCommand.Systems
{
    public class SpatialGridSystem : MonoBehaviour
    {
        // ... (Existing code)

        public List<IDamageable> GetTargetsInRange(Vector3 origin, float range, Team searchTeam, TargetCapability targetMask = TargetCapability.Surface)
        {
            List<IDamageable> results = new List<IDamageable>();
            Vector2Int centerCell = GetCellPos(origin);
            int cellRange = Mathf.CeilToInt(range / CellSize);

            // Search neighboring cells
            for (int x = -cellRange; x <= cellRange; x++)
            {
                for (int y = -cellRange; y <= cellRange; y++)
                {
                    Vector2Int checkCell = centerCell + new Vector2Int(x, y);
                    
                    if (grid.TryGetValue(checkCell, out List<IDamageable> cellContent))
                    {
                        foreach (var target in cellContent)
                        {
                            // Basic filtering
                            if (target.GetTeam() == searchTeam && !target.IsDead())
                            {
                                // Check Target Capability
                                UnitType targetType = target.GetUnitType();
                                bool isTargetable = false;

                                if (targetType == UnitType.Surface && (targetMask & TargetCapability.Surface) != 0) isTargetable = true;
                                if (targetType == UnitType.Air && (targetMask & TargetCapability.Air) != 0) isTargetable = true;
                                if (targetType == UnitType.Missile && (targetMask & TargetCapability.Missile) != 0) isTargetable = true;

                                if (isTargetable)
                                {
                                    float distSqr = (origin - ((MonoBehaviour)target).transform.position).sqrMagnitude;
                                    if (distSqr <= range * range)
                                    {
                                        results.Add(target);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
        public static SpatialGridSystem Instance { get; private set; }

        [Header("Settings")]
        public float CellSize = 2000f; // Increased from 50f to reduce iteration count for long-range weapons

        // Dictionary mapping Grid Coordinates (x,y) to a List of Damageables in that cell
        private Dictionary<Vector2Int, List<IDamageable>> grid = new Dictionary<Vector2Int, List<IDamageable>>();
        
        // Helper to track which cell an object is currently in, to handle moves
        private Dictionary<IDamageable, Vector2Int> objectCellMap = new Dictionary<IDamageable, Vector2Int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[SpatialGridSystem] Initialized.");
            }
            else
            {
                Debug.LogWarning("[SpatialGridSystem] Duplicate instance destroyed.");
                Destroy(gameObject);
            }
        }

        public void Register(IDamageable obj, Vector3 position)
        {
            Vector2Int cell = GetCellPos(position);
            
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new List<IDamageable>();
            }

            grid[cell].Add(obj);
            objectCellMap[obj] = cell;
            Debug.Log($"[SpatialGridSystem] Registered {((MonoBehaviour)obj).name} at {cell}. Team: {obj.GetTeam()}");
        }

        public void Unregister(IDamageable obj)
        {
            if (objectCellMap.TryGetValue(obj, out Vector2Int cell))
            {
                if (grid.ContainsKey(cell))
                {
                    grid[cell].Remove(obj);
                }
                objectCellMap.Remove(obj);
                Debug.Log($"[SpatialGridSystem] Unregistered {((MonoBehaviour)obj).name} from {cell}");
            }
        }

        public void UpdatePosition(IDamageable obj, Vector3 newPosition)
        {
            Vector2Int newCell = GetCellPos(newPosition);

            if (objectCellMap.TryGetValue(obj, out Vector2Int oldCell))
            {
                if (oldCell != newCell)
                {
                    // Move from old to new
                    if (grid.ContainsKey(oldCell))
                    {
                        grid[oldCell].Remove(obj);
                    }

                    if (!grid.ContainsKey(newCell))
                    {
                        grid[newCell] = new List<IDamageable>();
                    }

                    grid[newCell].Add(obj);
                    objectCellMap[obj] = newCell;
                }
            }
            else
            {
                // Not registered yet
                Register(obj, newPosition);
            }
        }



        private Vector2Int GetCellPos(Vector3 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / CellSize),
                Mathf.FloorToInt(pos.z / CellSize) // Assuming Y-up, so Z is the other horizontal axis
            );
        }
    }
}

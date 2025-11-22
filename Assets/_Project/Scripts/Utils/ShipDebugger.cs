using UnityEngine;
using NavalCommand.Entities.Units;
using NavalCommand.Systems;
using NavalCommand.Data;

namespace NavalCommand.Utils
{
    public class ShipDebugger : MonoBehaviour
    {
        private BaseUnit unit;
        private MeshRenderer[] renderers;

        private void Start()
        {
            unit = GetComponent<BaseUnit>();
            renderers = GetComponentsInChildren<MeshRenderer>();
            
            Debug.Log($"[ShipDebugger] Started on {name}");
            InvokeRepeating(nameof(LogStatus), 1f, 2f);
        }

        private void LogStatus()
        {
            string status = $"[ShipDebugger] Status Report for {name}:\n";
            
            // 1. Transform
            status += $"Pos: {transform.position}, Rot: {transform.rotation.eulerAngles}, Scale: {transform.localScale}\n";
            
            // 2. Visuals
            if (renderers.Length == 0)
            {
                status += "<color=red>VISUALS: No MeshRenderers found!</color>\n";
            }
            else
            {
                int enabledCount = 0;
                Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
                foreach (var r in renderers)
                {
                    if (r.enabled)
                    {
                        enabledCount++;
                        totalBounds.Encapsulate(r.bounds);
                    }
                }
                status += $"VISUALS: {enabledCount}/{renderers.Length} Renderers Enabled. Total Bounds: {totalBounds}\n";
                
                // Check Layer
                status += $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})\n";
            }

            // 3. Gameplay
            if (unit != null)
            {
                status += $"HP: {unit.CurrentHP}/{unit.MaxHP}, Team: {unit.UnitTeam}\n";
                
                // 4. Spatial Grid
                if (SpatialGridSystem.Instance != null)
                {
                    // We can't easily check internal dictionary, but we can check if we are found
                    var targets = SpatialGridSystem.Instance.GetTargetsInRange(transform.position, 100f, unit.UnitTeam == Core.Team.Player ? Core.Team.Enemy : Core.Team.Player, TargetCapability.Surface | TargetCapability.Air | TargetCapability.Missile);
                    status += $"Grid Check: Found {targets.Count} enemies nearby.\n";
                }
                else
                {
                    status += "<color=red>GRID: SpatialGridSystem Instance is NULL!</color>\n";
                }
            }
            else
            {
                status += "<color=red>UNIT: No BaseUnit/FlagshipController found!</color>\n";
            }

            Debug.Log(status);
        }
    }
}

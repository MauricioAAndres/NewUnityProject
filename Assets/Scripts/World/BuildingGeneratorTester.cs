using System.Text;
using UnityEngine;

namespace NewUnityProject.World
{
    // MonoBehaviour de prueba: genera un edificio y lo loguea por consola con Debug.Log.
    public class BuildingGeneratorTester : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;

        public BuildingBuilderSettings settings; // null → usa defaults en runtime.
        public bool buildGeometry = true;        // si es true, Generate Building también construye.

        private BuildingData lastBuilding;

        [ContextMenu("Generate Building")]
        public void GenerateBuilding()
        {
            lastBuilding = BuildingGenerator.Generate(seed, null);
            Debug.Log(BuildLog(lastBuilding));

            if (buildGeometry)
                BuildGeometry();
        }

        [ContextMenu("Build Geometry")]
        public void BuildGeometry()
        {
            if (lastBuilding == null)
            {
                Debug.LogWarning("No hay BuildingData generado. Ejecutá primero 'Generate Building'.");
                return;
            }

            // Limpia la geometría previa de este tester para no acumular edificios.
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            BuildingBuilder.Build(lastBuilding, settings, transform);
        }

        private static string BuildLog(BuildingData building)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Edificio generado ===");
            sb.AppendLine($"Nombre: {building.name}");
            sb.AppendLine($"Seed: {building.seed}");
            sb.AppendLine($"Pisos: {building.floors.Count}");

            foreach (var floor in building.floors)
            {
                sb.AppendLine();
                sb.AppendLine($"--- Piso {floor.index} ({floor.type}) ---");
                sb.AppendLine($"  Medidas: {floor.width}x{floor.depth} | Elevación: {floor.elevation}m");
                sb.AppendLine($"  Espacios comunes ({floor.commonRooms.Count}):");
                foreach (var room in floor.commonRooms)
                    sb.AppendLine($"    {room.type} @({room.x},{room.z}) {room.width}x{room.depth}");

                sb.AppendLine($"  Departamentos ({floor.apartments.Count}):");
                foreach (var apt in floor.apartments)
                {
                    sb.AppendLine($"    {apt.id} @({apt.x},{apt.z}) {apt.width}x{apt.depth} flipZ={apt.flipZ}");
                    foreach (var room in apt.rooms)
                        sb.AppendLine($"      {room.type} @({room.x},{room.z}) {room.width}x{room.depth}");
                    foreach (var door in apt.doors)
                        sb.AppendLine($"      puerta {door.from}->{door.to} @({door.x},{door.z}) {door.direction}");
                }
            }

            return sb.ToString();
        }
    }
}

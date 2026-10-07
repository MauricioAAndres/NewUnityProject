using UnityEngine;

namespace NewUnityProject.World
{
    // Parámetros y colores para construir la geometría de debug del edificio.
    // Se puede crear como asset (Create Asset Menu) o dejar el campo del tester en null:
    // en ese caso BuildingBuilder crea una instancia por defecto en runtime.
    [CreateAssetMenu(menuName = "World/Building Builder Settings", fileName = "BuildingBuilderSettings")]
    public class BuildingBuilderSettings : ScriptableObject
    {
        [Header("Medidas")]
        // = floorHeight(3) - slabThickness(0.1). Debe ser >= floorHeight - slabThickness
        // para que las paredes lleguen hasta la losa del piso siguiente (sin gap).
        public float wallHeight = 2.9f;
        public float wallThickness = 0.2f;
        public float slabThickness = 0.1f;

        [Header("Puertas")]
        public float doorWidth = 0.8f;      // ancho de la hoja (cabe en el vano de 1.0).
        public float doorHeight = 2.0f;     // alto de la hoja.
        public float doorThickness = 0.05f; // espesor de la hoja.
        public float doorOpenAngle = 90f;   // ángulo de apertura sobre Y.

        [Header("Escaleras")]
        public float stepRise = 0.15f;          // alto de cada peldaño.
        public float stepRun = 0.3f;            // fondo de cada peldaño.
        public float stairWidth = 1.5f;         // ancho del tramo.
        public float rampThickness = 0.2f;      // grosor del collider de rampa.
        public Color stairColor = new Color(0.8f, 0.8f, 0.8f); // gris claro.

        [Header("Colores por tipo de room")]
        public Color corridorColor = new Color(0.72f, 0.72f, 0.72f); // gris claro
        public Color hallwayColor = new Color(0.5f, 0.5f, 0.5f);     // gris medio
        public Color livingColor = new Color(0.95f, 0.85f, 0.2f);    // amarillo
        public Color kitchenColor = new Color(0.3f, 0.8f, 0.35f);    // verde
        public Color bathroomColor = new Color(0.4f, 0.75f, 0.95f);  // celeste
        public Color bedroomColor = new Color(0.95f, 0.55f, 0.15f);  // naranja
        public Color storageColor = new Color(0.5f, 0.35f, 0.2f);    // marrón
        public Color coreColor = new Color(0.6f, 0.2f, 0.2f);        // rojo apagado
        public Color commonColor = new Color(0.92f, 0.92f, 0.9f);    // blanco humo
        public Color balconyColor = new Color(0.55f, 0.85f, 0.55f);  // verde claro
        public Color entranceColor = new Color(0.6f, 0.4f, 0.9f);    // violeta

        [Header("Materiales base")]
        public Color slabColor = new Color(0.18f, 0.18f, 0.18f); // gris oscuro (losa)
        public Color wallColor = new Color(0.82f, 0.82f, 0.82f); // gris claro (pared)

        public Color GetColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Corridor: return corridorColor;
                case RoomType.Hallway: return hallwayColor;
                case RoomType.Living: return livingColor;
                case RoomType.Kitchen: return kitchenColor;
                case RoomType.Bathroom: return bathroomColor;
                case RoomType.Bedroom: return bedroomColor;
                case RoomType.Storage: return storageColor;
                case RoomType.Core: return coreColor;
                case RoomType.Common: return commonColor;
                case RoomType.Balcony: return balconyColor;
                case RoomType.Entrance: return entranceColor;
                default: return Color.magenta; // no debería ocurrir.
            }
        }
    }
}

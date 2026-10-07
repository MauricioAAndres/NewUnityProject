using System;

namespace NewUnityProject.World
{
    // Parámetros del generador de edificios. Valores provisorios hardcodeados.
    [Serializable]
    public class BuildingConfig
    {
        // Cantidad de pisos de departamentos (pisos 1..N).
        public int apartmentFloors = 5;

        // Medidas del piso de deptos en metros (grilla 1u = 1m).
        public int apartmentFloorWidth = 28;
        public int apartmentFloorDepth = 24;

        // Core de escaleras (cuadrado: coreSize x coreSize).
        public int coreSize = 4;

        // Ancho de los corredores en cruz.
        public int corridorWidth = 4;

        // Medidas de cada departamento.
        public int apartmentWidth = 12;
        public int apartmentDepth = 10;

        // Altura por piso en metros (para calcular elevation).
        public int floorHeight = 3;

        // Probabilidad de que un depto tenga 2 dormitorios (0..1).
        public float twoBedroomChance = 0.6f;

        // Secciones a incluir.
        public bool includeBasement = true;
        public bool includeLobby = true;
        public bool includeRooftop = true;

        // Configuración por defecto.
        public static readonly BuildingConfig Default = new BuildingConfig();
    }
}

using System;
using System.Collections.Generic;

namespace NewUnityProject.World
{
    // Resultado completo de la generación de un edificio.
    [Serializable]
    public class BuildingData
    {
        public int seed;
        public string name;
        public List<FloorData> floors = new List<FloorData>();
    }

    // Un piso del edificio.
    [Serializable]
    public class FloorData
    {
        public int index;       // -1 sótano, 0 lobby, 1..5 deptos, 6 azotea.
        public FloorType type;
        public int width;       // en metros (grilla 1u = 1m).
        public int depth;       // en metros.
        public int elevation;   // altura del piso en metros (index * floorHeight).
        public List<ApartmentData> apartments = new List<ApartmentData>();
        public List<RoomData> commonRooms = new List<RoomData>(); // core, corredores, entrada, etc.
        public List<DoorData> commonDoors = new List<DoorData>(); // puertas entre rooms comunes (cores/corredores), coords de piso.
    }

    // Un departamento dentro de un piso.
    [Serializable]
    public class ApartmentData
    {
        public string id;         // identificador único, ej. "Apt-3-0".
        public int floorIndex;
        public int indexOnFloor;  // 0..3, uno por esquina.
        public int x;             // origen (esquina SO) en coords del piso.
        public int z;             // origen (esquina SO) en coords del piso.
        public int width;
        public int depth;
        public bool flipZ;        // true = espejar el template en Z (deptos del sur).
        public List<RoomData> rooms = new List<RoomData>(); // coords locales al depto.
        public List<DoorData> doors = new List<DoorData>(); // coords locales al depto.
    }

    // Una habitación/espacio rectangular.
    // En apartments: coords locales al depto. En commonRooms: coords del piso.
    [Serializable]
    public class RoomData
    {
        public RoomType type;
        public int x;
        public int z;
        public int width;
        public int depth;
    }

    // Una puerta entre dos espacios.
    [Serializable]
    public class DoorData
    {
        public RoomType from;
        public RoomType to;
        public int x;
        public int z;
        public Direction direction;
    }
}

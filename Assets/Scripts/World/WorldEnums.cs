using System;

namespace NewUnityProject.World
{
    // Tipos de piso de un edificio.
    [Serializable]
    public enum FloorType
    {
        Basement,   // Sótano (piso -1): almacenamiento + core de escaleras.
        Lobby,      // Planta baja (piso 0): entrada/lobby, sin deptos.
        Apartments, // Pisos de deptos (1..5).
        Rooftop     // Azotea/balcón (piso 6), sin deptos.
    }

    // Tipos de habitación/espacio.
    [Serializable]
    public enum RoomType
    {
        Corridor, // Corredor del edificio.
        Hallway,  // Pasillo interno del depto.
        Living,   // Living.
        Kitchen,  // Cocina.
        Bathroom, // Baño.
        Bedroom,  // Dormitorio.
        Storage,  // Almacenamiento (sótano).
        Common,   // Espacio común genérico.
        Core,     // Core de escaleras.
        Balcony,  // Balcón / azotea.
        Entrance  // Entrada del edificio.
    }

    // Puntos cardinales (para orientación de puertas).
    [Serializable]
    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}

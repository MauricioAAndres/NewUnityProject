using System;
using System.Collections.Generic;

namespace NewUnityProject.World
{
    // Lógica pura de generación del edificio. Sin dependencias de UnityEngine/MonoBehaviour,
    // por lo que es testeable sin abrir una escena. Reproducible: usa System.Random con el seed.
    public static class BuildingGenerator
    {
        public static BuildingData Generate(int seed, BuildingConfig config)
        {
            if (config == null) config = BuildingConfig.Default;

            // RNG reproducible (NO UnityEngine.Random): el mismo seed da el mismo edificio.
            var rng = new Random(seed);

            var building = new BuildingData
            {
                seed = seed,
                name = $"Edificio_{seed}"
            };

            // Sótano (piso -1): almacenamiento + core de escaleras.
            if (config.includeBasement)
                building.floors.Add(GenerateBasement(config));

            // Planta baja (piso 0): entrada/lobby, sin deptos.
            if (config.includeLobby)
                building.floors.Add(GenerateLobby(config));

            // Pisos de deptos (1..N).
            for (int f = 1; f <= config.apartmentFloors; f++)
                building.floors.Add(GenerateApartmentFloor(f, config, rng));

            // Azotea/balcón (piso N+1): sin deptos.
            if (config.includeRooftop)
                building.floors.Add(GenerateRooftop(config));

            return building;
        }

        // Z de la banda horizontal (cores + corredor), centrada en profundidad.
        private static int CoreZ(BuildingConfig c) => (c.apartmentFloorDepth - c.coreSize) / 2;

        // X del corredor vertical, centrado en ancho.
        private static int VerticalX(BuildingConfig c) => (c.apartmentFloorWidth - c.corridorWidth) / 2;

        // Agrega los dos cores (oeste y este) en los extremos del corredor horizontal.
        private static void AddCores(FloorData floor, BuildingConfig c)
        {
            int coreZ = CoreZ(c);
            floor.commonRooms.Add(new RoomData { type = RoomType.Core, x = 0, z = coreZ, width = c.coreSize, depth = c.coreSize });
            floor.commonRooms.Add(new RoomData { type = RoomType.Core, x = c.apartmentFloorWidth - c.coreSize, z = coreZ, width = c.coreSize, depth = c.coreSize });
        }

        // Corredores: uno horizontal (entre los cores) y dos verticales (norte y sur).
        private static void AddCorridors(FloorData floor, BuildingConfig c)
        {
            int coreZ = CoreZ(c);
            int vx = VerticalX(c);

            // Horizontal: entre los dos cores (x = coreSize .. width-coreSize).
            floor.commonRooms.Add(new RoomData { type = RoomType.Corridor, x = c.coreSize, z = coreZ, width = c.apartmentFloorWidth - 2 * c.coreSize, depth = c.corridorWidth });
            // Vertical sur: de z=0 hasta la banda horizontal.
            floor.commonRooms.Add(new RoomData { type = RoomType.Corridor, x = vx, z = 0, width = c.corridorWidth, depth = coreZ });
            // Vertical norte: de la banda horizontal hasta z=depth.
            floor.commonRooms.Add(new RoomData { type = RoomType.Corridor, x = vx, z = coreZ + c.corridorWidth, width = c.corridorWidth, depth = c.apartmentFloorDepth - coreZ - c.corridorWidth });
        }

        private static FloorData NewFloor(int index, FloorType type, BuildingConfig c)
        {
            return new FloorData
            {
                index = index,
                type = type,
                width = c.apartmentFloorWidth,
                depth = c.apartmentFloorDepth,
                elevation = index * c.floorHeight
            };
        }

        private static FloorData GenerateBasement(BuildingConfig c)
        {
            var floor = NewFloor(-1, FloorType.Basement, c);
            AddCores(floor, c);
            // Almacenamiento: ocupa todo el piso (los cores quedan dentro; la geometría lo restará).
            floor.commonRooms.Add(new RoomData
            {
                type = RoomType.Storage,
                x = 0,
                z = 0,
                width = c.apartmentFloorWidth,
                depth = c.apartmentFloorDepth
            });
            // Puertas comunes: cada core conecta con la Storage (borde interior, segmento centrado).
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Storage, x = 4, z = 11, direction = Direction.East });
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Storage, x = 24, z = 11, direction = Direction.West });
            return floor;
        }

        private static FloorData GenerateLobby(BuildingConfig c)
        {
            var floor = NewFloor(0, FloorType.Lobby, c);
            AddCores(floor, c);
            AddCorridors(floor, c);
            // Entrada del edificio, al pie del corredor vertical (lado sur).
            floor.commonRooms.Add(new RoomData
            {
                type = RoomType.Entrance,
                x = VerticalX(c),
                z = 0,
                width = c.corridorWidth,
                depth = c.corridorWidth
            });
            // Puertas comunes: cada core conecta con el corredor horizontal (el común central).
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Corridor, x = 4, z = 11, direction = Direction.East });
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Corridor, x = 24, z = 11, direction = Direction.West });
            return floor;
        }

        private static FloorData GenerateApartmentFloor(int index, BuildingConfig c, Random rng)
        {
            var floor = NewFloor(index, FloorType.Apartments, c);
            AddCores(floor, c);
            AddCorridors(floor, c);

            // Puertas comunes: cores → corredor horizontal, y corredor horizontal ↔ verticales.
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Corridor, x = 4, z = 11, direction = Direction.East });   // core oeste → corredor
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Corridor, x = 24, z = 11, direction = Direction.West });  // core este → corredor
            floor.commonDoors.Add(new DoorData { from = RoomType.Corridor, to = RoomType.Corridor, x = 13, z = 10, direction = Direction.South }); // horizontal ↔ vertical sur
            floor.commonDoors.Add(new DoorData { from = RoomType.Corridor, to = RoomType.Corridor, x = 13, z = 14, direction = Direction.North }); // horizontal ↔ vertical norte

            // Esquinas de los 4 deptos. Convención de piso: origen = esquina SO,
            // x crece al Este (0..ancho), z crece al Norte (0..profundidad).
            int northAptZ = CoreZ(c) + c.corridorWidth; // borde norte del corredor horizontal.
            int eastAptX = VerticalX(c) + c.corridorWidth;  // borde este del corredor vertical.

            // Orden de esquinas: NW(0), NE(1), SE(2), SW(3).
            // Los del sur (SE, SW) usan flipZ=true para espejar el template.
            AddApartment(floor, index, 0, 0,        northAptZ, false, c, rng); // NW
            AddApartment(floor, index, 1, eastAptX, northAptZ, false, c, rng); // NE
            AddApartment(floor, index, 2, eastAptX, 0,         true,  c, rng); // SE (sur)
            AddApartment(floor, index, 3, 0,        0,         true,  c, rng); // SW (sur)

            return floor;
        }

        private static void AddApartment(FloorData floor, int floorIndex, int indexOnFloor,
            int x, int z, bool flipZ, BuildingConfig c, Random rng)
        {
            var apt = new ApartmentData
            {
                id = $"Apt-{floorIndex}-{indexOnFloor}",
                floorIndex = floorIndex,
                indexOnFloor = indexOnFloor,
                x = x,
                z = z,
                width = c.apartmentWidth,
                depth = c.apartmentDepth,
                flipZ = flipZ
            };

            // 60% de chance de 2 dormitorios (config.twoBedroomChance).
            bool twoBedrooms = rng.NextDouble() < c.twoBedroomChance;
            BuildApartmentTemplate(apt, twoBedrooms);

            floor.apartments.Add(apt);
        }

        // Template de depto en coords locales (origen = esquina SO del depto, x al Este, z al Norte).
        // Entrada en z=0 (lado sur) mirando al norte; los ambientes crecen hacia z=10.
        private static void BuildApartmentTemplate(ApartmentData apt, bool twoBedrooms)
        {
            // Franja z:0..3 (entrada).
            apt.rooms.Add(new RoomData { type = RoomType.Bathroom, x = 0, z = 0, width = 3, depth = 3 });
            apt.rooms.Add(new RoomData { type = RoomType.Hallway,  x = 3, z = 0, width = 4, depth = 3 });
            apt.rooms.Add(new RoomData { type = RoomType.Kitchen,  x = 7, z = 0, width = 5, depth = 3 });

            // Franja z:3..6 (living).
            apt.rooms.Add(new RoomData { type = RoomType.Living, x = 0, z = 3, width = 12, depth = 3 });

            // Franja z:6..10 (dormitorios).
            if (twoBedrooms)
            {
                apt.rooms.Add(new RoomData { type = RoomType.Bedroom, x = 0, z = 6, width = 6, depth = 4 });
                apt.rooms.Add(new RoomData { type = RoomType.Bedroom, x = 6, z = 6, width = 6, depth = 4 });
            }
            else
            {
                apt.rooms.Add(new RoomData { type = RoomType.Bedroom, x = 0, z = 6, width = 12, depth = 4 });
            }

            // Puertas.
            apt.doors.Add(new DoorData { from = RoomType.Corridor, to = RoomType.Hallway,  x = 5, z = 0, direction = Direction.South });
            apt.doors.Add(new DoorData { from = RoomType.Hallway,  to = RoomType.Bathroom, x = 3, z = 1, direction = Direction.West });
            apt.doors.Add(new DoorData { from = RoomType.Hallway,  to = RoomType.Kitchen,  x = 7, z = 1, direction = Direction.East });
            apt.doors.Add(new DoorData { from = RoomType.Hallway,  to = RoomType.Living,   x = 5, z = 3, direction = Direction.North });

            if (twoBedrooms)
            {
                apt.doors.Add(new DoorData { from = RoomType.Living, to = RoomType.Bedroom, x = 3, z = 6, direction = Direction.North });
                apt.doors.Add(new DoorData { from = RoomType.Living, to = RoomType.Bedroom, x = 9, z = 6, direction = Direction.North });
            }
            else
            {
                apt.doors.Add(new DoorData { from = RoomType.Living, to = RoomType.Bedroom, x = 6, z = 6, direction = Direction.North });
            }
        }

        private static FloorData GenerateRooftop(BuildingConfig c)
        {
            var floor = NewFloor(c.apartmentFloors + 1, FloorType.Rooftop, c);
            AddCores(floor, c); // acceso a la azotea por los cores.
            // Azotea/balcón: toda la superficie.
            floor.commonRooms.Add(new RoomData
            {
                type = RoomType.Balcony,
                x = 0,
                z = 0,
                width = c.apartmentFloorWidth,
                depth = c.apartmentFloorDepth
            });
            // Puertas comunes: cada core conecta con el Balcony (borde interior).
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Balcony, x = 4, z = 11, direction = Direction.East });
            floor.commonDoors.Add(new DoorData { from = RoomType.Core, to = RoomType.Balcony, x = 24, z = 11, direction = Direction.West });
            return floor;
        }
    }
}

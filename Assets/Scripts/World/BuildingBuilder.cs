using System.Collections.Generic;
using UnityEngine;

namespace NewUnityProject.World
{
    // Convierte un BuildingData en geometría 3D de debug (cubos) en la escena.
    // Sin arte, sin física custom: cubos + materiales creados en runtime.
    public static class BuildingBuilder
    {
        // Grosor y altura de los pisos de room (visualización de tipos), apenas por encima de la losa.
        private const float RoomFloorThickness = 0.02f;
        private const float RoomFloorY = 0.012f; // centro en Y (evita z-fighting con la losa).

        public static GameObject Build(BuildingData data, BuildingBuilderSettings settings, Transform parent = null)
        {
            if (data == null) return null;
            if (settings == null) settings = ScriptableObject.CreateInstance<BuildingBuilderSettings>();

            var root = new GameObject($"Building_{data.name}");
            if (parent != null)
                root.transform.SetParent(parent, false);

            // Materiales compartidos por color (uno por color distinto, no uno por cubo).
            var materials = new Dictionary<Color, Material>();

            for (int i = 0; i < data.floors.Count; i++)
            {
                FloorData next = (i + 1 < data.floors.Count) ? data.floors[i + 1] : null;
                BuildFloor(data.floors[i], next, settings, root.transform, materials);
            }

            return root;
        }

        private static void BuildFloor(FloorData floor, FloorData next, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            var floorGO = new GameObject($"Floor_{floor.index}");
            floorGO.transform.SetParent(parent, false);
            floorGO.transform.localPosition = new Vector3(0f, floor.elevation, 0f);

            BuildSlab(floor, settings, floorGO.transform, materials);
            BuildRoomFloors(floor, settings, floorGO.transform, materials);
            BuildWalls(floor, settings, floorGO.transform, materials);
            BuildDoors(floor, settings, floorGO.transform, materials);

            // Escaleras en los cores (para todos los pisos excepto el último).
            if (next != null)
                BuildStairs(floor, next, settings, floorGO.transform, materials);
        }

        private static void BuildSlab(FloorData floor, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"Slab_Floor_{floor.index}";
            slab.transform.SetParent(parent, false);
            slab.transform.localScale = new Vector3(floor.width, settings.slabThickness, floor.depth);
            slab.transform.localPosition = new Vector3(floor.width / 2f, -settings.slabThickness / 2f, floor.depth / 2f);
            SetMaterial(slab, settings.slabColor, materials);
        }

        private static void BuildRoomFloors(FloorData floor, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            // Rooms de deptos: coords locales → coords de piso (origen + flipZ).
            foreach (var apt in floor.apartments)
            {
                foreach (var room in apt.rooms)
                {
                    var r = RoomToFloor(apt, room);
                    BuildRoomFloor(parent, room.type, r.x, r.z, r.width, r.depth, $"Floor_{apt.id}_{room.type}", settings, materials);
                }
            }

            // Rooms comunes: ya están en coords de piso.
            foreach (var room in floor.commonRooms)
                BuildRoomFloor(parent, room.type, room.x, room.z, room.width, room.depth, $"Floor_Common_{room.type}_{room.x}_{room.z}", settings, materials);
        }

        private static void BuildRoomFloor(Transform parent, RoomType type, int x, int z, int w, int d, string name, BuildingBuilderSettings settings, Dictionary<Color, Material> materials)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(w, RoomFloorThickness, d);
            go.transform.localPosition = new Vector3(x + w / 2f, RoomFloorY, z + d / 2f);
            SetMaterial(go, settings.GetColor(type), materials);
        }

        private static void BuildWalls(FloorData floor, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            var walls = CollectWalls(floor);
            foreach (var wall in walls)
                BuildWall(wall, settings, parent, materials);
        }

        private static void BuildWall(WallKey wall, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{wall.orientation}_{wall.x}_{wall.z}";

            if (wall.orientation == WallOrientation.Horizontal)
            {
                go.transform.localScale = new Vector3(1f, settings.wallHeight, settings.wallThickness);
                go.transform.localPosition = new Vector3(wall.x + 0.5f, settings.wallHeight / 2f, wall.z);
            }
            else
            {
                go.transform.localScale = new Vector3(settings.wallThickness, settings.wallHeight, 1f);
                go.transform.localPosition = new Vector3(wall.x, settings.wallHeight / 2f, wall.z + 0.5f);
            }

            go.transform.SetParent(parent, false);
            SetMaterial(go, settings.wallColor, materials);
        }

        // Colecta las paredes del piso: suma los 4 bordes de cada room y quita las puertas.
        // Incluye debug logging temporal para verificar la remoción de puertas.
        private static HashSet<WallKey> CollectWalls(FloorData floor)
        {
            var walls = new HashSet<WallKey>();

            // Rooms comunes (coords de piso).
            foreach (var room in floor.commonRooms)
                AddRoomWalls(walls, room.x, room.z, room.width, room.depth);

            int totalDoors = 0;
            int removedDoors = 0;

            // Rooms de deptos (coords locales → piso) y sus puertas.
            foreach (var apt in floor.apartments)
            {
                foreach (var room in apt.rooms)
                {
                    var r = RoomToFloor(apt, room);
                    AddRoomWalls(walls, r.x, r.z, r.width, r.depth);
                }
                foreach (var door in apt.doors)
                {
                    totalDoors++;
                    var d = DoorToFloor(apt, door);
                    var wk = DoorWallKey(d.x, d.z, d.direction);
                    bool removed = RemoveDoorWall(walls, d.x, d.z, d.direction);
                    if (removed) removedDoors++;

                    Debug.Log($"Door {door.from}->{door.to} @({door.x},{door.z}) {door.direction} → WallKey({wk.x},{wk.z},{wk.orientation}) removed={removed}");

                    if (!removed)
                        LogMissingWallWarning(wk, walls);
                }
            }

            // Puertas comunes (cores/corredores), ya en coords de piso.
            foreach (var door in floor.commonDoors)
            {
                totalDoors++;
                var wk = DoorWallKey(door.x, door.z, door.direction);
                bool removed = RemoveDoorWall(walls, door.x, door.z, door.direction);
                if (removed) removedDoors++;

                Debug.Log($"Door {door.from}->{door.to} @({door.x},{door.z}) {door.direction} → WallKey({wk.x},{wk.z},{wk.orientation}) removed={removed}");

                if (!removed)
                    LogMissingWallWarning(wk, walls);
            }

            Debug.Log($"Floor {floor.index} ({floor.type}): walls={walls.Count} doors_removed={removedDoors}/{totalDoors}");

            return walls;
        }

        private static void LogMissingWallWarning(WallKey target, HashSet<WallKey> walls)
        {
            string list = "";
            foreach (var w in walls)
                if (w.x == target.x || w.z == target.z)
                    list += $"  ({w.x},{w.z},{w.orientation})\n";
            Debug.LogWarning($"WARNING: WallKey({target.x},{target.z},{target.orientation}) no removida. Cercanas (mismo x o z):\n{list}");
        }

        // Agrega los 4 bordes de un room al set de paredes.
        private static void AddRoomWalls(HashSet<WallKey> walls, int rx, int rz, int rw, int rd)
        {
            for (int i = 0; i < rw; i++)
            {
                walls.Add(new WallKey(rx + i, rz, WallOrientation.Horizontal));     // lado sur
                walls.Add(new WallKey(rx + i, rz + rd, WallOrientation.Horizontal)); // lado norte
            }
            for (int j = 0; j < rd; j++)
            {
                walls.Add(new WallKey(rx, rz + j, WallOrientation.Vertical));        // lado oeste
                walls.Add(new WallKey(rx + rw, rz + j, WallOrientation.Vertical));   // lado este
            }
        }

        // Quita la pared de una puerta. Usa DoorWallKey para no duplicar el mapeo.
        private static bool RemoveDoorWall(HashSet<WallKey> walls, int x, int z, Direction dir)
        {
            return walls.Remove(DoorWallKey(x, z, dir));
        }

        // WallKey de una puerta: (x,z) es el punto exacto de la pared; la dirección
        // solo define la orientación (Horizontal/Vertical), no modifica coordenadas.
        private static WallKey DoorWallKey(int x, int z, Direction dir)
        {
            switch (dir)
            {
                case Direction.South: return new WallKey(x, z, WallOrientation.Horizontal);
                case Direction.North: return new WallKey(x, z, WallOrientation.Horizontal);
                case Direction.West:  return new WallKey(x, z, WallOrientation.Vertical);
                case Direction.East:  return new WallKey(x, z, WallOrientation.Vertical);
                default: return default;
            }
        }

        // Convierte una room local de depto a coords de piso (aplica origen y flipZ).
        private static (int x, int z, int width, int depth) RoomToFloor(ApartmentData apt, RoomData room)
        {
            int z = apt.flipZ ? apt.depth - room.z - room.depth : room.z;
            return (apt.x + room.x, apt.z + z, room.width, room.depth);
        }

        // Convierte una puerta local de depto a coords de piso (aplica origen y flipZ).
        // door.z es la coordenada de la pared (espejo de pared: depth - z), no una celda.
        // El flipZ invierte Norte<->Sur (para el lado de apertura de la hoja).
        private static (int x, int z, Direction direction) DoorToFloor(ApartmentData apt, DoorData door)
        {
            int z = apt.flipZ ? apt.depth - door.z : door.z;
            Direction dir = door.direction;
            if (apt.flipZ)
            {
                if (dir == Direction.North) dir = Direction.South;
                else if (dir == Direction.South) dir = Direction.North;
            }
            return (apt.x + door.x, apt.z + z, dir);
        }

        // Construye las hojas de puerta. Solo la de entrada (Corridor -> Hallway) lleva hoja;
        // las interiores quedan como huecos pelados (sin marco, sin hoja, sin dintel).
        private static void BuildDoors(FloorData floor, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            foreach (var apt in floor.apartments)
            {
                foreach (var door in apt.doors)
                {
                    if (door.from != RoomType.Corridor) continue; // solo la entrada.

                    var d = DoorToFloor(apt, door);
                    BuildDoorLeaf(parent, d.x, d.z, d.direction, settings, materials);
                }
            }
        }

        // Hoja de la puerta de entrada: un solo cubo fino. El pivote efectivo queda en la jamba
        // (se calcula la posición del centro para que el borde caiga en el vano) y se rota
        // doorOpenAngle sobre Y para quedar abierta contra la pared.
        private static void BuildDoorLeaf(Transform parent, int x, int z, Direction dir, BuildingBuilderSettings settings, Dictionary<Color, Material> materials)
        {
            // Determinar la pared: (x,z) ya es el punto de la pared; la dirección solo
            // define la orientación (Horizontal/Vertical).
            WallOrientation orientation;
            switch (dir)
            {
                case Direction.North:
                case Direction.South: orientation = WallOrientation.Horizontal; break;
                case Direction.West:
                case Direction.East:  orientation = WallOrientation.Vertical; break;
                default: return;
            }
            int wallX = x;
            int wallZ = z;

            float w = settings.doorWidth;
            float h = settings.doorHeight;
            float t = settings.doorThickness;

            // Abre hacia afuera (el corredor): Sur/Este -> +ángulo, Norte/Oeste -> -ángulo.
            float sign = (dir == Direction.South || dir == Direction.East) ? 1f : -1f;
            float angle = sign * settings.doorOpenAngle;
            float rad = angle * Mathf.Deg2Rad;

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = $"Door_Entry_{orientation}_{wallX}_{wallZ}";
            leaf.transform.SetParent(parent, false);

            float posX, posZ;
            if (orientation == WallOrientation.Horizontal)
            {
                // Pared horizontal (corre en X). Hoja ancha en X, pivote en la jamba Oeste.
                // Centro = jamba + (rotación del medio ancho en X).
                leaf.transform.localScale = new Vector3(w, h, t);
                posX = wallX + w / 2f * Mathf.Cos(rad);
                posZ = wallZ - w / 2f * Mathf.Sin(rad);
            }
            else
            {
                // Pared vertical (corre en Z). Hoja ancha en Z, pivote en la jamba Sur.
                leaf.transform.localScale = new Vector3(t, h, w);
                posX = wallX + w / 2f * Mathf.Sin(rad);
                posZ = wallZ + w / 2f * Mathf.Cos(rad);
            }

            leaf.transform.localPosition = new Vector3(posX, h / 2f, posZ);
            leaf.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

            SetMaterial(leaf, settings.wallColor, materials);
        }

        // Crea un cubo, lo posiciona/rota y le asigna material (helper para las escaleras).
        private static GameObject CreateCube(string name, Transform parent, Vector3 scale, Vector3 position, Quaternion rotation, Color color, Dictionary<Color, Material> materials)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            SetMaterial(go, color, materials);
            return go;
        }

        // Construye las escaleras en U dentro de los cores (conecta current con next).
        private static void BuildStairs(FloorData current, FloorData next, BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            float rise = next.elevation - current.elevation;      // floorHeight (3m).
            float flightRise = rise / 2f;                          // 1.5m por tramo.
            float flightRun = 3f;                                  // avance en Z por tramo (core 4x4, 1m de descanso).
            float angle = Mathf.Atan2(flightRise, flightRun) * Mathf.Rad2Deg; // ≈ 26.57°.
            float rampLength = Mathf.Sqrt(flightRise * flightRise + flightRun * flightRun); // ≈ 3.354m.
            int stepsPerFlight = Mathf.RoundToInt(flightRise / settings.stepRise); // 10.

            foreach (var room in current.commonRooms)
            {
                if (room.type != RoomType.Core) continue;
                string coreSide = room.x < current.width / 2 ? "West" : "East";
                BuildCoreStairs(room.x, room.z, coreSide, current.index, stepsPerFlight, flightRise, flightRun, angle, rampLength, settings, parent, materials);
            }
        }

        // Construye la escalera en U de un solo core (2 tramos + descanso + 2 rampas).
        private static void BuildCoreStairs(int coreX, int coreZ, string coreSide, int fromFloorIndex,
            int stepsPerFlight, float flightRise, float flightRun, float angle, float rampLength,
            BuildingBuilderSettings settings, Transform parent, Dictionary<Color, Material> materials)
        {
            float w = settings.stairWidth;
            float sr = settings.stepRise;
            float run = settings.stepRun;

            // Tramo 1 (pared oeste): x∈[coreX, coreX+w], z∈[coreZ, coreZ+flightRun], Y 0→flightRise.
            for (int n = 0; n < stepsPerFlight; n++)
            {
                var step = CreateCube(
                    $"Stairs_{coreSide}_Floor_{fromFloorIndex}_Step_{n}",
                    parent,
                    new Vector3(w, sr, run),
                    new Vector3(coreX + w / 2f, (n + 0.5f) * sr, coreZ + (n + 0.5f) * run),
                    Quaternion.identity, settings.stairColor, materials);
                step.GetComponent<Collider>().enabled = false; // peldaño visual sin collider.
            }

            // Descanso (ancho completo): x∈[coreX, coreX+4], z∈[coreZ+flightRun, coreZ+4], Y=flightRise.
            // Ancho completo para que el giro en U conecte el tramo 1 (oeste) con el tramo 2 (este).
            CreateCube(
                $"Stairs_{coreSide}_Floor_{fromFloorIndex}_Landing",
                parent,
                new Vector3(4f, sr, 1f),
                new Vector3(coreX + 2f, flightRise - sr / 2f, coreZ + flightRun + 0.5f),
                Quaternion.identity, settings.stairColor, materials);

            // Tramo 2 (pared este): x∈[coreX+2.5, coreX+4], z∈[coreZ+1, coreZ+4], Y flightRise→2*flightRise.
            for (int n = 0; n < stepsPerFlight; n++)
            {
                var step = CreateCube(
                    $"Stairs_{coreSide}_Floor_{fromFloorIndex}_Step_{n + stepsPerFlight}",
                    parent,
                    new Vector3(w, sr, run),
                    new Vector3(coreX + 2.5f + w / 2f, flightRise + (n + 0.5f) * sr, coreZ + 4f - (n + 0.5f) * run),
                    Quaternion.identity, settings.stairColor, materials);
                step.GetComponent<Collider>().enabled = false;
            }

            // Rampa 1 (tramo 1): collider inclinado, sin renderer.
            var ramp1 = CreateCube(
                $"StairsRamp_{coreSide}_Floor_{fromFloorIndex}_Seg_1",
                parent,
                new Vector3(w, settings.rampThickness, rampLength),
                new Vector3(coreX + w / 2f, flightRise / 2f, coreZ + flightRun / 2f),
                Quaternion.Euler(-angle, 0f, 0f), settings.stairColor, materials);
            ramp1.GetComponent<Renderer>().enabled = false;

            // Rampa 2 (tramo 2): collider inclinado, sin renderer.
            var ramp2 = CreateCube(
                $"StairsRamp_{coreSide}_Floor_{fromFloorIndex}_Seg_2",
                parent,
                new Vector3(w, settings.rampThickness, rampLength),
                new Vector3(coreX + 2.5f + w / 2f, flightRise + flightRise / 2f, coreZ + 2.5f),
                Quaternion.Euler(angle, 0f, 0f), settings.stairColor, materials);
            ramp2.GetComponent<Renderer>().enabled = false;
        }

        private static void SetMaterial(GameObject go, Color color, Dictionary<Color, Material> materials)
        {
            if (!materials.TryGetValue(color, out var mat))
            {
                mat = new Material(Shader.Find("Standard")) { color = color };
                materials[color] = mat;
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}

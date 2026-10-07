using System;

namespace NewUnityProject.World
{
    // Orientación de una pared en la grilla 2D del piso.
    public enum WallOrientation
    {
        Horizontal, // corre a lo largo de X (pared norte/sur de una celda).
        Vertical    // corre a lo largo de Z (pared este/oeste de una celda).
    }

    // Clave canónica de una pared: un segmento entre dos celdas adyacentes.
    // Se usa en un HashSet para deduplicar paredes compartidas entre rooms.
    public struct WallKey : IEquatable<WallKey>
    {
        public int x;
        public int z;
        public WallOrientation orientation;

        public WallKey(int x, int z, WallOrientation orientation)
        {
            this.x = x;
            this.z = z;
            this.orientation = orientation;
        }

        public bool Equals(WallKey other)
        {
            return x == other.x && z == other.z && orientation == other.orientation;
        }

        public override bool Equals(object obj)
        {
            return obj is WallKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + z;
                hash = hash * 31 + (int)orientation;
                return hash;
            }
        }

        public static bool operator ==(WallKey a, WallKey b) => a.Equals(b);
        public static bool operator !=(WallKey a, WallKey b) => !a.Equals(b);
    }
}

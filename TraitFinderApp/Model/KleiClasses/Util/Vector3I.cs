using System;
using System.Xml.Serialization;

namespace TraitFinderApp.Model.KleiClasses.Util
{
	public struct Vector3I
	{
		public int x;

		public int y;

		public int z;

		public int sqrMagnitude => (int)Math.Floor(Math.Pow(x, 2f) + Math.Pow(y, 2f) + Math.Pow(z, 2f));

		public int magnitude => (int)Math.Floor(Math.Sqrt(sqrMagnitude));

		public Vector3I normalized => this / magnitude;

		public Vector3I(int a, int b, int c)
		{
			x = a;
			y = b;
			z = c;
		}

		public static Vector3I operator +(Vector3I u, Vector3I v)
		{
			return new Vector3I(u.x + v.x, u.y + v.y, u.z + v.z);
		}

		public static Vector3I operator -(Vector3I u, Vector3I v)
		{
			return new Vector3I(u.x - v.x, u.y - v.y, u.z - v.z);
		}

		public static Vector3I operator *(Vector3I u, Vector3I v)
		{
			return new Vector3I(u.x * v.x, u.y * v.y, u.z * v.z);
		}

		public static Vector3I operator /(Vector3I u, Vector3I v)
		{
			return new Vector3I(u.x / v.x, u.y / v.y, u.z / v.z);
		}

		public static Vector3I operator *(Vector3I v, int s)
		{
			return new Vector3I(v.x * s, v.y * s, v.z * s);
		}

		public static Vector3I operator /(Vector3I v, int s)
		{
			return new Vector3I(v.x / s, v.y / s, v.z / s);
		}

		public static Vector3I operator +(Vector3I u, int scalar)
		{
			return new Vector3I(u.x + scalar, u.y + scalar, u.z + scalar);
		}

		public static Vector3I operator -(Vector3I u, int scalar)
		{
			return new Vector3I(u.x - scalar, u.y - scalar, u.z - scalar);
		}

		public static bool operator ==(Vector3I v1, Vector3I v2)
		{
			if (v1.x == v2.x && v1.y == v2.y)
			{
				return v1.z == v2.z;
			}

			return false;
		}

		public static bool operator !=(Vector3I v1, Vector3I v2)
		{
			return !(v1 == v2);
		}

		public override bool Equals(object o)
		{
			return base.Equals(o);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return $"{x}, {y}, {z}";
		}
	}
}

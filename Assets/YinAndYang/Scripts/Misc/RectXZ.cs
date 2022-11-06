
using UnityEngine;

public struct RectXZ
{
    public VectorXZ position;
    public VectorXZ size;

    public float x
    {
        get
        {
            return position.x;
        }
    }
    public float z
    {
        get
        {
            return position.z;
        }
    }

    public float width
    {
        get
        {
            return size.x;
        }
    }
    public float depth
    {
        get
        {
            return size.z;
        }
    }

    public VectorXZ center
    {
        get
        {
            return position + size / 2f;
        }
        set
        {
            position = value - size / 2f;
        }
    }

    public RectXZ(VectorXZ pos, VectorXZ size)
    {
        this.position = pos;
        this.size = size;
    }

    public RectXZ(float x, float z, float width, float depth) : this(new VectorXZ(x, z), new VectorXZ(width, depth))
    {
    }

    public VectorXZ ClosestPoint(VectorXZ pos)
    {
        return VectorXZ.zero; //TODO
    }

    public override string ToString()
    {
        return $"pos:{position}, size:{size}"; 
    }
}

public struct VectorXZ
{
    public static readonly VectorXZ zero = new VectorXZ(0f, 0f);
    public static readonly VectorXZ one = new VectorXZ(1f, 1f);

    public float x;
    public float z;
    public VectorXZ(float x, float z)
    {
        this.x = x;
        this.z = z;
    }

    public static float Distance(VectorXZ a, VectorXZ b)
    {
        var sub = a - b;
        return Mathf.Sqrt(sub.x * sub.x  + sub.z * sub.z);
    }

    public static VectorXZ operator / (VectorXZ vec, float val)
    {
        return new VectorXZ(vec.x / val, vec.z / val);
    }
    public static VectorXZ operator *(VectorXZ vec, float val)
    {
        return new VectorXZ(vec.x * val, vec.z * val);
    }
    public static VectorXZ operator +(VectorXZ vec, float val)
    {
        return new VectorXZ(vec.x + val, vec.z + val);
    }
    public static VectorXZ operator -(VectorXZ vec, float val)
    {
        return new VectorXZ(vec.x - val, vec.z - val);
    }
    public static VectorXZ operator +(VectorXZ a, VectorXZ b)
    {
        return new VectorXZ(a.x + b.x, a.z + b.z);
    }
    public static VectorXZ operator -(VectorXZ a, VectorXZ b)
    {
        return new VectorXZ(a.x - b.x, a.z - b.z);
    }

    public static implicit operator VectorXZ(Vector3 v)
    {
        return new VectorXZ(v.x, v.z);
    }

    public static implicit operator Vector3(VectorXZ v)
    {
        return new Vector3(v.x, 0, v.z);
    }
    public override string ToString()
    {
        return $"x:{x:+.00;-.00}, z:{x:+.00;-.00}";
    }
}

public struct VectorXZInt
{
    public static readonly VectorXZInt zero = new VectorXZInt(0, 0);
    public static readonly VectorXZInt one = new VectorXZInt(1, 1);

    public int x;
    public int z;
    public VectorXZInt(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static float Distance(VectorXZInt a, VectorXZInt b)
    {
        var sub = a - b;
        return Mathf.Sqrt(sub.x * sub.x + sub.z * sub.z);
    }

    public static VectorXZInt operator /(VectorXZInt vec, int val)
    {
        return new VectorXZInt(vec.x / val, vec.z / val);
    }
    public static VectorXZInt operator *(VectorXZInt vec, int val)
    {
        return new VectorXZInt(vec.x * val, vec.z * val);
    }
    public static VectorXZInt operator +(VectorXZInt vec, int val)
    {
        return new VectorXZInt(vec.x + val, vec.z + val);
    }
    public static VectorXZInt operator -(VectorXZInt vec, int val)
    {
        return new VectorXZInt(vec.x - val, vec.z - val);
    }
    public static VectorXZInt operator +(VectorXZInt a, VectorXZInt b)
    {
        return new VectorXZInt(a.x + b.x, a.z + b.z);
    }
    public static VectorXZInt operator -(VectorXZInt a, VectorXZInt b)
    {
        return new VectorXZInt(a.x - b.x, a.z - b.z);
    }

    public static implicit operator VectorXZInt(Vector3Int v)
    {
        return new VectorXZInt(v.x, v.z);
    }

    public static implicit operator Vector3Int(VectorXZInt v)
    {
        return new Vector3Int(v.x, 0, v.z);
    }

    public static VectorXZInt FloorToInt(VectorXZ v)
    {
        return new VectorXZInt(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.z));
    }

    public static VectorXZInt Min(VectorXZInt lhs, VectorXZInt rhs)
    {
        return new VectorXZInt(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.z, rhs.z));
    }

    public static VectorXZInt Max(VectorXZInt lhs, VectorXZInt rhs)
    {
        return new VectorXZInt(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.z, rhs.z));
    }
}
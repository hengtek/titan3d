﻿using EngineNS.GamePlay.Camera;
using System;

using System.Globalization;

namespace EngineNS
{
    /// <summary>
    /// 平面对象的结构体
    /// </summary>
    [System.Serializable]
	[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
    //[System.ComponentModel.TypeConverter( typeof(EngineNS.Design.PlaneConverter) )]
	public struct Plane : System.IEquatable<Plane>
    {
        /// <summary>
        /// 平面法向量
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// 偏移值D
        /// </summary>
        public float D;

        public float A { get => Normal.X; }
        public float B { get => Normal.Y; }
        public float C { get => Normal.Z; }


        #region Equal Override
        /// <summary>
        /// 转换成String类型
        /// </summary>
        /// <returns>返回转换成string类型的对象</returns>
        public override string ToString()
	    {
		    return string.Format( CultureInfo.CurrentCulture, "Normal:{0} D:{1}", Normal.ToString(), D.ToString(CultureInfo.CurrentCulture) );
	    }
        /// <summary>
        /// 获取对象的哈希值
        /// </summary>
        /// <returns>返回对象的哈希值</returns>
	    public override int GetHashCode()
	    {
		    return Normal.GetHashCode() + D.GetHashCode();
	    }
        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <param name="value">可以转换成平面对象的对象</param>
        /// <returns>如果两个对象相等返回true，否则返回false</returns>
	    public override bool Equals( object value )
	    {
		    if( value == null )
			    return false;

		    if( value.GetType() != GetType() )
			    return false;

		    return Equals( (Plane)( value ) );
	    }
        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <param name="value">平面对象</param>
        /// <returns>如果两个对象相等返回true，否则返回false</returns>
	    public bool Equals( Plane value )
	    {
		    return ( Normal == value.Normal && D == value.D );
	    }
        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <param name="value1">平面对象</param>
        /// <param name="value2">平面对象</param>
        /// <returns>如果两个对象相等返回true，否则返回false</returns>
	    public static bool Equals( ref Plane value1, ref Plane value2 )
	    {
		    return ( value1.Normal == value2.Normal && value1.D == value2.D );
	    }
        #endregion
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="value">平面对象</param>
        public Plane(Plane value)
        {
            Normal = value.Normal;
            D = value.D;
        }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="a">平面法向量X方向的值</param>
        /// <param name="b">平面法向量Y方向的值</param>
        /// <param name="c">平面法向量Z方向的值</param>
        /// <param name="d">偏移值D</param>
        public Plane( float a, float b, float c, float d )
        {
            Normal.X = a;
            Normal.Y = b;
            Normal.Z = c;
            D = d;
        }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="normal">平面法向量</param>
        /// <param name="d">偏移值D</param>
        public Plane(Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="point">平面通过的点坐标</param>
        /// <param name="normal">平面法向量</param>
        public Plane( Vector3 point, Vector3 normal )
	    {
		    Normal = normal;
		    D = -Vector3.Dot( normal, point );
	    }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="point1">平面通过的点坐标</param>
        /// <param name="point2">平面通过的点坐标</param>
        /// <param name="point3">平面通过的点坐标</param>
        public Plane( Vector3 point1, Vector3 point2, Vector3 point3 )
	    {
		    float x1 = point2.X - point1.X;
		    float y1 = point2.Y - point1.Y;
		    float z1 = point2.Z - point1.Z;
		    float x2 = point3.X - point1.X;
		    float y2 = point3.Y - point1.Y;
		    float z2 = point3.Z - point1.Z;
		    float yz = (y1 * z2) - (z1 * y2);
		    float xz = (z1 * x2) - (x1 * z2);
		    float xy = (x1 * y2) - (y1 * x2);
		    float invPyth = 1.0f / (float)( Math.Sqrt((yz * yz) + (xz * xz) + (xy * xy)) );

		    Normal.X = yz * invPyth;
		    Normal.Y = xz * invPyth;
		    Normal.Z = xy * invPyth;
		    D = -((Normal.X * point1.X) + (Normal.Y * point1.Y) + (Normal.Z * point1.Z));
	    }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="value">4维向量</param>
        public Plane( Vector4 value )
	    {
            Normal.X = value.X;
            Normal.Y = value.Y;
            Normal.Z = value.Z;
            D = value.W;
	    }
        /// <summary>
        /// 向量的点积
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="point">点的4维坐标</param>
        /// <returns>返回计算后的值</returns>
        [Rtti.Meta]
        public static float Dot(Plane plane, Vector4 point )
	    {
		    return (plane.Normal.X * point.X) + (plane.Normal.Y * point.Y) + (plane.Normal.Z * point.Z) + (plane.D * point.W);
        }
        /// <summary>
        /// 平面与向量的点积
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="point">三维向量</param>
        /// <returns>返回计算后的值</returns>
        [Rtti.Meta]
        public static float DotCoordinate(Plane plane, Vector3 point)
        {
            return (plane.Normal.X * point.X) + (plane.Normal.Y * point.Y) + (plane.Normal.Z * point.Z) + plane.D;
        }
        /// <summary>
        /// 平面与向量的点积
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="point">三维向量</param>
        /// <returns>返回计算后的值</returns>
        [Rtti.Meta]
        public static float DotNormal(Plane plane, Vector3 point)
        {
            return (plane.Normal.X * point.X) + (plane.Normal.Y * point.Y) + (plane.Normal.Z * point.Z);
        }
        /// <summary>
        /// 把向量变为单位向量
        /// </summary>
        [Rtti.Meta]
        public void Normalize()
        {
            float magnitude = 1.0f / (float)(Math.Sqrt((Normal.X * Normal.X) + (Normal.Y * Normal.Y) + (Normal.Z * Normal.Z)));

            Normal.X *= magnitude;
            Normal.Y *= magnitude;
            Normal.Z *= magnitude;
            D *= magnitude;
        }
        /// <summary>
        /// 平面对象的单位化
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <returns>返回单位化后的平面</returns>
        [Rtti.Meta]
        public static Plane Normalize( Plane plane )
	    {
		    float magnitude = 1.0f / (float)( Math.Sqrt( (plane.Normal.X * plane.Normal.X) + (plane.Normal.Y * plane.Normal.Y) + (plane.Normal.Z * plane.Normal.Z) ) );

            Plane result;
            result.Normal.X = plane.Normal.X * magnitude;
            result.Normal.Y = plane.Normal.Y * magnitude;
            result.Normal.Z = plane.Normal.Z * magnitude;
            result.D = plane.D * magnitude;
            return result;
	    }
        /// <summary>
        /// 平面对象的单位化
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="result">单位化后的平面</param>
        [Rtti.Meta]
        public static void Normalize( ref Plane plane, out Plane result )
	    {
		    float magnitude = 1.0f / (float)( Math.Sqrt( (plane.Normal.X * plane.Normal.X) + (plane.Normal.Y * plane.Normal.Y) + (plane.Normal.Z * plane.Normal.Z) ) );

            result.Normal.X = plane.Normal.X * magnitude;
            result.Normal.Y = plane.Normal.Y * magnitude;
            result.Normal.Z = plane.Normal.Z * magnitude;
            result.D = plane.D * magnitude;
	    }
        /// <summary>
        /// 平面对象的移动
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="transformation">转换矩阵</param>
        /// <returns>返回转换后的平面</returns>
        [Rtti.Meta]
        public static Plane Transform( Plane plane, Matrix transformation)
	    {
		    Plane result;

            result.Normal.X = transformation.M11 * plane.Normal.X + transformation.M21 * plane.Normal.Y + transformation.M31 * plane.Normal.Z + transformation.M41 * plane.D;
            result.Normal.Y = transformation.M12 * plane.Normal.X + transformation.M22 * plane.Normal.Y + transformation.M32 * plane.Normal.Z + transformation.M42 * plane.D;
            result.Normal.Z = transformation.M13 * plane.Normal.X + transformation.M23 * plane.Normal.Y + transformation.M33 * plane.Normal.Z + transformation.M43 * plane.D;
            result.D = transformation.M14 * plane.Normal.X + transformation.M24 * plane.Normal.Y + transformation.M34 * plane.Normal.Z + transformation.M44 * plane.D;

		    return result;
	    }
        /// <summary>
        /// 平面对象的移动
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="temp">转换矩阵</param>
        /// <param name="result">转换后的平面</param>
        [Rtti.Meta]
        public static void Transform( ref Plane plane, ref Matrix temp, out Plane result )
	    {
		    float x = plane.Normal.X;
		    float y = plane.Normal.Y;
		    float z = plane.Normal.Z;
		    float d = plane.D;

		    Matrix transformation = Matrix.Invert(in temp );

		    Plane r;
		    r.Normal.X = (((x * transformation.M11) + (y * transformation.M12)) + (z * transformation.M13)) + (d * transformation.M14);
		    r.Normal.Y = (((x * transformation.M21) + (y * transformation.M22)) + (z * transformation.M23)) + (d * transformation.M24);
		    r.Normal.Z = (((x * transformation.M31) + (y * transformation.M32)) + (z * transformation.M33)) + (d * transformation.M34);
		    r.D		= (((x * transformation.M41) + (y * transformation.M42)) + (z * transformation.M43)) + (d * transformation.M44);
	
		    result = r;
	    }
        /// <summary>
        /// 平面对象的移动
        /// </summary>
        /// <param name="planes">平面对象列表</param>
        /// <param name="temp">转换矩阵</param>
        /// <returns>返回转换后的平面列表</returns>
	    public static Plane[] Transform( Plane[] planes, ref Matrix temp )
	    {
		    if( planes == null )
			    throw new ArgumentNullException( "planes" );

		    int count = planes.Length;
		    Plane[] results = new Plane[ count ];
		    Matrix transformation = Matrix.Invert(in temp );

		    for( int i = 0; i < count; i++ )
		    {
			    float x = planes[i].Normal.X;
			    float y = planes[i].Normal.Y;
			    float z = planes[i].Normal.Z;
			    float d = planes[i].D;

			    Plane r;
			    r.Normal.X = (((x * transformation.M11) + (y * transformation.M12)) + (z * transformation.M13)) + (d * transformation.M14);
			    r.Normal.Y = (((x * transformation.M21) + (y * transformation.M22)) + (z * transformation.M23)) + (d * transformation.M24);
			    r.Normal.Z = (((x * transformation.M31) + (y * transformation.M32)) + (z * transformation.M33)) + (d * transformation.M34);
			    r.D		= (((x * transformation.M41) + (y * transformation.M42)) + (z * transformation.M43)) + (d * transformation.M44);
		
			    results[i] = r;
		    }

		    return results;
	    }
        /// <summary>
        /// 平面对象的转换
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="rotation">旋转四元数</param>
        /// <returns>返回转换后的平面</returns>
        [Rtti.Meta]
        public static Plane Transform( Plane plane, Quaternion rotation )
	    {
		    Plane result;
		    float x2 = rotation.X + rotation.X;
		    float y2 = rotation.Y + rotation.Y;
		    float z2 = rotation.Z + rotation.Z;
		    float wx = rotation.W * x2;
		    float wy = rotation.W * y2;
		    float wz = rotation.W * z2;
		    float xx = rotation.X * x2;
		    float xy = rotation.X * y2;
		    float xz = rotation.X * z2;
		    float yy = rotation.Y * y2;
		    float yz = rotation.Y * z2;
		    float zz = rotation.Z * z2;

		    float x = plane.Normal.X;
		    float y = plane.Normal.Y;
		    float z = plane.Normal.Z;

		    result.Normal.X = ((x * ((1.0f - yy) - zz)) + (y * (xy - wz))) + (z * (xz + wy));
		    result.Normal.Y = ((x * (xy + wz)) + (y * ((1.0f - xx) - zz))) + (z * (yz - wx));
		    result.Normal.Z = ((x * (xz - wy)) + (y * (yz + wx))) + (z * ((1.0f - xx) - yy));
		    result.D = plane.D;
		    return result;
	    }
        /// <summary>
        /// 平面对象的转换
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="rotation">旋转四元数</param>
        /// <param name="result">转换后的平面</param>
        [Rtti.Meta]
        public static void Transform( ref Plane plane, ref Quaternion rotation, out Plane result )
	    {
		    float x2 = rotation.X + rotation.X;
		    float y2 = rotation.Y + rotation.Y;
		    float z2 = rotation.Z + rotation.Z;
		    float wx = rotation.W * x2;
		    float wy = rotation.W * y2;
		    float wz = rotation.W * z2;
		    float xx = rotation.X * x2;
		    float xy = rotation.X * y2;
		    float xz = rotation.X * z2;
		    float yy = rotation.Y * y2;
		    float yz = rotation.Y * z2;
		    float zz = rotation.Z * z2;

		    float x = plane.Normal.X;
		    float y = plane.Normal.Y;
		    float z = plane.Normal.Z;

		    Plane r;
		    r.Normal.X = ((x * ((1.0f - yy) - zz)) + (y * (xy - wz))) + (z * (xz + wy));
		    r.Normal.Y = ((x * (xy + wz)) + (y * ((1.0f - xx) - zz))) + (z * (yz - wx));
		    r.Normal.Z = ((x * (xz - wy)) + (y * (yz + wx))) + (z * ((1.0f - xx) - yy));
		    r.D = plane.D;

		    result = r;
	    }
        /// <summary>
        /// 平面对象的转换
        /// </summary>
        /// <param name="planes">平面对象列表</param>
        /// <param name="rotation">旋转四元数</param>
        /// <returns>返回转换后的平面列表</returns>
	    public static Plane[] Transform( Plane[] planes, ref Quaternion rotation )
	    {
		    if( planes == null )
			    throw new ArgumentNullException( "planes" );

		    int count = planes.Length;
		    Plane[] results = new Plane[ count ];

		    float x2 = rotation.X + rotation.X;
		    float y2 = rotation.Y + rotation.Y;
		    float z2 = rotation.Z + rotation.Z;
		    float wx = rotation.W * x2;
		    float wy = rotation.W * y2;
		    float wz = rotation.W * z2;
		    float xx = rotation.X * x2;
		    float xy = rotation.X * y2;
		    float xz = rotation.X * z2;
		    float yy = rotation.Y * y2;
		    float yz = rotation.Y * z2;
		    float zz = rotation.Z * z2;

		    for( int i = 0; i < count; i++ )
		    {
			    float x = planes[i].Normal.X;
			    float y = planes[i].Normal.Y;
			    float z = planes[i].Normal.Z;

			    Plane r;
			    r.Normal.X = ((x * ((1.0f - yy) - zz)) + (y * (xy - wz))) + (z * (xz + wy));
			    r.Normal.Y = ((x * (xy + wz)) + (y * ((1.0f - xx) - zz))) + (z * (yz - wx));
			    r.Normal.Z = ((x * (xz - wy)) + (y * (yz + wx))) + (z * ((1.0f - xx) - yy));
			    r.D = planes[i].D;

			    results[i] = r;
		    }

		    return results;
	    }
        /// <summary>
        /// 平面位置判断
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="start">起点坐标</param>
        /// <param name="end">终点坐标</param>
        /// <param name="intersectPoint">交点坐标</param>
        /// <returns>相交返回true，否则返回false</returns>
        [Rtti.Meta]
        public static bool Intersects(in Plane plane, in Vector3 start, in Vector3 end, out Vector3 intersectPoint)
	    {
            unsafe
            {
                fixed (Plane* pPlane = &plane)
                {
                    fixed (Vector3* pStart = &start)
                    {
                        fixed (Vector3* pEnd = &end)
                        {
                            fixed (Vector3* pIntersectPoint = &intersectPoint)
                            {
                                if (0 == (int)IDllImportApi.v3dxPlaneIntersectLine(pIntersectPoint, pPlane, pStart, pEnd))
                                {
                                    return false;
                                }
                                return true;
                            }
                        }
                    }
                }
            }
	    }
        /// <summary>
        /// 平面位置判断
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="box">包围盒</param>
        /// <returns>返回平面与包围盒对象的相对位置类型</returns>
        [Rtti.Meta]
        public static PlaneIntersectionType Intersects(in Plane plane, in BoundingBox box)
        {
            Vector3 min;
            Vector3 max;
            max.X = (plane.Normal.X >= 0.0f) ? box.Minimum.X : box.Maximum.X;
            max.Y = (plane.Normal.Y >= 0.0f) ? box.Minimum.Y : box.Maximum.Y;
            max.Z = (plane.Normal.Z >= 0.0f) ? box.Minimum.Z : box.Maximum.Z;
            min.X = (plane.Normal.X >= 0.0f) ? box.Maximum.X : box.Minimum.X;
            min.Y = (plane.Normal.Y >= 0.0f) ? box.Maximum.Y : box.Minimum.Y;
            min.Z = (plane.Normal.Z >= 0.0f) ? box.Maximum.Z : box.Minimum.Z;

            float dot = (plane.Normal.X * max.X) + (plane.Normal.Y * max.Y) + (plane.Normal.Z * max.Z);

            if (dot + plane.D > 0.0f)
                return PlaneIntersectionType.Front;

            dot = (plane.Normal.X * min.X) + (plane.Normal.Y * min.Y) + (plane.Normal.Z * min.Z);

            if (dot + plane.D < 0.0f)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }
        public static unsafe bool Intersects(Plane* planes, int num, in BoundingBox box)
        {
            for (int i = 0; i < num; i++)
            {
                if (Intersects(in planes[i], in box) == PlaneIntersectionType.Back)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 平面位置判断
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="sphere">球体包围盒</param>
        /// <returns>返回平面与包围盒对象的相对位置类型</returns>
        [Rtti.Meta]
        public static PlaneIntersectionType Intersects(Plane plane, BoundingSphere sphere)
        {
            float dot = (sphere.Center.X * plane.Normal.X) + (sphere.Center.Y * plane.Normal.Y) + (sphere.Center.Z * plane.Normal.Z) + plane.D;

            if (dot > sphere.Radius)
                return PlaneIntersectionType.Front;

            if (dot < -sphere.Radius)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }
        /// <summary>
        /// 平面对象的缩放操作
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="scale">缩放值</param>
        /// <returns>返回转换后的平面对象</returns>
        [Rtti.Meta]
        public static Plane Multiply( Plane plane, float scale )
	    {
		    Plane result;
            unsafe
            {
                IDllImportApi.v3dxPlaneScale((Plane*)&result, (Plane*)&plane, scale);
            }
            
		    return result;
	    }
        /// <summary>
        /// 平面对象的缩放操作
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="scale">缩放值</param>
        /// <param name="result">转换后的平面对象</param>
        [Rtti.Meta]
        public static void Multiply( ref Plane plane, float scale, out Plane result )
	    {
            unsafe
            {
                fixed (Plane* pResult = &result)
                {
                    fixed (Plane* pPlane = &plane)
                    {
                        IDllImportApi.v3dxPlaneScale(pResult, pPlane, scale);
                    }
                }
            }
        }
        /// <summary>
        /// 重载"*"号操作符
        /// </summary>
        /// <param name="plane">平面对象</param>
        /// <param name="scale">缩放值</param>
        /// <returns>返回计算后的平面对象</returns>
        public static Plane operator * ( Plane plane, float scale )
	    {
		    Plane result;
            unsafe
            {
                IDllImportApi.v3dxPlaneScale((Plane*)(&result), (Plane*)(&plane), scale);
            }
		    return result;
	    }
        /// <summary>
        /// 重载"*"号操作符
        /// </summary>
        /// <param name="scale">缩放值</param>
        /// <param name="plane">平面对象</param>
        /// <returns>返回计算后的平面对象</returns>
	    public static Plane operator * ( float scale, Plane plane )
	    {
		    return plane * scale;
	    }
        /// <summary>
        /// 重载"=="号操作符
        /// </summary>
        /// <param name="left">平面对象</param>
        /// <param name="right">平面对象</param>
        /// <returns>如果两个对象相等返回true，否则返回false</returns>
	    public static bool operator == ( Plane left, Plane right )
	    {
            return left.Equals(right);
		    //return Equals( left, right );
	    }
        /// <summary>
        /// 重载"!="号操作符
        /// </summary>
        /// <param name="left">平面对象</param>
        /// <param name="right">平面对象</param>
        /// <returns>如果两个对象不相等返回true，否则返回false</returns>
        public static bool operator != ( Plane left, Plane right )
	    {
            return !left.Equals(right);
		    //return !Equals( left, right );
	    }

        public static unsafe bool PickPlanePos(
            in Vector3 pickRay,
            in Vector3 cameraLocalPosition,
            in DVector3 matrixStartPosition,
            int x, int y, in DVector3 planePos, 
            in EngineNS.Vector3 planeNormal, 
            out DVector3 resultPos)
        {
            resultPos = DVector3.Zero;
            var localPlanePos = planePos - matrixStartPosition;
            var plane = new Plane();
            plane.Normal = planeNormal;
            plane.D = (float)-DVector3.Dot(localPlanePos, planeNormal);
            var end = cameraLocalPosition + pickRay * 10000;
            unsafe
            {
                fixed(Vector3* clpPtr = &cameraLocalPosition)
                //(Plane* pPlane = &plane)
                {
                    Vector3 hitPos;
                    if ((IntPtr)IDllImportApi.v3dxPlaneIntersectLine(&hitPos, &plane, clpPtr, &end) == IntPtr.Zero)
                        return false;
                    resultPos = hitPos.AsDVector() + matrixStartPosition;
                }
            }

            return true;
        }
    }
}

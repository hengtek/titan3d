/********************************************************************
	V3D					A Powerful 3D Enjine
	File:				v3dxfrustum.cpp
	Created Time:		30:6:2002   16:35
	Modify Time:
	Original Author:	< johnson >
	More Author:	
	Abstract:			?
	
	Note:				

*********************************************************************/
#include "v3dxFrustum.h"
#include "v3dxPoly3.h"
#include "../Base/vfxsampcounter.h"

//#define new VNEW
#if !defined(PLATFORM_WIN)
#pragma GCC diagnostic ignored "-Wunused-function"
#endif

v3dxVector3	g_StandFrustum[8] =
{
	v3dxVector3(-1.0f, -1.0f,  0.0f), // xyz
	v3dxVector3( 1.0f, -1.0f,  0.0f), // Xyz
	v3dxVector3(-1.0f,  1.0f,  0.0f), // xYz
	v3dxVector3( 1.0f,  1.0f,  0.0f), // XYz
	v3dxVector3(-1.0f, -1.0f,  1.0f), // xyZ
	v3dxVector3( 1.0f, -1.0f,  1.0f), // XyZ
	v3dxVector3(-1.0f,  1.0f,  1.0f), // xYZ
	v3dxVector3( 1.0f,  1.0f,  1.0f), // XYZ
};

/*
corner arrange
	3----2
	|    |
	0----1
	number = 4
*/
/*HRESULT v3dxFrustum::build(const v3dxVector3* pvTipPt,const v3dxVector3 avDir[ENUM_FRUSTUMCN_NUMBER],
					   const v3dxVector3* pvDirection,FLOAT fHeight)
{
	m_vTipPt = *pvTipPt;

	v3dxVector3 vNormal;
	v3dxVec3Cross( &vNormal , &avDir[ENUM_FRUSTUMCN_RIGHTTOP] , &avDir[ENUM_FRUSTUMCN_LEFTTOP] );
	m_aPlane[ENUM_FRUSTUMPL_TOP].set( &vNormal , pvTipPt );
	v3dxVec3Cross( &vNormal , &avDir[ENUM_FRUSTUMCN_RIGHTBOTTOM] , &avDir[ENUM_FRUSTUMCN_RIGHTTOP] );
	m_aPlane[ENUM_FRUSTUMPL_RIGHT].set( &vNormal , pvTipPt );
	v3dxVec3Cross( &vNormal , &avDir[ENUM_FRUSTUMCN_LEFTBOTTOM] , &avDir[ENUM_FRUSTUMCN_RIGHTBOTTOM] );
	m_aPlane[ENUM_FRUSTUMPL_BOTTOM].set( &vNormal , pvTipPt );
	v3dxVec3Cross( &vNormal , &avDir[ENUM_FRUSTUMCN_LEFTTOP] , &avDir[ENUM_FRUSTUMCN_LEFTBOTTOM] );
	m_aPlane[ENUM_FRUSTUMPL_LEFT].set( &vNormal , pvTipPt );

	if( pvDirection )
		vNormal = *pvDirection;
	else
		vNormal = (avDir[0] + avDir[1] + avDir[2] + avDir[3])*0.25f;
	auto upt = (*pvTipPt + (*pvDirection) * fHeight);
	m_aPlane[ENUM_FRUSTUMPL_FAR].set( &vNormal , &upt);

	return S_OK;
}*/

void v3dxFrustum::buildFrustum( const v3dxVector3* pvTipPt , 
				  const v3dxVector3* pv1,
				  const v3dxVector3* pv2,
				  const v3dxVector3* pv3,
				  const v3dxVector3* pv4,
				  const v3dxPlane3* pNearPlane,
				  const v3dxPlane3* pFarPlane )
{
	m_vTipPt = *pvTipPt;

	m_aPlane[ENUM_FRUSTUMPL_TOP].set( *pvTipPt , *pv1 , *pv2 );
	m_aPlane[ENUM_FRUSTUMPL_RIGHT].set( *pvTipPt , *pv2 , *pv3 );
	m_aPlane[ENUM_FRUSTUMPL_BOTTOM].set( *pvTipPt , *pv3 , *pv4 );
	m_aPlane[ENUM_FRUSTUMPL_LEFT].set( *pvTipPt , *pv4 , *pv1 );

	m_aPlane[ENUM_FRUSTUMPL_FAR] = *pFarPlane;

	m_aPlane[ENUM_FRUSTUMPL_NEAR] = *pNearPlane;
}

/*
6-----7
|\   /|
| 2-3 |
| |+| |
| 0-1 |
|/   \|
4-----5
*/
 void v3dxFrustum::buildFrustum(const v3dVector3_t vecFrustum[8])
{
	// Corner
	for (INT i = 0; i < 8; i++)
	{
		m_vecFrustum[i] = vecFrustum[i];
	}

	m_aPlane[ENUM_FRUSTUMPL_NEAR].set(vecFrustum[1], vecFrustum[0], vecFrustum[2]); // Near
	m_aPlane[ENUM_FRUSTUMPL_FAR].set( vecFrustum[7], vecFrustum[6], vecFrustum[5] ); // Far
	m_aPlane[ENUM_FRUSTUMPL_LEFT].set( vecFrustum[6], vecFrustum[2], vecFrustum[4] ); // Left
	m_aPlane[ENUM_FRUSTUMPL_RIGHT].set( vecFrustum[3], vecFrustum[7], vecFrustum[5] ); // Right
	m_aPlane[ENUM_FRUSTUMPL_TOP].set( vecFrustum[3], vecFrustum[2], vecFrustum[6] ); // Top
	m_aPlane[ENUM_FRUSTUMPL_BOTTOM].set( vecFrustum[0], vecFrustum[1], vecFrustum[4] ); // Bottom
}
/*
6-----7
|\   /|
| 2-3 |
| |+| |
| 0-1 |
|/   \|
4-----5
*/
void v3dxFrustum::BuildFrustum(const v3dxVector3* vecPoints)
{
	for (INT i = 0; i < 8; i++)
	{
		m_vecFrustum[i] = vecPoints[i];
	}
	m_aPlane[ENUM_FRUSTUMPL_NEAR].set(m_vecFrustum[1], m_vecFrustum[0], m_vecFrustum[2]); // Near
	m_aPlane[ENUM_FRUSTUMPL_FAR].set(m_vecFrustum[7], m_vecFrustum[6], m_vecFrustum[5]); // Far
	m_aPlane[ENUM_FRUSTUMPL_LEFT].set(m_vecFrustum[6], m_vecFrustum[2], m_vecFrustum[4]); // Left
	m_aPlane[ENUM_FRUSTUMPL_RIGHT].set(m_vecFrustum[3], m_vecFrustum[7], m_vecFrustum[5]); // Right
	m_aPlane[ENUM_FRUSTUMPL_TOP].set(m_vecFrustum[3], m_vecFrustum[2], m_vecFrustum[6]); // Top
	m_aPlane[ENUM_FRUSTUMPL_BOTTOM].set(m_vecFrustum[0], m_vecFrustum[1], m_vecFrustum[4]); // Bottom
}

void v3dxFrustum::BuildFrustum(const v3dxMatrix4* InvViewProj)
{
	// Corner
	for( INT i = 0; i < 8; i++ )
	{
		m_vecFrustum[i] = g_StandFrustum[i];
		//::D3DXVec3TransformCoord( &vecFrustum[i], &vecFrustum[i], &mat );
		::v3dxVec3TransformCoord( &m_vecFrustum[i], &m_vecFrustum[i], InvViewProj );
	}

	m_aPlane[ENUM_FRUSTUMPL_NEAR].set(m_vecFrustum[0], m_vecFrustum[1], m_vecFrustum[2] ); // Near
	m_aPlane[ENUM_FRUSTUMPL_FAR].set(m_vecFrustum[6], m_vecFrustum[7], m_vecFrustum[5] ); // Far
	m_aPlane[ENUM_FRUSTUMPL_LEFT].set(m_vecFrustum[2], m_vecFrustum[6], m_vecFrustum[4] ); // Left
	m_aPlane[ENUM_FRUSTUMPL_RIGHT].set(m_vecFrustum[7], m_vecFrustum[3], m_vecFrustum[5] ); // Right
	m_aPlane[ENUM_FRUSTUMPL_TOP].set(m_vecFrustum[2], m_vecFrustum[3], m_vecFrustum[6] ); // Top
	m_aPlane[ENUM_FRUSTUMPL_BOTTOM].set(m_vecFrustum[1], m_vecFrustum[0], m_vecFrustum[4] ); // Bottom
}


//------------------------------------------------------------------------
//enum	BoxFrustumCheckResult
//{
//	COMPLETE_OUT = 0,
//	INTERSECT    = 1,
//	COMPLETE_IN  = 2,
//};
#define V_IN 1
#define V_OUT 2
#define V_INTERSECT 3
// 
inline bool PointInBox(float x,float y, float z, float xMin, float yMin, float zMin, float xMax, float yMax, float zMax)
{
	if (x > xMin && x < xMax
		&& y > yMin && y < yMax
		&& z > zMin && z < zMax
		)
	{
		return true;
	}
	return false;
}
//------------------------------------------------------------------------
int v3dxFrustum::_checkBeContained(const v3dxBox3* box)
{
	float x = box->minbox.X;
	float y = box->minbox.Y;
	float z = box->minbox.Z;
	float x2 = box->maxbox.X;
	float y2 = box->maxbox.Y;
	float z2 = box->maxbox.Z;

	int hitcount = 0;
	for (int i = 0 ; i < 8; ++i)
		if ( PointInBox(m_vecFrustum[i].X, m_vecFrustum[i].Y, m_vecFrustum[i].Z, x,y,z,x2,y2,z2) )
			++hitcount;

	if (hitcount == 8)
		return CONTAIN_TEST_INNER;
	else if (hitcount == 0)
		return CONTAIN_TEST_OUTER;
	return CONTAIN_TEST_REFER;
}

int v3dxFrustum::_checkContain(const v3dxBox3* box) const
{
	float x = box->minbox.X;
	float y = box->minbox.Y;
	float z = box->minbox.Z;
	float x2 = box->maxbox.X;
	float y2 = box->maxbox.Y;
	float z2 = box->maxbox.Z;
	// Detailed explanation on the BSP tutorial 4
	BYTE mode=0;					// set IN and OUT bit to 0
	for(int i = 0; i < 6; i++ )
	{
		mode &= V_OUT;				// clear the IN bit to 0 
		if( m_aPlane[i].A() * x  + m_aPlane[i].B() * y  + m_aPlane[i].C() * z  + m_aPlane[i].D() >= 0 )
			mode |= V_IN;  // set IN bit to 1
		else
			mode |= V_OUT;			// set OUT bit to 1

		// if we found A() vertex IN for THIS plane and A() vertex OUT of ANY plane continue
		// ( we have enough information to say: INTERSECT! IF there is not vertex missing from the FRONT of the remaining planes)
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x2 + m_aPlane[i].B() * y  + m_aPlane[i].C() * z  + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x  + m_aPlane[i].B() * y2 + m_aPlane[i].C() * z  + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x2 + m_aPlane[i].B() * y2 + m_aPlane[i].C() * z  + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x  + m_aPlane[i].B() * y  + m_aPlane[i].C() * z2 + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x2 + m_aPlane[i].B() * y  + m_aPlane[i].C() * z2 + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x  + m_aPlane[i].B() * y2 + m_aPlane[i].C() * z2 + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		if(m_aPlane[i].A() * x2 + m_aPlane[i].B() * y2 + m_aPlane[i].C() * z2 + m_aPlane[i].D() >= 0)
			mode |= V_IN;
		else
			mode |= V_OUT;
		if (mode == V_INTERSECT) continue;

		// if we arrive to this point, then there are two possibilities:
		// there is not vertices in or there is not intersection till know, if 
		// there is A() vertice in, continue (we are not over!) 
		if (mode == V_IN) continue;

		// there is not vertex IN front of this plane, so the box is COMPLETE_OUT
		return CONTAIN_TEST_OUTER;
	}

	// All planes has A() vertex IN FRONT so or the box is intersecting or complete IN
	if (mode == V_INTERSECT)
		return CONTAIN_TEST_REFER;
	else
		return CONTAIN_TEST_INNER;
}

static inline vBOOL __CheckVectorOutSideFrustum(const v3dxPlane3 * p,float x,float y,float z)
{
	vBOOL bOutside = 0;
	for( int i = 0; i < ENUM_FRUSTUMPL_NUMBER; i++ )
	{
		if( p[i].A() * x +
			p[i].B() * y +
			p[i].C() * z +
			p[i].D() > 0)
		{
			bOutside |= (1 << i);
		}
	}

	return bOutside;
}

static inline vBOOL __CheckVectorOutSideFrustum(const v3dxPlane3 * p,float x,float y,float z,const v3dMatrix4_t & m)
{
	float xx = x * m.m11 + y * m.m21 + z * m.m31 + m.m41;
	float yy = x * m.m12 + y * m.m22 + z * m.m32 + m.m42;
	float zz = x * m.m13 + y * m.m23 + z * m.m33 + m.m43;

	vBOOL bOutside = 0;
	for( int i = 0; i < ENUM_FRUSTUMPL_NUMBER; i++ )
	{
		if( p[i].A() * xx +
			p[i].B() * yy +
			p[i].C() * zz +
			p[i].D() > 0)
		{
			bOutside |= (1 << i);
		}
	}

	return bOutside;
}

#define CheckVectorOutSideFrustumBoolean(x,y,z)	\
	bOutside[iPoint] = __CheckVectorOutSideFrustum(m_aPlane,x,y,z);\
	if( bOutside[iPoint] == 0 )		return TRUE;\
	iPoint++;
#define CheckVectorOutSideFrustumBooleanMT(x,y,z)	\
	bOutside[iPoint] = __CheckVectorOutSideFrustum(m_aPlane,x,y,z,mat);\
	if( bOutside[iPoint] == 0 )		return TRUE;\
	iPoint++;

vBOOL v3dxFrustum::isContain(const v3dxVector3 & vec) const
{
	return __CheckVectorOutSideFrustum(m_aPlane,vec.X,vec.Y,vec.Z) == 0;
}

vBOOL v3dxFrustum::isContain(const v3dxVector3 & center, float fRadius) const
{
	for (int i = 0; i < ENUM_FRUSTUMPL_NUMBER; i++)
	{
		if (m_aPlane[i].A() * center.X +
			m_aPlane[i].B() * center.Y +
			m_aPlane[i].C() * center.Z +
			m_aPlane[i].D() > fRadius)
		{
			return FALSE;
		}
	}

	return TRUE;
}

vBOOL v3dxFrustum::isContain(const v3dxBox3 & box) const
{
	v3dxVector3 /*_vcMin, */_vcMax;                // _vcMin ==> P , _vcMax ==> Q  
	//bool bOutSide = false;
	for (UINT i = 0; i < 6; i++)
	{
		//if (m_aPlane[i].A() > 0)
		if (m_aPlane[i].A() < 0)
		{
			_vcMax.X = box.maxbox.X;
			//_vcMin.x = box.minbox.x;
		}
		else
		{
			//_vcMin.x = box.maxbox.x;
			_vcMax.X = box.minbox.X;
		}

		//if (m_aPlane[i].B() > 0)
		if (m_aPlane[i].B() < 0)
		{
			_vcMax.Y = box.maxbox.Y;
			//_vcMin.y = box.minbox.y;
		}
		else
		{
			//_vcMin.y = box.maxbox.y;
			_vcMax.Y = box.minbox.Y;
		}

		//if (m_aPlane[i].C() > 0)
		if (m_aPlane[i].C() < 0)
		{
			_vcMax.Z = box.maxbox.Z;
			//_vcMin.z = box.minbox.z;
		}
		else
		{
			//_vcMin.z = box.maxbox.z;
			_vcMax.Z = box.minbox.Z;
		}

		//if (D3DXVec3Dot(&D3DXVECTOR3(m_Plane[i].a, m_Plane[i].b, m_Plane[i].c), &_vcMax) + m_Plane[i].d < 0)
		if(m_aPlane[i].classify(_vcMax)>0)
		{
			return false;
		}
	}

	return true;
}

//int v3dxFrustum::whichContainTypeFast(const v3dxBox3* pBBox) const
//{
//	float f;
//	//int iInner = 0;
//	int iOuter = 0;
//	for (int i = 0; i < ENUM_FRUSTUMPL_NUMBER; i++)
//	{
//		int iPlaneOuts = 0;
//		for (int j = 0; j < BOX3_CORNER_NUMBER; j++)
//		{
//			f = m_aPlane[i].classify(pBBox->GetCorner(j));
//			if (f > 0.f)
//			{
//				iPlaneOuts++;
//				iOuter++;
//			}
//		}
//		if (iPlaneOuts == BOX3_CORNER_NUMBER)
//		{
//			return CONTAIN_TEST_OUTER;
//		}
//	}
//	if (iOuter == 0)
//	{
//		return CONTAIN_TEST_INNER;
//	}
//	else
//	{
//		return CONTAIN_TEST_REFER;
//	}
//}
CONTAIN_TYPE v3dxFrustum::whichContainTypeFast( const v3dxBox3* pBBox, vBOOL testInner) const
{
	//https://www.xuebuyuan.com/2963193.html
	v3dxVector3 _vcMin, _vcMax;                // _vcMin ==> P , _vcMax ==> Q  
	int fullInSide = 0;
	const auto& vcMin = pBBox->minbox;
	const auto& vcMax = pBBox->maxbox;
	for (UINT i = 0; i < 6; i++)
	{
		//auto& pPlane = m_aPlane[i];
		const auto& pPlane = m_aPlane[i];
		if (pPlane.m_vNormal.X > 0)
		{
			_vcMax.X = vcMax.X;
			_vcMin.X = vcMin.X;
		}
		else
		{
			_vcMin.X = vcMax.X;
			_vcMax.X = vcMin.X;
		}

		if (pPlane.m_vNormal.Y > 0)
		{
			_vcMax.Y = vcMax.Y;
			_vcMin.Y = vcMin.Y;
		}
		else
		{
			_vcMin.Y = vcMax.Y;
			_vcMax.Y = vcMin.Y;
		}

		if (pPlane.m_vNormal.Z > 0)
		{
			_vcMax.Z = vcMax.Z;
			_vcMin.Z = vcMin.Z;
		}
		else
		{
			_vcMin.Z = vcMax.Z;
			_vcMax.Z = vcMin.Z;
		}

		float dist = v3dxVec3Dot(&pPlane.m_vNormal, &_vcMin) + pPlane.m_fDD;
		if (dist > 0)
		{
			return CONTAIN_TEST_OUTER;
		}
		if (testInner != FALSE)
		{
			float dist2 = v3dxVec3Dot(&pPlane.m_vNormal, &_vcMax) + pPlane.m_fDD;
			if (dist2 < 0)
			{
				fullInSide++;
			}
		}
	}
	if (fullInSide == 6)
		return CONTAIN_TEST_INNER;
	return CONTAIN_TEST_REFER;
}

CONTAIN_TYPE v3dxFrustum::whichContainType(v3dxVector3* verts, int num, const v3dxVector3* pcenter, vBOOL testInner) const
{
	auto& center = *pcenter;

	int totalNegitive = 0;
	BYTE planes = 0;
	for (int i = 0; i < 6; i++)
	{
		if (m_aPlane[i].classify(center) > 0)
		{
			planes |= (BYTE)(i << i);
			int positive = 0;
			for (int j = 0; j < num; j++)
			{
				if (m_aPlane[i].classify(verts[j]) > 0)
					positive++;
				else
					totalNegitive++;
			}
			if (positive == num)
				return CONTAIN_TEST_OUTER;
		}
	}
	if (planes == 0)
	{
		if (testInner)
		{
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < num; j++)
				{
					if (m_aPlane[i].classify(verts[j]) > 0)
						return CONTAIN_TEST_REFER;
				}
			}
		}
		return CONTAIN_TEST_INNER;
	}
	return CONTAIN_TEST_REFER;

	/*for (UINT i = 0; i < 6; i++)
	{
		if (planes&(1 << i))
			continue;
		int positive = 0;
		for (int j = 0; j<num; j++)
		{
			if (m_aPlane[i].classify(verts[j]) > 0)
				positive++;
			else
				totalNegitive++;
		}
		if (positive == num)
			return CONTAIN_TEST_OUTER;
	}
	if (totalNegitive == 6*num)
		return CONTAIN_TEST_INNER;
	return CONTAIN_TEST_REFER;*/
}

CONTAIN_TYPE v3dxFrustum::whichContainTypeFast( const v3dxPoly3* pPoly , const v3dxMatrix4* pTM ) const
{
	v3dxVector3* vec = new v3dxVector3[ pPoly->getNumVertices() ];
	if( pTM )
	{
		for( int j=0 ; j < pPoly->getNumVertices() ; j++ )
		{
			v3dxVec3TransformCoord( &vec[j] , pPoly->getVertex(j) , pTM );
		}
	}
	else
	{		
		for( int j=0 ; j < pPoly->getNumVertices() ; j++ )
		{
			vec[j] = *pPoly->getVertex(j) ;
		}
	}

	float f;
	//int iInner = 0;
	int iOuter = 0;
	for( int i=0 ; i < ENUM_FRUSTUMPL_NUMBER-2 ; i++ )
	{
		int iPlaneOuts = 0;
		for( int j=0 ; j < pPoly->getNumVertices() ; j++ )
		{
			f = m_aPlane[i].classify( vec[j] );
			if( f>0.f )
			{
				iPlaneOuts++;
				iOuter++;
			}
		}
		if( iPlaneOuts==pPoly->getNumVertices() )
		{
			delete[] vec;
			return CONTAIN_TEST_OUTER;
		}
	}
	if( iOuter==0 )
	{
		delete[] vec;
		return CONTAIN_TEST_INNER;
	}
	else
	{
		delete[] vec;
		return CONTAIN_TEST_REFER;
	}
}

bool v3dxFrustum::fastTestOBB( const v3dxOBB* pOBB , const v3dxMatrix4* pTM ) const
{
	v3dxVector3 boxCorner[BOX3_CORNER_NUMBER];
	for( int i=0 ; i<BOX3_CORNER_NUMBER ; i++ )
	{
		boxCorner[i] = pOBB->GetCorner(i);
		v3dxVec3TransformCoord( &boxCorner[i] , &boxCorner[i] , pTM );
	}

	float f;
	int iOuter = 0;
	for( int i=0 ; i < ENUM_FRUSTUMPL_NUMBER-2 ; i++ )
	{
		int iPlaneOuts = 0;
		for( int j=0 ; j < BOX3_CORNER_NUMBER ; j++ )
		{
			f = m_aPlane[i].classify( boxCorner[j] );
			if( f>0.f )
			{
				iPlaneOuts++;
				iOuter++;
			}
		}
		if( iPlaneOuts==BOX3_CORNER_NUMBER )
		{
			return false;
		}
	}

	return true;
}

bool v3dxFrustum::fastTestOBB2( const v3dxBox3* pBox , const v3dxMatrix4* pTM ) const
{
	v3dxVector3 boxCorner[BOX3_CORNER_NUMBER];
	for( int i=0 ; i<BOX3_CORNER_NUMBER ; i++ )
	{
		boxCorner[i] = pBox->GetCorner(i);
		v3dxVec3TransformCoord( &boxCorner[i] , &boxCorner[i] , pTM );
	}

	float f;
	int iOuter = 0;
	for( int i=0 ; i < ENUM_FRUSTUMPL_NUMBER-2 ; i++ )
	{
		int iPlaneOuts = 0;
		for( int j=0 ; j < BOX3_CORNER_NUMBER ; j++ )
		{
			f = m_aPlane[i].classify( boxCorner[j] );
			if( f>0.f )
			{
				iPlaneOuts++;
				iOuter++;
			}
		}
		if( iPlaneOuts==BOX3_CORNER_NUMBER )
		{
			return false;
		}
	}
	return true;
}

CONTAIN_TYPE v3dxFrustum::whichContainTypeFast(const v3dxBox3* pBBox, const v3dxMatrix4* pTMInverse, vBOOL testInner) const
{
	v3dxPlane3				aPlane[6];
	for (int i = 0; i < 6; i++)
	{
		v3dxPlane3::Transform(aPlane[i], m_aPlane[i], *pTMInverse);
	}

	v3dxVector3 _vcMin, _vcMax;                // _vcMin ==> P , _vcMax ==> Q  
	int fullInSide = 0;
	const auto& vcMin = pBBox->minbox;
	const auto& vcMax = pBBox->maxbox;
	for (UINT i = 0; i < 6; i++)
	{
		//auto& pPlane = m_aPlane[i];
		const auto& pPlane = aPlane[i];
		if (pPlane.m_vNormal.X > 0)
		{
			_vcMax.X = vcMax.X;
			_vcMin.X = vcMin.X;
		}
		else
		{
			_vcMin.X = vcMax.X;
			_vcMax.X = vcMin.X;
		}

		if (pPlane.m_vNormal.Y > 0)
		{
			_vcMax.Y = vcMax.Y;
			_vcMin.Y = vcMin.Y;
		}
		else
		{
			_vcMin.Y = vcMax.Y;
			_vcMax.Y = vcMin.Y;
		}

		if (pPlane.m_vNormal.Z > 0)
		{
			_vcMax.Z = vcMax.Z;
			_vcMin.Z = vcMin.Z;
		}
		else
		{
			_vcMin.Z = vcMax.Z;
			_vcMax.Z = vcMin.Z;
		}

		float dist = v3dxVec3Dot(&pPlane.m_vNormal, &_vcMin) + pPlane.m_fDD;
		if (dist > 0)
		{
			return CONTAIN_TEST_OUTER;
		}
		if (testInner != FALSE)
		{
			float dist2 = v3dxVec3Dot(&pPlane.m_vNormal, &_vcMax) + pPlane.m_fDD;
			if (dist2 < 0)
			{
				fullInSide++;
			}
		}
	}
	if (fullInSide == 6)
		return CONTAIN_TEST_INNER;
	return CONTAIN_TEST_REFER;
}

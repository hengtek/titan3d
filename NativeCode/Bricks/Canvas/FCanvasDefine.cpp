#include "FCanvasDefine.h"
#include "FCanvasBrush.h"
#define new VNEW

NS_BEGIN

v3dxVector2 FUtility::RotateVector(const v3dxVector2& v, float angle)
{
	//https://blog.csdn.net/hjq376247328/article/details/45113563
	float rotMatrix[2][2];
	float cosV = Math::Cos(angle);
	float sinV = Math::Sin(angle);
	rotMatrix[0][0] = cosV, rotMatrix[1][0] = -sinV;
	rotMatrix[0][1] = sinV, rotMatrix[1][1] = cosV;

	v3dxVector2 result;
	result.X = v.X * rotMatrix[0][0] + v.Y * rotMatrix[1][0];
	result.Y = v.X * rotMatrix[0][1] + v.Y * rotMatrix[1][1];
	return result;
}
bool FUtility::LineLineIntersection(const v3dxVector2& ps1, const v3dxVector2& pe1, const v3dxVector2& ps2, const v3dxVector2& pe2, v3dxVector2* pPoint)
{
	// Get A,B of first line - points : ps1 to pe1
	float A1 = pe1.Y - ps1.Y;
	float B1 = ps1.X - pe1.X;
	// Get A,B of second line - points : ps2 to pe2
	float A2 = pe2.Y - ps2.Y;
	float B2 = ps2.X - pe2.X;

	// Get delta and check if the lines are parallel
	float delta = A1 * B2 - A2 * B1;
	if (abs(delta) < 0.0001f)
		return false;

	// Get C of first and second lines
	float C2 = A2 * ps2.X + B2 * ps2.Y;
	float C1 = A1 * ps1.X + B1 * ps1.Y;
	//invert delta to make division cheaper
	float invdelta = 1 / delta;
	// now return the Vector2 intersection point
	pPoint->X = (B2 * C1 - B1 * C2) * invdelta;
	pPoint->Y = (A1 * C2 - A2 * C1) * invdelta;
	return true;
}
bool FUtility::LineRectIntersection(const v3dxVector2& s, const v3dxVector2& e, const FRectanglef& rect, v3dxVector2* pPoint)
{
	v3dxVector2 intersection;
	v3dxVector2 r00 = rect.Get_X0_Y0();
	v3dxVector2 r10 = rect.Get_X1_Y0();
	v3dxVector2 r11 = rect.Get_X1_Y1();
	v3dxVector2 r01 = rect.Get_X0_Y1();
	if (LineLineIntersection(s, e, r00, r10, &intersection))
		return true;
	if (LineLineIntersection(s, e, r00, r01, &intersection))
		return true;
	if (LineLineIntersection(s, e, r01, r11, &intersection))
		return true;
	if (LineLineIntersection(s, e, r11, r01, &intersection))
		return true;
	return false;
}

NS_END
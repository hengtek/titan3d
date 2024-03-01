#include "Utility.h"
#include "FCanvasBrush.h"
NS_BEGIN

namespace Canvas
{
	void FPathUtility::SafeNormalize(const v3dxVector2& In, v3dxVector2& Out, float& Length)
	{
		Length = std::sqrt(DotProduct(In, In));
		if (Length < 1e-6f)
		{
			Out = v3dxVector2::Zero;
			Length = 0.f;
			return;
		}
		Out = In / Length;
	}

	v3dxVector2 FPathUtility::Normalize(const v3dxVector2& In)
	{
		v3dxVector2 Out;
		float Length;
		SafeNormalize(In, Out, Length);
		return Out;
	}

	v3dxVector2 FPathUtility::Lerp(const v3dxVector2& A, const v3dxVector2& B, float Alpha)
	{
		return (1.f - Alpha) * A + Alpha * B;
	}

	v3dxVector2 FPathUtility::CubicInterp(const v3dxVector2& P0, const v3dxVector2& P1, const v3dxVector2& P2, const v3dxVector2& P3, float Alpha)
	{
		const float& t = Alpha;
		const float t2 = t * t;
		const float _t = 1.f - t;
		const float _t2 = _t * _t;
		return _t2 * _t * P0 + 3.f * _t2 * t * P1 + 3.f * _t * t2 * P2 + t2 * t * P3;
	}

	v3dxVector2 FPathUtility::CubicInterpDeriv(const v3dxVector2& P0, const v3dxVector2& P1, const v3dxVector2& P2, const v3dxVector2& P3, float Alpha)
	{
		const float& t = Alpha;
		const float _t = 1.f - t;
		return 3.f * _t * _t * (P1 - P0) + 6.f * _t * t * (P2 - P1) + 3.f * t * t * (P3 - P2);
	}

	float FPathUtility::DotProduct(const v3dxVector2& V0, const v3dxVector2& V1)
	{
		return V0.X * V1.X + V0.Y * V1.Y;
	}

	float FPathUtility::CrossProduct(const v3dxVector2& V0, const v3dxVector2& V1)
	{
		return V0.X * V1.Y - V0.Y * V1.X;
	}

	v3dxVector2 FPathUtility::GetRight(const v3dxVector2& V)
	{
		return v3dxVector2(V.Y, -V.X);
	}

	float FPathUtility::GetDistance(const v3dxVector2& P0, const v3dxVector2& P1)
	{
		const v3dxVector2 V = P1 - P0;
		return std::sqrt(DotProduct(V, V));
	}

	bool FPathUtility::IsInsideTriangle(const v3dxVector2& A, const v3dxVector2& B, const v3dxVector2& C, const v3dxVector2& P)
	{
		v3dxVector2 PA = P - A;
		v3dxVector2 PB = P - B;
		v3dxVector2 PC = P - C;

		float CrossAPB = CrossProduct(PA, PB);
		float CrossBPC = CrossProduct(PB, PC);
		float CrossCPA = CrossProduct(PC, PA);

		return (CrossAPB > 0 && CrossBPC > 0 && CrossCPA > 0)
			|| (CrossAPB < 0 && CrossBPC < 0 && CrossCPA < 0);
	}

	bool FPathUtility::IsInsideRect(const FRectanglef& Rect, const v3dxVector2& P)
	{
		const float MinX = Rect.X;
		const float MaxX = Rect.X + Rect.Width;
		const float MinY = Rect.Y;
		const float MaxY = Rect.Y + Rect.Height;
		return P.X >= MinX && P.X <= MaxX && P.Y >= MinY && P.Y <= MaxY;
	}

	float FPathUtility::GetTriangleArea(const v3dxVector2& A, const v3dxVector2& B, const v3dxVector2& C)
	{
		return CrossProduct(B - A, C - A) * 0.5f;
	}

	FRectanglef FPathUtility::CalcBounds(const void* InVerts, UINT NumVerts)
	{
		if (NumVerts <= 0)
			return FRectanglef(0, 0, 0, 0);
		float MinX = std::numeric_limits<float>::max();
		float MaxX = std::numeric_limits<float>::lowest();
		float MinY = MinX;
		float MaxY = MaxX;
		v3dxVector2* vp = (v3dxVector2*)InVerts;
		for (UINT i = 0; i < NumVerts; i++)
		{
			MinX = std::min(MinX, (*vp).X);
			MaxX = std::max(MaxX, (*vp).X);
			MinY = std::min(MinY, (*vp).Y);
			MaxY = std::max(MaxY, (*vp).Y);
			vp++;
		}
		return FRectanglef(MinX, MinY, MaxX - MinX, MaxY - MinY);
	}

	void FPathUtility::Triangulate(const void* InVerts, UINT NumVerts, std::vector<UINT>& OutInds, UINT IndOffset, bool bConvex)
	{
		if (NumVerts < 3)
			return;
		if (bConvex || NumVerts == 3)
		{
			OutInds.reserve(OutInds.size() + NumVerts * 3);
			for (UINT i = 2; i < NumVerts; i++)
			{
				OutInds.push_back(IndOffset);
				OutInds.push_back(IndOffset + i - 1);
				OutInds.push_back(IndOffset + i);
			}
		}
		else
		{
			OutInds.reserve(OutInds.size() + NumVerts * 3);
			const v3dxVector2* Vertices = (v3dxVector2*)InVerts;
			std::vector<UINT> Candidate;
			for (UINT i = 0; i < NumVerts; i++)
				Candidate.push_back(i);

			UINT CurInd = 0;
			UINT NumCand = (UINT)Candidate.size();
			UINT StepCount = 0;
			while (NumCand > 2)
			{
				UINT NextInd = (CurInd + 1) % NumCand;
				UINT Index0 = Candidate[CurInd];
				UINT Index1 = Candidate[NextInd];
				UINT Index2 = Candidate[(CurInd + 2) % NumCand];
				const v3dxVector2& P0 = Vertices[Index0];
				const v3dxVector2& P1 = Vertices[Index1];
				const v3dxVector2& P2 = Vertices[Index2];

				bool bAnyInTriangle = false;
				for (UINT j = 3; j < NumCand; j++)
				{
					UINT Index = Candidate[(CurInd + j) % NumCand];
					const v3dxVector2& P = Vertices[Index];

					if (FPathUtility::IsInsideTriangle(P0, P1, P2, P))
					{
						bAnyInTriangle = true;
						break;
					}
				}

				if (!bAnyInTriangle && FPathUtility::GetTriangleArea(P0, P1, P2) > 0)
				{
					OutInds.push_back(IndOffset + Index0);
					OutInds.push_back(IndOffset + Index1);
					OutInds.push_back(IndOffset + Index2);
					Candidate.erase(Candidate.begin() + NextInd);
					CurInd = NextInd == 0 ? CurInd - 1 : CurInd;
					StepCount = 0;
					NumCand--;
				}
				else
				{
					CurInd = NextInd;
					StepCount++;
					if (StepCount == NumCand)
						break;
				}
			}
		}
	}

	void FPathUtility::SutherlandHodgmanClip(const FRectanglef& Rect,
		const v3dxVector2* InVerts, UINT NumVerts,
		std::vector<v3dxVector2>& OutVerts,
		const v3dxVector2* InUVs,
		std::vector<v3dxVector2>* OutUVs)
	{
		if (NumVerts <= 0 || !Rect.IsValid())
			return;
		FRectanglef Bounds = CalcBounds(InVerts, NumVerts);
		if (!Rect.IsOverlap(Bounds))
			return;

		const bool bHasUV = InUVs && OutUVs;
		if (Rect.IsContain(Bounds))
		{
			OutVerts.reserve(OutVerts.size() + NumVerts);
			for(UINT i = 0; i < NumVerts; i++)
				OutVerts.push_back(InVerts[i]);
			if (bHasUV)
			{
				(*OutUVs).reserve((*OutUVs).size() + NumVerts);
				for (UINT i = 0; i < NumVerts; i++)
					(*OutUVs).push_back(InUVs[i]);
			}
			return;
		}

		std::vector<v3dxVector2> Vertices(NumVerts);
		std::vector<v3dxVector2> UVs;
		std::memcpy(Vertices.data(), InVerts, sizeof(v3dxVector2) * NumVerts);
		if (bHasUV)
		{
			UVs.resize(NumVerts);
			std::memcpy(UVs.data(), InUVs, sizeof(v3dxVector2) * NumVerts);
		}

		using FVInput = const v3dxVector2&;
		using FIsInFunc = std::function<bool(FVInput)>;
		using FInscFunc = std::function<v3dxVector2(FVInput, FVInput, float&)>;
		auto ClipByLine = [&Vertices, &UVs, Rect, bHasUV](FIsInFunc IsInside, FInscFunc Intersect)
		{
			const UINT NumVerts = (UINT)Vertices.size();
			std::vector<v3dxVector2> NewVerts;
			std::vector<v3dxVector2> NewUVs;

			UINT LastInd = NumVerts - 1;
			bool InClip0 = IsInside(Vertices[LastInd]);
			for (UINT i = 0; i < NumVerts; i++)
			{
				const v3dxVector2& P0 = Vertices[LastInd];
				const v3dxVector2& P1 = Vertices[i];
				bool InClip1 = IsInside(P1);
				if (InClip0)
				{
					NewVerts.push_back(P0);
					if (bHasUV)	
						NewUVs.push_back(UVs[LastInd]);
				}
				if (InClip0 != InClip1)
				{
					float Alpha;
					NewVerts.push_back(Intersect(P0, P1, Alpha));
					if (bHasUV)
						NewUVs.push_back((1 - Alpha) * UVs[LastInd] + Alpha * UVs[i]);
				}
				InClip0 = InClip1;
				LastInd = i;
			}
			Vertices = std::move(NewVerts);
			UVs = std::move(NewUVs);
		};

		if (Bounds.Y < Rect.Y)
		{
			FIsInFunc IsInside = [Rect](FVInput P) { return P.Y >= Rect.Y; };
			FInscFunc Intersect = [Rect](FVInput P0, FVInput P1, float& Alpha)
			{
				Alpha = (Rect.Y - P0.Y) / (P1.Y - P0.Y);
				return v3dxVector2(P0.X + Alpha * (P1.X - P0.X), Rect.Y);
			};
			ClipByLine(IsInside, Intersect);
		}
		if (Bounds.GetRight() > Rect.GetRight())
		{
			FIsInFunc IsInside = [Rect](FVInput P) { return P.X <= Rect.GetRight(); };
			FInscFunc Intersect = [Rect](FVInput P0, FVInput P1, float& Alpha)
			{
				Alpha = (Rect.GetRight() - P0.X) / (P1.X - P0.X);
				return v3dxVector2(Rect.GetRight(), P0.Y + Alpha * (P1.Y - P0.Y));
			};
			ClipByLine(IsInside, Intersect);
		}
		if (Bounds.GetBottom() > Rect.GetBottom())
		{
			FIsInFunc IsInside = [Rect](FVInput P) { return P.Y <= Rect.GetBottom(); };
			FInscFunc Intersect = [Rect](FVInput P0, FVInput P1, float& Alpha)
			{
				Alpha = (Rect.GetBottom() - P0.Y) / (P1.Y - P0.Y);
				return v3dxVector2(P0.X + Alpha * (P1.X - P0.X), Rect.GetBottom());
			};
			ClipByLine(IsInside, Intersect);
		}
		if (Bounds.X < Rect.X)
		{
			FIsInFunc IsInside = [Rect](FVInput P) { return P.X >= Rect.X; };
			FInscFunc Intersect = [Rect](FVInput P0, FVInput P1, float& Alpha)
			{
				Alpha = (Rect.X - P0.X) / (P1.X - P0.X);
				return v3dxVector2(Rect.X, P0.Y + Alpha * (P1.Y - P0.Y));
			};
			ClipByLine(IsInside, Intersect);
		}

		NumVerts = (UINT)Vertices.size();
		if (NumVerts < 3)
			return;

		OutVerts.reserve(OutVerts.size() + NumVerts);
		for (UINT i = 0; i < NumVerts; i++)
			OutVerts.push_back(Vertices[i]);
		if (bHasUV)
		{
			(*OutUVs).reserve((*OutUVs).size() + NumVerts);
			for (UINT i = 0; i < NumVerts; i++)
				(*OutUVs).push_back(UVs[i]);
		}
	}

	FArcLUT GArcLUT;

	FArcLUT::FArcLUT()
	{
		for (UINT i = 0; i < NUM_FASTLUT; i++)
			LUT[i] = v3dxVector2(std::cos(DELTA * i), std::sin(DELTA * i));
	}

	UINT FArcLUT::GetLUTIndex(const v3dxVector2& Orient, bool InCCW)
	{
		if (Orient.X == 1.f)
			return 0;
		if (Orient.X == -1.f)
			return NUM_FASTLUT / 2;

		UINT PrevInd = 0;
		UINT NextInd = NUM_FASTLUT / 2;
		while (NextInd - PrevInd > 1)
		{
			UINT MidInd = (NextInd + PrevInd) / 2;
			if (LUT[MidInd].X <= Orient.X)
				NextInd = MidInd;
			else
				PrevInd = MidInd;
		}
		UINT Index = Orient.Y < 0 ? (PrevInd == 0 ? 0 : NUM_FASTLUT - PrevInd) : NextInd;
		if (!InCCW)
			Index = (Index + NUM_FASTLUT - 1) % NUM_FASTLUT;
		return Index;
	}

	UINT FArcLUT::GetLUTIndex(const v3dxVector2& Orient, bool InCCW, float& OutArcToIndex)
	{
		OutArcToIndex = 0;
		if (Orient.X == 1.f)
			return 0;
		if (Orient.X == -1.f)
			return NUM_FASTLUT / 2;

		UINT PrevInd = 0;
		UINT NextInd = NUM_FASTLUT / 2;
		while (NextInd - PrevInd > 1)
		{
			UINT MidInd = (NextInd + PrevInd) / 2;
			if (LUT[MidInd].X <= Orient.X)
				NextInd = MidInd;
			else
				PrevInd = MidInd;
		}
		UINT Index = Orient.Y < 0 ? (PrevInd == 0 ? 0 : NUM_FASTLUT - PrevInd) : NextInd;
		PrevInd = (Index + NUM_FASTLUT - 1) % NUM_FASTLUT;
		if (!InCCW)
			std::swap(Index, PrevInd);
		float SinToNext = Math::Abs(Orient.Y * LUT[Index].X - Orient.X * LUT[Index].Y);
		float SinToPrev = Math::Abs(Orient.Y * LUT[PrevInd].X - Orient.X * LUT[PrevInd].Y);
		OutArcToIndex = SinToNext / (SinToNext + SinToPrev) * DELTA;
		return Index;
	}
}

NS_END
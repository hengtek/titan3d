//This is a ShaderAsset

void DoUpdateSystem(uint3 id, uint3 GroupId, uint3 GroupThreadId, uint GroupIndex)
{
    //uint oldValue;
    //InterlockedCompareExchange(bfSystemData[0].Flags, 0, 1, oldValue);
    //if (oldValue == 0)
    //{
    //    SpawnParticle((uint3) 0, 512, SetParticleFlags(Nebula_EmitShape, 0), 3.0f);
    //}
    uint oldValue;
    InterlockedAdd(bfSystemData[0].Flags, 1, oldValue);
    [branch]
    if (oldValue < 512)
    {
        SpawnParticle((uint3) 0, 1, SetParticleFlags(Nebula_EmitShape, 0), 3.0f);
    }
}
#define USER_PARTICLE_UPDATESYS

void OnInitParticle(uint3 id, inout FParticle particle)
{
	float4 rdValue = RandomFloat4BySeed2(particle); //RandomFloat4(id.x);
	particle.Life += (rdValue.w + 0.5f)* 0.5f ;

    uint index = GetParticleData(particle.Flags);
	if (IsParticleEmitShape(particle))
    { //Index as EmitShap Type(0 or 1): SetParticleFlags(Nebula_EmitShape, 1) SetParticleFlags(Nebula_EmitShape, 0)
        if (index == 0)
		{
			particle.Scale = 0.5f - rdValue.z * 0.2f;
		}
		else
		{
			particle.Scale = 0.1f - rdValue.z * 0.2f;
		}
	}
	else
    { //Index as EmitIndex: SetParticleFlags(Nebula_EmitIndex, idxInPool)
		particle.Scale = 0.5f - rdValue.z * 0.2f;
        particle.Location = bfParticles[index].Location;
    }
    //particle.Scale = 1.0f;
    particle.Velocity = EmitterData.Velocity + float3(1,3,0);
    particle.Color = ToColorUint(rdValue);

}
#define USER_PARTICLE_INITIALIZE

void OnDeadParticle(uint3 id, uint idxInPool, inout FParticle particle)
{
	if (HasParticleFlags(particle, Nebula_EmitShape))
	{
		uint shapeIndex = GetParticleData(particle.Flags);
		if (shapeIndex == 0)
		{
			SpawnParticle(id, 1, SetParticleFlags(Nebula_EmitShape, 1), 5.0f);
		}
		else
		{
			SpawnParticle(id, 1, SetParticleFlags(Nebula_EmitShape, 0), 3.0f);
		}
	}
    else
    {
        SpawnParticle(id, 1, SetParticleFlags(Nebula_EmitIndex, idxInPool), 3.0f);
    }
}
#define USER_PARTICLE_FINALIZE

void DoOnTimer(uint3 id, uint3 GroupId, uint3 GroupThreadId, uint GroupIndex, float second)
{
    TtReadonlyRawArray CurAlives = GetCurrentAlives();
    uint CountOfAlive = CurAlives.GetCount();
    if (id.x < CountOfAlive)
    {
        //uint index = GetParticleIndexByThreadID(id.x);
        uint index = CurAlives.GetValue(id.x);
        FParticle particle = bfParticles[index];
    }
}
#define USER_PARTICLE_ONTIMER



//This is a ShaderAsset
//void {Your Effector Name}_EffectorExecute(uint3 id, float elapseTime, inout FParticle particle, {Your Effector Name}_EffectorParameters parameter)
#if defined(USE_EFFECTOR_Accelerated)
void Accelerated_EffectorExecute(uint3 id, float elapseTime, inout FParticle particle, Accelerated_EffectorParameters parameter)
{
	//float rdValue = RandomFloatBySeedSignedUnit(particle);// RandomFloat4(id.x);
	//particle.Velocity += (parameter.AccelerationMin + parameter.AccelerationRange * rdValue) * elapseTime;
}
#endif
#pragma once
#include "PhyEntity.h"

NS_BEGIN

class PhyScene;
class PhyActor;
class TR_CLASS() 
	PhyMaterial : public PhyEntity
{
public:
	ENGINE_RTTI(PhyMaterial);

	physx::PxMaterial* mMaterial;
public:
	PhyMaterial();
	~PhyMaterial();
	virtual void Cleanup() override;
	void BindPhysX();

	float GetDynamicFriction() {
		return mMaterial->getDynamicFriction();
	}
	void SetDynamicFriction(float v) {
		mMaterial->setDynamicFriction(v);
	}
	float GetStaticFriction() {
		return mMaterial->getStaticFriction();
	}
	void SetStaticFriction(float v) {
		mMaterial->setStaticFriction(v);
	}
	float GetRestitution() {
		return mMaterial->getRestitution();
	}
	void SetRestitution(float v) {
		mMaterial->setRestitution(v);
	}
};

NS_END
#include "VKGpuDevice.h"
#include "VKCommandList.h"
#include "VKShader.h"
#include "VKBuffer.h"
#include "VKGpuState.h"
#include "VKEvent.h"
#include "VKInputAssembly.h"
#include "VKFrameBuffers.h"
#include "VKEffect.h"
#include "VKDrawcall.h"
#include "../NxEffect.h"
#include "../../Base/thread/vfxthread.h"

#if defined(HasModule_GpuDump)
#include "../../Bricks/GpuDump/NvAftermath.h"
#endif

#define new VNEW

NS_BEGIN

namespace NxRHI
{
#define ImplVKFunctionPtr(name) PFN_##name VKGpuSystem::name = VK_NULL_HANDLE;
	
	ImplVKFunctionPtr(vkCmdBeginDebugUtilsLabelEXT);
	ImplVKFunctionPtr(vkCmdEndDebugUtilsLabelEXT);
	ImplVKFunctionPtr(vkDebugMarkerSetObjectNameEXT);
	ImplVKFunctionPtr(vkCmdDebugMarkerBeginEXT);
	ImplVKFunctionPtr(vkCmdDebugMarkerEndEXT);
	ImplVKFunctionPtr(vkQueueSubmit2);
	ImplVKFunctionPtr(vkGetSemaphoreCounterValue);
	ImplVKFunctionPtr(vkSignalSemaphore);
	ImplVKFunctionPtr(vkWaitSemaphores);

	void populateDebugMessengerCreateInfo(VkDebugUtilsMessengerCreateInfoEXT& createInfo) 
	{
		createInfo = {};
		createInfo.sType = VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
		createInfo.messageSeverity = VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT |
			VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT |
			VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT |
			VK_DEBUG_UTILS_MESSAGE_SEVERITY_INFO_BIT_EXT;
		createInfo.messageType = VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT;
		createInfo.pfnUserCallback = &VKGpuSystem::debugCallback;
	}
	VkBool32 VKGpuSystem::debugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, const VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
	{
		auto pSys = (VKGpuSystem*)pUserData;
		return pSys->OnVKDebugCallback(messageSeverity, messageType, pCallbackData);
	}
	VKGpuSystem::VKGpuSystem()
	{

	}
	VKGpuSystem::~VKGpuSystem()
	{
		if (mVKInstance != nullptr)
		{
			vkDestroyInstance(mVKInstance, nullptr);
			mVKInstance = nullptr;
		}
	}
	vBOOL VKGpuSystem::OnVKDebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, const VkDebugUtilsMessengerCallbackDataEXT* pCallbackData)
	{
		VFX_LTRACE(ELTT_Graphics, "Vulkan: %s\r\n", pCallbackData->pMessage);
		return FALSE;
	}
	bool VKGpuSystem::InitGpuSystem(ERhiType type, const FGpuSystemDesc* desc)
	{
		VkApplicationInfo appInfo = {};
		appInfo.sType = VK_STRUCTURE_TYPE_APPLICATION_INFO;
		appInfo.pApplicationName = "Titan3D";
		appInfo.applicationVersion = VK_MAKE_VERSION(1, 0, 0);
		appInfo.pEngineName = "Titan3D";
		appInfo.engineVersion = VK_MAKE_VERSION(1, 0, 0);
		appInfo.apiVersion = VK_API_VERSION_1_3;//2080 only support vk 1.2

		uint32_t extensionCount = 0;
		vkEnumerateInstanceExtensionProperties(nullptr, &extensionCount, nullptr);
		mDeviceExtensions.resize(extensionCount);
		vkEnumerateInstanceExtensionProperties(nullptr, &extensionCount, mDeviceExtensions.data());

		UINT NumOfLayer = 0;
		vkEnumerateInstanceLayerProperties(&NumOfLayer, nullptr);
		mLayerProperties.resize(NumOfLayer);
		vkEnumerateInstanceLayerProperties(&NumOfLayer, &mLayerProperties[0]);

		VkInstanceCreateInfo createInfo = {};
		createInfo.sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
		createInfo.pApplicationInfo = &appInfo;

		std::vector<const char*> extensionNames;
		/*for (uint32_t i = 0; i < extensionCount; i++)
		{
			extensionNames.push_back(mDeviceExtensions[i].extensionName);
		}*/
		if (FindExtension(VK_EXT_DEBUG_REPORT_EXTENSION_NAME))
			extensionNames.push_back(VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
		if (FindExtension(VK_KHR_SURFACE_EXTENSION_NAME))
			extensionNames.push_back(VK_KHR_SURFACE_EXTENSION_NAME);
#if defined(PLATFORM_WIN)
		if (FindExtension(VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
			extensionNames.push_back(VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
#elif defined(PLATFORM_DROID)
		if (FindExtension(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
			extensionNames.push_back(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
#endif
		
		createInfo.enabledExtensionCount = static_cast<uint32_t>(extensionNames.size());
		createInfo.ppEnabledExtensionNames = extensionNames.data();

		VkValidationFeaturesEXT validationFeatures{};
		validationFeatures.sType = VK_STRUCTURE_TYPE_VALIDATION_FEATURES_EXT;
		VkValidationFeatureEnableEXT enableFeatures[] = { VK_VALIDATION_FEATURE_ENABLE_GPU_ASSISTED_EXT,
			VK_VALIDATION_FEATURE_ENABLE_GPU_ASSISTED_RESERVE_BINDING_SLOT_EXT ,
			//VK_VALIDATION_FEATURE_ENABLE_BEST_PRACTICES_EXT ,
			VK_VALIDATION_FEATURE_ENABLE_DEBUG_PRINTF_EXT ,
			VK_VALIDATION_FEATURE_ENABLE_SYNCHRONIZATION_VALIDATION_EXT };
		validationFeatures.enabledValidationFeatureCount = sizeof(enableFeatures) / sizeof(VkValidationFeatureEnableEXT);
		validationFeatures.pEnabledValidationFeatures = enableFeatures;

		VkDebugUtilsMessengerCreateInfoEXT debugCreateInfo{};
		std::vector<const char*>	mValidationLayers;
		if (desc->UseRenderDoc)
		{
			auto pLayer = FindLayer("VK_LAYER_RENDERDOC_Capture");
			if (pLayer != nullptr)
			{
				mValidationLayers.push_back("VK_LAYER_RENDERDOC_Capture");
			}
		}
		if (desc->CreateDebugLayer)
		{
			auto pLayer = FindLayer("VK_LAYER_KHRONOS_validation");
			if (pLayer != nullptr)
			{
				mValidationLayers.push_back("VK_LAYER_KHRONOS_validation");
			}

			populateDebugMessengerCreateInfo(debugCreateInfo);
			debugCreateInfo.pUserData = this;
			createInfo.pNext = &debugCreateInfo;
			debugCreateInfo.pNext = &validationFeatures;
		}

		createInfo.enabledLayerCount = static_cast<uint32_t>(mValidationLayers.size());
		if (mValidationLayers.size() > 0)
			createInfo.ppEnabledLayerNames = mValidationLayers.data();

		if (vkCreateInstance(&createInfo, nullptr, &mVKInstance) != VK_SUCCESS) 
		{
			return false;
		}

		UINT						mDeviceNumber = 0;
		vkEnumeratePhysicalDevices(mVKInstance, &mDeviceNumber, nullptr);
		mHwDevices.resize(mDeviceNumber);
		vkEnumeratePhysicalDevices(mVKInstance, &mDeviceNumber, mHwDevices.data());

#define GetVKFunctionPtr(name) name = (PFN_##name)vkGetInstanceProcAddr(mVKInstance, #name);

		//vkCmdBeginDebugUtilsLabelEXT
		GetVKFunctionPtr(vkCmdBeginDebugUtilsLabelEXT);
		GetVKFunctionPtr(vkCmdEndDebugUtilsLabelEXT);
		GetVKFunctionPtr(vkDebugMarkerSetObjectNameEXT);
		GetVKFunctionPtr(vkDebugMarkerSetObjectNameEXT);
		GetVKFunctionPtr(vkCmdDebugMarkerBeginEXT);
		GetVKFunctionPtr(vkCmdDebugMarkerEndEXT);
		GetVKFunctionPtr(vkQueueSubmit2);
		GetVKFunctionPtr(vkGetSemaphoreCounterValue);
		GetVKFunctionPtr(vkSignalSemaphore);
		GetVKFunctionPtr(vkWaitSemaphores);
		
#ifdef PLATFORM_WIN
		{
			VkWin32SurfaceCreateInfoKHR createInfo_surf = {};
			createInfo_surf.sType = VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR;
			createInfo_surf.pNext = nullptr;
			createInfo_surf.hinstance = nullptr;
			createInfo_surf.hwnd = (HWND)desc->WindowHandle; 
			vkCreateWin32SurfaceKHR(mVKInstance, &createInfo_surf, nullptr, &mSurface);
		}
#endif
		return true;
	}
	void VKGpuSystem::GetDeviceDesc(int index, FGpuDeviceDesc* desc) const
	{
		if (index < 0 || index >= (int)mHwDevices.size())
			return;
		VkPhysicalDeviceProperties dxdesc{};
		vkGetPhysicalDeviceProperties(mHwDevices[index], &dxdesc);
		desc->RhiType = ERhiType::RHI_VK;
		desc->VendorId = dxdesc.vendorID;
		desc->AdapterId = index;
		//desc->DedicatedVideoMemory = dxdesc.limits.memor dxdesc.DedicatedVideoMemory;
		strcpy(desc->Name, dxdesc.deviceName);
	}
	IGpuDevice* VKGpuSystem::CreateDevice(const FGpuDeviceDesc* desc)
	{
		auto result = new VKGpuDevice();
		result->InitDevice(this, desc);
		result->Desc.RhiType = ERhiType::RHI_VK;
		return result;
	}
	VKGpuDevice::VKGpuDevice()
	{
		
	}
	static bool GVKGpuDeviceValid = true;
	VKGpuDevice::~VKGpuDevice()
	{
		for (int i = 0; i < 5; i++)
		{
			this->TickPostEvents();
		}

		mNullUBO = nullptr;
		mNullSSBO = nullptr;
		mNullVB = nullptr;
		mNullSampledImage = nullptr;
		mNullSampler = nullptr;
		
		if (mCmdAllocatorManager != nullptr)
		{
			mCmdAllocatorManager->FinalCleanup();
			mCmdAllocatorManager = nullptr;
		}

		mCmdQueue->ClearIdleCmdlists();
		mCmdQueue = nullptr;
		
		if (mSurface != nullptr)
		{
			vkDestroySurfaceKHR(GetVkInstance(), mSurface, this->GetVkAllocCallBacks());
			mSurface = nullptr;
		}
		
		mCBufferAllocator = nullptr;
		mSsboAllocator = nullptr;
		mVbIbAllocator = nullptr;
		mUploadBufferAllocator = nullptr;
		mReadBackAllocator = nullptr;
		mDefaultBufferAllocator = nullptr;
		mPipelineManager = nullptr;
		mFrameFence = nullptr;

		if (mDevice != nullptr)
		{
			GVKGpuDeviceValid = false;
			vkDestroyDevice(mDevice, nullptr);
			mDevice = nullptr;
		}
		if (mDebugReportCallback != nullptr)
		{
			auto fn_vkDestroyDebugReportCallbackEXT = (PFN_vkDestroyDebugReportCallbackEXT)vkGetInstanceProcAddr(GetVkInstance(), "vkDestroyDebugReportCallbackEXT");
			if (fn_vkDestroyDebugReportCallbackEXT != nullptr)
			{
				fn_vkDestroyDebugReportCallbackEXT(GetVkInstance(), mDebugReportCallback, GetVkAllocCallBacks());
			}
			mDebugReportCallback = nullptr;
		}
	}
	ICmdQueue* VKGpuDevice::GetCmdQueue()
	{
		return mCmdQueue;
	}
	VkBool32 VKAPI_PTR OnVK_DebugReportCallbackEXT(
		VkDebugReportFlagsEXT                       flags,
		VkDebugReportObjectTypeEXT                  objectType,
		uint64_t                                    object,
		size_t                                      location,
		int32_t                                     messageCode,
		const char* pLayerPrefix,
		const char* pMessage,
		void* pUserData)
	{
		if (GVKGpuDeviceValid == false)
			return FALSE;
		const char* Mode = "Default";
		//auto device = (VKGpuDevice*)pUserData;
		if (flags & VK_DEBUG_REPORT_WARNING_BIT_EXT)
		{
			Mode = "Warning";
		}
		else if (flags & VK_DEBUG_REPORT_PERFORMANCE_WARNING_BIT_EXT)
		{
			Mode = "PerfWarning";
		}
		else if (flags & VK_DEBUG_REPORT_ERROR_BIT_EXT)
		{
			Mode = "Error";
		}
		else if (flags & VK_DEBUG_REPORT_DEBUG_BIT_EXT)
		{
			Mode = "Debug";
		}
		else if (flags & VK_DEBUG_REPORT_INFORMATION_BIT_EXT)
		{
			Mode = "Info";
		}
		if (strstr(pMessage, "VK_PIPELINE_STAGE_ALL_COMMANDS_BIT") != nullptr)
		{
			return FALSE;
		}
		VFX_LTRACE(ELTT_Graphics, "Vk[%s]: %s\r\n", Mode, pMessage);
		return FALSE;
	}
	struct VkStructureHead
	{
		VkStructureType                     sType;
		const void* pNext;
	};
	void VKGpuDevice::SetBreakOnID(int id, bool open)
	{

	}
	bool VKGpuDevice::InitDevice(IGpuSystem* pGpuSystem, const FGpuDeviceDesc* desc)
	{
		mDeviceThreadId = vfxThread::GetCurrentThreadId();
		auto pVkGpuSys = (VKGpuSystem*)pGpuSystem;

		Desc = *desc;
		mGpuSystem.FromObject(pGpuSystem);

		mCmdQueue = MakeWeakRef(new VKCmdQueue());
		mCmdQueue->mDevice = this;

		mPhysicalDevice = ((VKGpuSystem*)pGpuSystem)->mHwDevices[desc->AdapterId];
		mSurface = ((VKGpuSystem*)pGpuSystem)->mSurface;

		VkSurfaceFormatKHR SwapchainFormats[16];
		vkGetPhysicalDeviceSurfaceFormatsKHR(mPhysicalDevice, mSurface, &mCaps.NumOfSwapchainFormats, nullptr);
		if (mCaps.NumOfSwapchainFormats > 16)
		{
			ASSERT(false);
		}
		vkGetPhysicalDeviceSurfaceFormatsKHR(mPhysicalDevice, mSurface, &mCaps.NumOfSwapchainFormats, SwapchainFormats);
		for (UINT i = 0; i < mCaps.NumOfSwapchainFormats; i++)
		{
			mCaps.SwapchainFormats[i] = VKFormat2Format(SwapchainFormats[i].format);
		}
		//vkGetPhysicalDeviceFormatProperties(mPhysicalDevice, VkFormat::VK_FORMAT_UNDEFINED, VkFormatProperties)
		
		UINT graphicsFamily = -1, presentFamily = -1;

		{
			uint32_t queueFamilyCount = 0;
			vkGetPhysicalDeviceQueueFamilyProperties(mPhysicalDevice, &queueFamilyCount, nullptr);
			std::vector<VkQueueFamilyProperties> queueFamilies(queueFamilyCount);
			vkGetPhysicalDeviceQueueFamilyProperties(mPhysicalDevice, &queueFamilyCount, queueFamilies.data());
			for (UINT i = 0; i < (UINT)queueFamilies.size(); i++)
			{
				if (queueFamilies[i].queueFlags & VK_QUEUE_GRAPHICS_BIT)
				{
					graphicsFamily = i;
				}

				VkBool32 presentSupport = FALSE;
				vkGetPhysicalDeviceSurfaceSupportKHR(mPhysicalDevice, i, mSurface, &presentSupport);
				if (presentSupport)
				{
					presentFamily = i;
				}
				if (presentFamily != -1 && graphicsFamily != -1)
					break;
			}

			ASSERT(presentFamily != -1 && graphicsFamily != -1);
		}
		
		mCmdQueue->mGraphicsQueueIndex = graphicsFamily;
		mCmdQueue->mPresentQueueIndex = presentFamily;
		
		UINT deviceExtCount = 0;
		vkEnumerateDeviceExtensionProperties(mPhysicalDevice, nullptr, &deviceExtCount, nullptr);
		mDeviceExtensions.resize(deviceExtCount);
		vkEnumerateDeviceExtensionProperties(mPhysicalDevice, nullptr, &deviceExtCount, &mDeviceExtensions[0]);
		std::vector<const char*>	extensions;
		{
			/*for (auto& i : mDeviceExtensions)
			{
				extensions.push_back(i.extensionName);
			}*/
			extensions.push_back(VK_KHR_16BIT_STORAGE_EXTENSION_NAME);
			extensions.push_back(VK_KHR_SWAPCHAIN_EXTENSION_NAME);
			extensions.push_back(VK_KHR_16BIT_STORAGE_EXTENSION_NAME);
			extensions.push_back(VK_KHR_SHADER_FLOAT16_INT8_EXTENSION_NAME);
			extensions.push_back(VK_KHR_TIMELINE_SEMAPHORE_EXTENSION_NAME);
			extensions.push_back(VK_EXT_DEBUG_MARKER_EXTENSION_NAME);
			extensions.push_back(VK_EXT_TOOLING_INFO_EXTENSION_NAME);
			if (HasExtension(VK_NV_DEVICE_DIAGNOSTIC_CHECKPOINTS_EXTENSION_NAME))
				extensions.push_back(VK_NV_DEVICE_DIAGNOSTIC_CHECKPOINTS_EXTENSION_NAME);
			if (HasExtension(VK_NV_DEVICE_DIAGNOSTICS_CONFIG_EXTENSION_NAME))
				extensions.push_back(VK_NV_DEVICE_DIAGNOSTICS_CONFIG_EXTENSION_NAME);
			
			//extensions.push_back(VK_GOOGLE_HLSL_FUNCTIONALITY1_EXTENSION_NAME);
			/*extensions.push_back(VK_KHR_SWAPCHAIN_EXTENSION_NAME);
			extensions.push_back(VK_KHR_16BIT_STORAGE_EXTENSION_NAME);
			extensions.push_back(VK_KHR_SHADER_FLOAT16_INT8_EXTENSION_NAME);
			extensions.push_back(VK_KHR_TIMELINE_SEMAPHORE_EXTENSION_NAME);*/
			//mDeviceExtensions.push_back(VK_GOOGLE_HLSL_FUNCTIONALITY1_EXTENSION_NAME);
			//mDeviceExtensions.push_back("SPV_GOOGLE_user_type");
		}

		VkPhysicalDeviceInheritedViewportScissorFeaturesNV dynScissorFeatures{};
		dynScissorFeatures.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_INHERITED_VIEWPORT_SCISSOR_FEATURES_NV;
		dynScissorFeatures.inheritedViewportScissor2D = VK_TRUE;

		/*VkPhysicalDeviceRobustness2FeaturesEXT robustFeatures{};
		robustFeatures.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ROBUSTNESS_2_PROPERTIES_EXT;
		robustFeatures.pNext = &dynScissorFeatures;*/

		/*VkPhysicalDeviceShaderFloat16Int8Features f16i8Features{};
		f16i8Features.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SHADER_FLOAT16_INT8_FEATURES_KHR;
		f16i8Features.pNext = &robustFeatures;*/

		VkPhysicalDeviceFeatures2 features2{};
		features2.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2_KHR;
		features2.pNext = &dynScissorFeatures;// &f16i8Features;
		vkGetPhysicalDeviceFeatures2(mPhysicalDevice, &features2);
		features2.features.robustBufferAccess = VK_TRUE;

		vkGetPhysicalDeviceProperties(mPhysicalDevice, &mDeviceProperties);
		//f16i8Features.shaderFloat16 = VK_TRUE;
		//robustFeatures.nullDescriptor = VK_TRUE;
		//robustFeatures.robustBufferAccess2 = VK_TRUE;
		//robustFeatures.robustImageAccess2 = VK_TRUE;
		//robustFeatures.pNext = &devfeatures11;

		VkPhysicalDeviceVulkan11Features devfeatures11{};
		devfeatures11.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_1_FEATURES;
		devfeatures11.pNext = nullptr;//&dynScissorFeatures;
		devfeatures11.multiview = VK_TRUE;
		//devfeatures11.storageInputOutput16 = VK_TRUE;
		devfeatures11.uniformAndStorageBuffer16BitAccess = VK_TRUE;
		//devfeatures11.storagePushConstant16 = VK_TRUE;	

		VkPhysicalDeviceVulkan12Features devfeatures12{};
		devfeatures12.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES;
		devfeatures12.timelineSemaphore = VK_TRUE;
		devfeatures12.shaderFloat16 = VK_TRUE;
		devfeatures12.shaderInt8 = VK_TRUE;
		//devfeatures12.pNext = &devfeatures11;
		devfeatures12.pNext = &features2;

		VkDeviceCreateInfo createInfo = {};
		createInfo.sType = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
		createInfo.pNext = &devfeatures12;

		[[maybe_unused]]VkStructureHead* curDeviceCreateChainTail = nullptr;
		curDeviceCreateChainTail = (VkStructureHead*)&devfeatures12;
		float queuePriority = 1.0f;
		VkDeviceQueueCreateInfo queueCreateInfos[2]{};
		{
			if (graphicsFamily == presentFamily)
			{
				queueCreateInfos[0].sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
				queueCreateInfos[0].queueFamilyIndex = graphicsFamily;
				queueCreateInfos[0].queueCount = 1;
				queueCreateInfos[0].pQueuePriorities = &queuePriority;

				createInfo.queueCreateInfoCount = 1;
				createInfo.pQueueCreateInfos = queueCreateInfos;
			}
			else
			{
				queueCreateInfos[0].sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
				queueCreateInfos[0].queueFamilyIndex = graphicsFamily;
				queueCreateInfos[0].queueCount = 1;
				queueCreateInfos[0].pQueuePriorities = &queuePriority;

				queueCreateInfos[1].sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
				queueCreateInfos[1].queueFamilyIndex = presentFamily;
				queueCreateInfos[1].queueCount = 1;
				queueCreateInfos[1].pQueuePriorities = &queuePriority;

				createInfo.queueCreateInfoCount = 2;
				createInfo.pQueueCreateInfos = queueCreateInfos;
			}
		}

		{
			mDeviceFeatures.shaderInt16 = VK_TRUE;
			mDeviceFeatures.samplerAnisotropy = VK_TRUE;
			mDeviceFeatures.depthClamp = VK_TRUE;
			mDeviceFeatures.fillModeNonSolid = VK_TRUE;
			mDeviceFeatures.shaderUniformBufferArrayDynamicIndexing = VK_TRUE;
			mDeviceFeatures.shaderSampledImageArrayDynamicIndexing = VK_TRUE;
			mDeviceFeatures.shaderStorageBufferArrayDynamicIndexing = VK_TRUE;
			mDeviceFeatures.shaderStorageImageArrayDynamicIndexing = VK_TRUE;
			mDeviceFeatures.robustBufferAccess = VK_TRUE;
			createInfo.pEnabledFeatures = &mDeviceFeatures;
		}

		createInfo.enabledExtensionCount = static_cast<uint32_t>(extensions.size());
		createInfo.ppEnabledExtensionNames = extensions.data();

		std::vector<const char*>	mValidationLayers;
		if (desc->CreateDebugLayer)
		{
			auto pLayer = pVkGpuSys->FindLayer("VK_LAYER_KHRONOS_validation");
			if (pLayer != nullptr)
			{
				mValidationLayers.push_back("VK_LAYER_KHRONOS_validation");
			}
			createInfo.enabledLayerCount = static_cast<uint32_t>(mValidationLayers.size());
			createInfo.ppEnabledLayerNames = mValidationLayers.data();
		}
		else
		{
			createInfo.enabledLayerCount = 0;
		}

#if defined(HasModule_GpuDump)
		if (desc->GpuDump && desc->IsNVIDIA())
		{
			GpuDump::NvAftermath::InitDump(NxRHI::RHI_VK);
			VkDeviceDiagnosticsConfigCreateInfoNV nvDiagnosticsInfo{};
			curDeviceCreateChainTail->pNext = &nvDiagnosticsInfo;
			nvDiagnosticsInfo.sType = VK_STRUCTURE_TYPE_DEVICE_DIAGNOSTICS_CONFIG_CREATE_INFO_NV;
			nvDiagnosticsInfo.flags = VkDeviceDiagnosticsConfigFlagBitsNV::VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_RESOURCE_TRACKING_BIT_NV |
				VkDeviceDiagnosticsConfigFlagBitsNV::VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_AUTOMATIC_CHECKPOINTS_BIT_NV |
				VkDeviceDiagnosticsConfigFlagBitsNV::VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_SHADER_DEBUG_INFO_BIT_NV;
		}
#endif
		if (vkCreateDevice(mPhysicalDevice, &createInfo, GetVkAllocCallBacks(), &mDevice) != VK_SUCCESS)
		{
			ASSERT(false);
			return false;
		}
		QueryDevice();
		
		auto fn_vkCreateDebugReportCallbackEXT = (PFN_vkCreateDebugReportCallbackEXT)vkGetInstanceProcAddr(GetVkInstance(), "vkCreateDebugReportCallbackEXT");
		if (fn_vkCreateDebugReportCallbackEXT != nullptr)
		{
			VkDebugReportCallbackCreateInfoEXT cbInfo{};
			cbInfo.sType = VK_STRUCTURE_TYPE_DEBUG_REPORT_CALLBACK_CREATE_INFO_EXT;
			cbInfo.flags = VkDebugReportFlagBitsEXT::VK_DEBUG_REPORT_ERROR_BIT_EXT | VkDebugReportFlagBitsEXT::VK_DEBUG_REPORT_WARNING_BIT_EXT;
			cbInfo.pfnCallback = OnVK_DebugReportCallbackEXT;
			cbInfo.pUserData = this;
			fn_vkCreateDebugReportCallbackEXT(this->GetVkInstance(), &cbInfo, this->GetVkAllocCallBacks(), &mDebugReportCallback);
		}
		
		vkGetDeviceQueue(mDevice, graphicsFamily, 0, &mCmdQueue->mGraphicsQueue);
		vkGetDeviceQueue(mDevice, presentFamily, 0, &mCmdQueue->mPresentQueue);
		
		FFenceDesc fcDesc{};
		mFrameFence = MakeWeakRef(this->CreateFence(&fcDesc, "Vulkan Frame Fence"));
		
		{
			auto cmdAllocator = new VKCmdBufferManager();
			cmdAllocator->Initialize(this);
			mCmdAllocatorManager = MakeWeakRef(cmdAllocator);

			/*auto manager = mCmdAllocatorManager;
			FContextTickableManager::GetInstance()->PushTickable([manager]()->bool
				{
					auto context = manager->GetThreadContext();
					if (context == nullptr)
						return true;
					context->TickForRecycle(manager->mDevice);
					return false;
				});*/
		}

		mCmdQueue->Init(this);
		
		mGpuResourceAlignment.TexturePitchAlignment = (UINT)mDeviceProperties.limits.minTexelBufferOffsetAlignment;
		mGpuResourceAlignment.TextureAlignment = (UINT)mGpuResourceAlignment.TexturePitchAlignment;
		mGpuResourceAlignment.MsaaAlignment = (UINT)mDeviceProperties.limits.minTexelBufferOffsetAlignment;
		mGpuResourceAlignment.RawSrvUavAlignment = (UINT)mDeviceProperties.limits.minStorageBufferOffsetAlignment;
		mGpuResourceAlignment.UavCounterAlignment = (UINT)mDeviceProperties.limits.minStorageBufferOffsetAlignment;
		
		mCaps.IsSupoortBufferToTexture = true;
		mCaps.IsSupportSSBO_VS = true;

		vkGetPhysicalDeviceMemoryProperties(mPhysicalDevice, &mMemProperties);

		UINT memTypeIndex, memAlignment;
		if (GetAllocatorInfo(VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
			VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
			memTypeIndex, memAlignment))
		{
			mCBufferAllocator = MakeWeakRef(new VKGpuPooledMemAllocator());
			mCBufferAllocator->mBatchPoolSize = 128 * 1024 * 1024;//128k
			mCBufferAllocator->mMemTypeIndex = memTypeIndex;
			mGpuResourceAlignment.CBufferAlignment = memAlignment;
		}
		if (GetAllocatorInfo(VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT | VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VK_BUFFER_USAGE_TRANSFER_SRC_BIT | VK_BUFFER_USAGE_TRANSFER_DST_BIT,
			VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
			memTypeIndex, memAlignment))
		{
			mSsboAllocator = MakeWeakRef(new VKGpuLinearMemAllocator());
			mSsboAllocator->PoolSize = 1024 * 1204 * 8;//8 mbytes per block;
			mSsboAllocator->mAlignment = memAlignment;
			mSsboAllocator->mMemTypeIndex = memTypeIndex;
			mGpuResourceAlignment.SsbAlignment = memAlignment;
		}
		if (GetAllocatorInfo(VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VK_BUFFER_USAGE_INDEX_BUFFER_BIT,
			VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
			memTypeIndex, memAlignment))
		{
			mVbIbAllocator = MakeWeakRef(new VKGpuLinearMemAllocator());
			mVbIbAllocator->PoolSize = 1024 * 1204 * 8;//8 mbytes per block;
			mVbIbAllocator->mAlignment = memAlignment;
			mVbIbAllocator->mMemTypeIndex = memTypeIndex;
			mGpuResourceAlignment.VbIbAlignment = memAlignment;
		}
		if (GetAllocatorInfo(VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VK_BUFFER_USAGE_INDEX_BUFFER_BIT | VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT | VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
			VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
			memTypeIndex, memAlignment))
		{
			mUploadBufferAllocator = MakeWeakRef(new VKGpuLinearMemAllocator());
			mUploadBufferAllocator->PoolSize = 1024 * 1204 * 8;//8 mbytes per block;
			mUploadBufferAllocator->mAlignment = memAlignment;
			mUploadBufferAllocator->mMemTypeIndex = memTypeIndex;
		}
		if (GetAllocatorInfo(VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VK_BUFFER_USAGE_INDEX_BUFFER_BIT | VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT | VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VK_BUFFER_USAGE_TRANSFER_DST_BIT,
			VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT,
			memTypeIndex, memAlignment))
		{
			mReadBackAllocator = MakeWeakRef(new VKGpuLinearMemAllocator());
			mReadBackAllocator->PoolSize = 1024 * 1204 * 8;//8 mbytes per block;
			mReadBackAllocator->mAlignment = memAlignment;
			mReadBackAllocator->mMemTypeIndex = memTypeIndex;
		}

		mDefaultBufferAllocator = MakeWeakRef(new VKGpuDefaultMemAllocator());

		CreateNullObjects();
		return true;
	}
	void VKGpuDevice::CreateNullObjects()
	{
		{
			FBufferDesc vbDesc{};
			vbDesc.Type = EBufferType::BFT_UAV;
			vbDesc.Size = 1;
			vbDesc.RowPitch = 1;
			vbDesc.DepthPitch = 1;
			vbDesc.StructureStride = 1;
			mNullSSBO = MakeWeakRef((VKBuffer*)this->CreateBuffer(&vbDesc));
			mNullSSBO->SetDebugName("NullSSBO");
		}
		{
			FBufferDesc vbDesc{};
			vbDesc.Type = EBufferType::BFT_CBuffer;
			vbDesc.Size = 1;
			vbDesc.RowPitch = 1;
			vbDesc.DepthPitch = 1;
			vbDesc.StructureStride = 1;
			mNullUBO = MakeWeakRef((VKBuffer*)this->CreateBuffer(&vbDesc));
			mNullUBO->SetDebugName("NullUBO");
		}
		{
			FBufferDesc vbDesc{};
			vbDesc.Type = EBufferType::BFT_Vertex;
			vbDesc.Size = 1;
			vbDesc.RowPitch = 1;
			vbDesc.DepthPitch = 1;
			vbDesc.StructureStride = 1;
			mNullVB = MakeWeakRef((VKBuffer*)this->CreateBuffer(&vbDesc));
			mNullVB->SetDebugName("NullVB");
		}
		{
			FTextureDesc texDesc{};
			texDesc.SetDefault();
			texDesc.BindFlags = (EBufferType)(EBufferType::BFT_SRV);
			auto pTex = MakeWeakRef((VKTexture*)this->CreateTexture(&texDesc));
			FSrvDesc srvDesc{};
			srvDesc.SetTexture2D();
			srvDesc.Format = texDesc.Format;
			mNullSampledImage = MakeWeakRef((VKSrView*)this->CreateSRV(pTex, &srvDesc));
			mNullSampledImage->SetDebugName("NullSampledImage");
		}
		{
			FSamplerDesc samplerDesc{};
			samplerDesc.SetDefault();
			mNullSampler = MakeWeakRef((VKSampler*)this->CreateSampler(&samplerDesc));
			mNullSampler->SetDebugName("NullSampler");
		}
	}
	bool VKGpuDevice::GetAllocatorInfo(VkBufferUsageFlags flags, VkMemoryPropertyFlags prop, UINT& typeIndex, UINT& alignment)
	{
		VkBufferCreateInfo bufferInfo = {};
		bufferInfo.sType = VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO;
		bufferInfo.size = flags;
		bufferInfo.flags = 0;
		bufferInfo.usage |= VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT;
		bufferInfo.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
		bufferInfo.queueFamilyIndexCount = 0;
		VkBuffer tmp = nullptr;
		if (vkCreateBuffer(mDevice, &bufferInfo, GetVkAllocCallBacks(), &tmp) != VK_SUCCESS)
		{
			ASSERT(false);
			return false;
		}
		VkMemoryRequirements memRequirements;
		vkGetBufferMemoryRequirements(mDevice, tmp, &memRequirements);
		vkDestroyBuffer(mDevice, tmp, GetVkAllocCallBacks());
		tmp = nullptr;

		alignment = (UINT)memRequirements.alignment;
		typeIndex = this->FindMemoryType(memRequirements.memoryTypeBits, prop);
		return true;
	}
	void VKGpuDevice::QueryDevice()
	{
		
	}
	IBuffer* VKGpuDevice::CreateBuffer(const FBufferDesc* desc)
	{
		auto result = new VKBuffer();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ITexture* VKGpuDevice::CreateTexture(const FTextureDesc* desc)
	{
		auto result = new VKTexture();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ICbView* VKGpuDevice::CreateCBV(IBuffer* pBuffer, const FCbvDesc* desc)
	{
		auto result = new VKCbView();
		if (result->Init(this, pBuffer, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IVbView* VKGpuDevice::CreateVBV(IBuffer* pBuffer, const FVbvDesc* desc)
	{
		auto result = new VKVbView();
		if (result->Init(this, pBuffer, desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IIbView* VKGpuDevice::CreateIBV(IBuffer* pBuffer, const FIbvDesc* desc)
	{
		auto result = new VKIbView();
		if (result->Init(this, pBuffer, desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ISrView* VKGpuDevice::CreateSRV(IGpuBufferData* pBuffer, const FSrvDesc* desc)
	{
		auto result = new VKSrView();
		if (result->Init(this, pBuffer, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IUaView* VKGpuDevice::CreateUAV(IGpuBufferData* pBuffer, const FUavDesc* desc)
	{
		auto result = new VKUaView();
		if (result->Init(this, pBuffer, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IRenderTargetView* VKGpuDevice::CreateRTV(ITexture* pBuffer, const FRtvDesc* desc)
	{
		auto result = new VKRenderTargetView();
		if (result->Init(this, pBuffer, desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IDepthStencilView* VKGpuDevice::CreateDSV(ITexture* pBuffer, const FDsvDesc* desc)
	{
		auto result = new VKDepthStencilView();
		if (result->Init(this, pBuffer, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ISampler* VKGpuDevice::CreateSampler(const FSamplerDesc* desc)
	{
		auto result = new VKSampler();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ISwapChain* VKGpuDevice::CreateSwapChain(const FSwapChainDesc* desc)
	{
		auto result = new VKSwapChain();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IRenderPass* VKGpuDevice::CreateRenderPass(const FRenderPassDesc* desc)
	{
		auto result = new VKRenderPass();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IFrameBuffers* VKGpuDevice::CreateFrameBuffers(IRenderPass* rpass)
	{
		auto result = new VKFrameBuffers();
		result->mRenderPass = rpass;
		result->mDeviceRef.FromObject(this);
		return result;
	}
	IGpuPipeline* VKGpuDevice::CreatePipeline(const FGpuPipelineDesc* desc)
	{
		auto result = new VKGpuPipeline();
		if (result->Init(this, *desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IGpuDrawState* VKGpuDevice::CreateGpuDrawState()
	{
		return new VKGpuDrawState();
	}
	IInputLayout* VKGpuDevice::CreateInputLayout(FInputLayoutDesc* desc)
	{
		auto result = new VKInputLayout();
		if (result->Init(this, desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	ICommandList* VKGpuDevice::CreateCommandList()
	{
		auto result = new VKCommandList();
		if (result->Init(this) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IShader* VKGpuDevice::CreateShader(FShaderDesc* desc)
	{
		auto result = new VKShader();
		if (result->Init(this, desc) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IGraphicsEffect* VKGpuDevice::CreateShaderEffect()
	{
		return new VKGraphicsEffect();
	}
	IComputeEffect* VKGpuDevice::CreateComputeEffect()
	{
		return new VKComputeEffect();
	}
	IFence* VKGpuDevice::CreateFence(const FFenceDesc* desc, const char* name)
	{
		auto result = new VKFence();
		if (result->Init(this, *desc, name) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IEvent* VKGpuDevice::CreateGpuEvent(const FEventDesc* desc, const char* name)
	{
		auto result = new VKEvent(name);
		if (result->Init(this, *desc, name) == false)
		{
			result->Release();
			return nullptr;
		}
		return result;
	}
	IGraphicDraw* VKGpuDevice::CreateGraphicDraw()
	{
		auto result = new VKGraphicDraw();
		result->mDeviceRef.FromObject(this);
		return result;
	}
	IComputeDraw* VKGpuDevice::CreateComputeDraw()
	{
		auto result = new VKComputeDraw();
		result->mDeviceRef.FromObject(this);
		return result;
	}
	IGpuScope* VKGpuDevice::CreateGpuScope()
	{
		return nullptr;
	}
	void VKGpuDevice::TickPostEvents()
	{
		IsSyncStage = true;
		IGpuDevice::TickPostEvents();
		/*auto delta = (UINT)(aspect - completed);
		if (delta > 1)
		{
			VFX_LTRACE(ELTT_Graphics, "Aspect - Completed = %d\r\n", delta);
		}*/
		IsSyncStage = false;
	}

	VKCmdQueue::VKCmdQueue()
	{

	}
	VKCmdQueue::~VKCmdQueue()
	{
		if (mIdleCmdlist.empty() == false)
		{
			mIdleCmdlist.pop();
		}
	}
	void VKCmdQueue::ClearIdleCmdlists()
	{
		if (mIdleCmdlist.empty() == false)
		{
			mIdleCmdlist.pop();
		}
	}
	void VKCmdQueue::Init(VKGpuDevice* device)
	{
		FFenceDesc fcDesc{};
		mFlushFence = MakeWeakRef(device->CreateFence(&fcDesc, "CmdQueue Fence"));
		
		mDummyCmdList = MakeWeakRef((VKCommandList*)device->CreateCommandList());
		mDummyCmdList->BeginCommand(VK_COMMAND_BUFFER_USAGE_SIMULTANEOUS_USE_BIT);
		/*mDummyCmdList->BeginEvent("dummy");
		mDummyCmdList->EndEvent();*/
		mDummyCmdList->EndCommand(false);
	}
	void VKCmdQueue::QueueExecuteCommandList(ICommandList* Cmdlist, EQueueType type)
	{
		auto dx11Cmd = (VKCommandList*)Cmdlist;
		auto cmdFence = dx11Cmd->mCommitFence.UnsafeConvertTo<VKFence>();
		if (dx11Cmd->mCommandBuffer == nullptr)
			return;

		{
			VAutoVSLLock lk(mGraphicsQueueLocker);
			VkSubmitInfo submitInfo{};
			submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
			submitInfo.commandBufferCount = 1;
			submitInfo.pCommandBuffers = &dx11Cmd->mCommandBuffer->RealObject;
			submitInfo.waitSemaphoreCount = 0;
			submitInfo.pWaitSemaphores = nullptr;
			VkPipelineStageFlags waitStage = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;/*VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT |
				VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT |
				VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;*/
			submitInfo.pWaitDstStageMask = &waitStage;
			submitInfo.signalSemaphoreCount = 1;
			VkSemaphore signalSemas[1]{};
			signalSemas[0] = cmdFence->mSemaphore;
			submitInfo.pSignalSemaphores = signalSemas;

			VkTimelineSemaphoreSubmitInfo timelineInfo{};
			submitInfo.pNext = &timelineInfo;
			timelineInfo.sType = VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO;

			timelineInfo.waitSemaphoreValueCount = 0;
			timelineInfo.pWaitSemaphoreValues = nullptr;

			UINT64 signalValues[1]{};
			signalValues[0] = ++cmdFence->ExpectValue;
			timelineInfo.signalSemaphoreValueCount = 2;
			timelineInfo.pSignalSemaphoreValues = signalValues;

			auto hr = vkQueueSubmit(mGraphicsQueue, 1, &submitInfo, nullptr);
			ASSERT(hr == VK_SUCCESS);
			//dx11Cmd->mCommandBuffer = nullptr;
			dx11Cmd->Commit(this, type);
		}
	}
	void VKCmdQueue::ExecuteCommandList(UINT NumOfExe, ICommandList** Cmdlist, UINT NumOfWait, ICommandList** ppWaitCmdlists, EQueueType type)
	{
		for (UINT i = 0; i < NumOfWait; i++)
		{
			ppWaitCmdlists[i]->mCommitFence->WaitToExpect();
		}

		for (UINT i = 0; i < NumOfExe; i++)
		{
			auto vkCmd = (VKCommandList*)Cmdlist[i];
			//vkCmd->Commit(this);
			QueueExecuteCommandList(vkCmd, type);
			this->IncreaseSignal(vkCmd->mCommitFence, type);
			//vkCmd->ResetGpuDraws();
		}
	}
	void VKCmdQueue::WaitFence(IFence* fence, UINT64 value)
	{
		auto waitFence = (VKFence*)fence;

		VAutoVSLLock lk(mGraphicsQueueLocker);

		VkSubmitInfo submitInfo{};
		submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
		submitInfo.commandBufferCount = 1;
		submitInfo.pCommandBuffers = &mDummyCmdList->mCommandBuffer->RealObject;
		
		VkPipelineStageFlags waitStage = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;/*VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT |
			VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT |
			VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;*/
		VkSemaphore waitSmp[1]{};
		waitSmp[0] = waitFence->mSemaphore;
		submitInfo.waitSemaphoreCount = 1;
		submitInfo.pWaitSemaphores = waitSmp;
		VkPipelineStageFlags waitStages[1];
		waitStages[0] = waitStage;
		submitInfo.pWaitDstStageMask = waitStages;

		submitInfo.signalSemaphoreCount = 0;
		submitInfo.pSignalSemaphores = nullptr;

		VkTimelineSemaphoreSubmitInfo timelineInfo{};
		submitInfo.pNext = &timelineInfo;
		timelineInfo.sType = VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO;

		UINT64 waitValue[1]{};
		waitValue[0] = value;
		timelineInfo.waitSemaphoreValueCount = 2;
		timelineInfo.pWaitSemaphoreValues = waitValue;

		timelineInfo.signalSemaphoreValueCount = 0;
		timelineInfo.pSignalSemaphoreValues = nullptr;

		auto hr = vkQueueSubmit(mGraphicsQueue, 1, &submitInfo, nullptr);
		ASSERT(hr == VK_SUCCESS);
	}
	UINT64 VKCmdQueue::QueueSignal(IFence* fence, UINT64 value, VkFence g2hFence, EQueueType type)
	{
		ASSERT(value != UINT64_MAX);
		auto signalFence = (VKFence*)fence;

		VAutoVSLLock lk(mGraphicsQueueLocker);

		if (signalFence->IsBinary() == false)
		{
			if (signalFence->ExpectValue >= value)
			{
				ASSERT(false);
				return signalFence->GetCompletedValue();
			}
			signalFence->ExpectValue = value;
		}

		VkSubmitInfo submitInfo{};
		submitInfo.sType = VK_STRUCTURE_TYPE_SUBMIT_INFO;
		submitInfo.commandBufferCount = 1;
		submitInfo.pCommandBuffers = &mDummyCmdList->mCommandBuffer->RealObject;

		submitInfo.waitSemaphoreCount = 0;
		submitInfo.pWaitSemaphores = nullptr;
		VkPipelineStageFlags waitStage = VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT |
			VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT |
			VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
		submitInfo.pWaitDstStageMask = &waitStage;
		submitInfo.signalSemaphoreCount = 1;
		VkSemaphore signalSmp[1]{};
		signalSmp[0] = signalFence->mSemaphore;
		submitInfo.pSignalSemaphores = signalSmp;

		VkTimelineSemaphoreSubmitInfo timelineInfo{};
		submitInfo.pNext = &timelineInfo;
		timelineInfo.sType = VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO;

		timelineInfo.waitSemaphoreValueCount = 0;
		timelineInfo.pWaitSemaphoreValues = nullptr;

		UINT64 signalValue[1]{};
		signalValue[0] = value;
		timelineInfo.signalSemaphoreValueCount = 1;
		timelineInfo.pSignalSemaphoreValues = signalValue;

		auto hr = vkQueueSubmit(mGraphicsQueue, 1, &submitInfo, g2hFence);
		ASSERT(hr == VK_SUCCESS);

		return 0;
	}
	ICommandList* VKCmdQueue::GetIdleCmdlist()
	{
		VAutoVSLLock locker(mQueueLocker);
		if (mIdleCmdlist.empty())
		{
			mIdleCmdlist.push(MakeWeakRef(mDevice->CreateCommandList()));
		}
		auto result = mIdleCmdlist.front();
		result->AddRef();
		mIdleCmdlist.pop();
		return result;
	}
	void VKCmdQueue::ReleaseIdleCmdlist(ICommandList* cmd)
	{
		VAutoVSLLock locker(mQueueLocker);
		mIdleCmdlist.push(cmd);
		cmd->Release();
		return;
	}
	UINT64 VKCmdQueue::Flush(EQueueType type)
	{
		return mFlushFence->WaitToExpect();
	}
}

NS_END
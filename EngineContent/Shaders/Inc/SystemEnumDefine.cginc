#ifndef	 _SYSTEM_ENUM_DEFINE_H_
#define _SYSTEM_ENUM_DEFINE_H_

/*
enum ERenderFlags
{
    None = 0,
    DisableEnvColor = 1,
}
*/
#define ERenderFlags_None 0
#define ERenderFlags_DisableEnvColor 1

#define ObjectFlags_2Bit_Shadow 1
#define ObjectFlags_2Bit_Lighting (1 << 1)

#endif 
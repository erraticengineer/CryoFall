﻿#define MASK_LAYER_TWO
#define MASK_LAYER_THREE

#include "GroundTileBlendBase.fx"

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
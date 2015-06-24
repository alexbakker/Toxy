#include <stdint.h>

/*
This code was taked from uTox, https://github.com/notsecure/uTox
It's licensed under GPLv3
*/

static uint8_t rgb_to_y(int r, int g, int b)
{
	int y = ((9798 * r + 19235 * g + 3736 * b) >> 15);
	return y>255 ? 255 : y<0 ? 0 : y;
}

static uint8_t rgb_to_u(int r, int g, int b)
{
	int u = ((-5538 * r + -10846 * g + 16351 * b) >> 15) + 128;
	return u>255 ? 255 : u<0 ? 0 : u;
}

static uint8_t rgb_to_v(int r, int g, int b)
{
	int v = ((16351 * r + -13697 * g + -2664 * b) >> 15) + 128;
	return v>255 ? 255 : v<0 ? 0 : v;
}

__declspec(dllexport) void yuv420tobgr(uint16_t width, uint16_t height, const uint8_t *y, const uint8_t *u, const uint8_t *v, unsigned int ystride, unsigned int ustride, unsigned int vstride, uint8_t *out);
__declspec(dllexport) void bgrtoyuv420(uint8_t *plane_y, uint8_t *plane_u, uint8_t *plane_v, uint8_t *rgb, uint16_t width, uint16_t height);
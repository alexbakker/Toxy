#include "util.h"

/*
This code was taked from uTox, https://github.com/notsecure/uTox
It's licensed under GPLv3
*/

void yuv420tobgr(uint16_t width, uint16_t height, const uint8_t *y, const uint8_t *u, const uint8_t *v, unsigned int ystride, unsigned int ustride, unsigned int vstride, uint8_t *out)
{
	unsigned long int i, j;
	for (i = 0; i < height; ++i) {
		for (j = 0; j < width; ++j) {
			uint8_t *point = out + 4 * ((i * width) + j);
			int t_y = y[((i * ystride) + j)];
			int t_u = u[(((i / 2) * ustride) + (j / 2))];
			int t_v = v[(((i / 2) * vstride) + (j / 2))];
			t_y = t_y < 16 ? 16 : t_y;

			int r = (298 * (t_y - 16) + 409 * (t_v - 128) + 128) >> 8;
			int g = (298 * (t_y - 16) - 100 * (t_u - 128) - 208 * (t_v - 128) + 128) >> 8;
			int b = (298 * (t_y - 16) + 516 * (t_u - 128) + 128) >> 8;

			point[2] = r>255 ? 255 : r<0 ? 0 : r;
			point[1] = g>255 ? 255 : g<0 ? 0 : g;
			point[0] = b>255 ? 255 : b<0 ? 0 : b;
			point[3] = ~0;
		}
	}
}

void bgrtoyuv420(uint8_t *plane_y, uint8_t *plane_u, uint8_t *plane_v, uint8_t *rgb, uint16_t width, uint16_t height)
{
	uint16_t x, y;
	uint8_t *p;
	uint8_t r, g, b;

	for (y = 0; y != height; y += 2) {
		p = rgb;
		for (x = 0; x != width; x++) {
			b = *rgb++;
			g = *rgb++;
			r = *rgb++;
			*plane_y++ = rgb_to_y(r, g, b);
		}

		for (x = 0; x != width / 2; x++) {
			b = *rgb++;
			g = *rgb++;
			r = *rgb++;
			*plane_y++ = rgb_to_y(r, g, b);

			b = *rgb++;
			g = *rgb++;
			r = *rgb++;
			*plane_y++ = rgb_to_y(r, g, b);

			b = ((int)b + (int)*(rgb - 6) + (int)*p + (int)*(p + 3) + 2) / 4; p++;
			g = ((int)g + (int)*(rgb - 5) + (int)*p + (int)*(p + 3) + 2) / 4; p++;
			r = ((int)r + (int)*(rgb - 4) + (int)*p + (int)*(p + 3) + 2) / 4; p++;

			*plane_u++ = rgb_to_u(r, g, b);
			*plane_v++ = rgb_to_v(r, g, b);

			p += 3;
		}
	}
}
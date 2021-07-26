using System.Linq;
using UnityEngine;

namespace MBaske.Sensors.Audio.Util
{
    /// <summary>
    /// Utility class for encoding audio samples as textures.
    /// </summary>
    public static class TextureUtil
    {
        /// <summary>
        /// Creates a new texture whose size matches the observation shape.
        /// </summary>
        /// <param name="shape">Observation shape of the <see cref="AudioSensor"/>.</param>
        public static Texture2D CreateTexture(SensorObservationShape shape)
        {
            var texture = new Texture2D(shape.Width, shape.Height, TextureFormat.RGB24, false);
            texture.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), shape.Width * shape.Height).ToArray());
            return texture;
        }

        /// <summary>
        /// Encodes <see cref="AudioBuffer"/> contents to texture pixels for "visual" observations.
        /// </summary>
        /// <param name="sensor">The <see cref="AudioSensor"/>.</param>
        /// <param name="texture">The texture to update.</param>
        /// <param name="textureIndex">Index of the texture.</param>
        /// <param name="channelsPerTexture">Number of channels (colors) per texture.</param>
        public static void UpdateTexture(AudioSensor sensor,
            Texture2D texture, int textureIndex, int channelsPerTexture = 3)
        {
            var buffer = sensor.Buffer;
            var shape = sensor.Shape;
            int w = shape.Width;
            int h = shape.Height;

            // TODO https://github.com/Unity-Technologies/ml-agents/issues/5445
            var colors = texture.GetPixels32();

            for (int col = 0; col < channelsPerTexture; col++)
            {
                int channel = textureIndex * channelsPerTexture + col;
                if (channel < shape.Channels)
                {
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            // Bottom to top, left to right (Color32 array is vertically flipped).
                            colors[(h - y - 1) * w + x][col] = (byte)(buffer.GetSample(channel, w * y + x) * 255);
                        }
                    }
                }
                else
                {
                    for (int i = 0, n = w * h; i < n; i++)
                    {
                        colors[i][col] = 0;
                    }
                }
            }

            texture.SetPixels32(colors);
        }
    }
}
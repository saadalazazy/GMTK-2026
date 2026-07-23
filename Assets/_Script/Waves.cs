using UnityEngine;

namespace Ditzelgames
{
    [System.Serializable]
    public struct Wave
    {
        public Vector2 Direction;    // XZ travel direction (normalized automatically)
        [Range(0f, 1f)]
        public float Steepness;      // 0 = gentle sine, closer to 1 = sharper Gerstner peak
        public float WaveLength;     // distance between crests, in meters
    }

    // Single source of truth for wave shape. Computes height/position on the CPU
    // (for WaterFloat) AND pushes the same parameters to the water material so the
    // Shader Graph vertex displacement matches exactly.
    public class Waves : MonoBehaviour
    {
        [Tooltip("Renderer using the water Shader Graph material. Required to keep GPU waves in sync with CPU waves.")]
        public Renderer WaterRenderer;

        public Wave[] waves = new Wave[]
        {
            new Wave { Direction = new Vector2(1f, 0f),     Steepness = 0.4f,  WaveLength = 12f },
            new Wave { Direction = new Vector2(1f, 0.6f),   Steepness = 0.3f,  WaveLength = 7f  },
            new Wave { Direction = new Vector2(-0.6f, 1f),  Steepness = 0.25f, WaveLength = 4f  },
            new Wave { Direction = new Vector2(0.3f, -1f),  Steepness = 0.2f,  WaveLength = 2.5f },
        };

        const float Gravity = 9.81f;

        static readonly int[] WaveIDs =
        {
            Shader.PropertyToID("_Wave0"),
            Shader.PropertyToID("_Wave1"),
            Shader.PropertyToID("_Wave2"),
            Shader.PropertyToID("_Wave3"),
        };

        MaterialPropertyBlock mpb;

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            PushToShader();
        }

        void OnValidate()
        {
            if (Application.isPlaying && mpb != null)
                PushToShader();
        }

        // Sends wave parameters to the water material as (dirX, dirZ, steepness, wavelength).
        // Property names MUST match the Vector4 properties exposed on the Shader Graph blackboard.
        void PushToShader()
        {
            if (WaterRenderer == null)
                return;

            WaterRenderer.GetPropertyBlock(mpb);
            for (int i = 0; i < WaveIDs.Length; i++)
            {
                if (i < waves.Length && waves[i].WaveLength > 0f)
                {
                    var dir = waves[i].Direction.normalized;
                    mpb.SetVector(WaveIDs[i], new Vector4(dir.x, dir.y, waves[i].Steepness, waves[i].WaveLength));
                }
                else
                {
                    mpb.SetVector(WaveIDs[i], Vector4.zero); // disabled slot
                }
            }
            WaterRenderer.SetPropertyBlock(mpb);
        }

        // World-space water surface height at a given XZ position, at the current time.
        public float GetHeight(Vector3 position)
        {
            return GetPosition(position).y;
        }

        // Full displaced surface position (horizontal Gerstner displacement included).
        // Keep this math identical to GerstnerWave_float in GerstnerWaves.hlsl.
        public Vector3 GetPosition(Vector3 position)
        {
            float t = Time.time;
            float x = 0f, y = 0f, z = 0f;

            for (int i = 0; i < waves.Length; i++)
            {
                var wave = waves[i];
                if (wave.WaveLength <= 0f)
                    continue;

                Vector2 dir = wave.Direction.normalized;
                float k = 2f * Mathf.PI / wave.WaveLength;
                float c = Mathf.Sqrt(Gravity / k);
                float a = wave.Steepness / k;
                float f = k * (dir.x * position.x + dir.y * position.z) - c * t;

                x += dir.x * a * Mathf.Cos(f);
                z += dir.y * a * Mathf.Cos(f);
                y += a * Mathf.Sin(f);
            }

            return new Vector3(position.x + x, y, position.z + z);
        }
    }
}

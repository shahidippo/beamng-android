using System;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Original four-cylinder synthesis for development. Firing frequency follows RPM;
    /// this is deliberately not presented as a sampled production audio asset.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralEngineAudio : MonoBehaviour
    {
        public VehicleController Vehicle;
        public float Volume = .45f;
        private volatile float frequency = 30f;
        private volatile float amplitude;
        private double phase;
        private double smoothedFrequency = 30;
        private double smoothedAmplitude;
        private int sampleRate;
        private AudioClip carrier;
        private AudioSource source;

        public static ProceduralEngineAudio Create(VehicleController vehicle)
        {
            var audioObject = new GameObject("Original engine synthesizer");
            audioObject.transform.SetParent(vehicle.transform, false);
            var engine = audioObject.AddComponent<ProceduralEngineAudio>();
            engine.Vehicle = vehicle;
            return engine;
        }

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) sampleRate = 48000;
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = .2f;
            source.minDistance = 2f;
            source.maxDistance = 60f;
            carrier = AudioClip.Create("ImpactDrive original synthesis carrier", sampleRate, 1, sampleRate, false);
            source.clip = carrier;
            source.Play();
        }

        private void Update()
        {
            if (!Vehicle) { amplitude = 0f; return; }
            frequency = Mathf.Max(1f, Vehicle.EngineRpm / 60f * 2f);
            amplitude = Vehicle.EngineRunning && Time.timeScale > 0f
                ? Mathf.Clamp01(Volume) * (.1f + .2f * Vehicle.Input.Throttle) : 0f;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            // Only copied scalars are touched on Unity's audio thread.
            double targetFrequency = frequency;
            double targetAmplitude = amplitude;
            double step = 2.0 * Math.PI / Math.Max(8000, sampleRate);
            for (int sample = 0; sample < data.Length; sample += channels)
            {
                smoothedFrequency += (targetFrequency - smoothedFrequency) * .002;
                smoothedAmplitude += (targetAmplitude - smoothedAmplitude) * .001;
                phase += smoothedFrequency * step;
                if (phase >= 2.0 * Math.PI) phase -= 2.0 * Math.PI;
                float value = (float)(smoothedAmplitude * (Math.Sin(phase) * .60
                    + Math.Sin(phase * 2.0 + .35) * .24 + Math.Sin(phase * 3.0) * .11
                    + Math.Sin(phase * 4.0 + .2) * .05));
                for (int channel = 0; channel < channels; channel++) data[sample + channel] = value;
            }
        }

        private void OnDestroy()
        {
            if (source) source.Stop();
            if (carrier) Destroy(carrier);
        }
    }
}

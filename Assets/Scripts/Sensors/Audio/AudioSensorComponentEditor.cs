#if (UNITY_EDITOR)
using UnityEditor;
using UnityEngine;
using MBaske.Sensors.Util;
using MBaske.Sensors.Audio.Util;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// Custom editor for the <see cref="AudioSensorComponent"/>.
    /// </summary>
    [CustomEditor(typeof(AudioSensorComponent))]
    [CanEditMultipleObjects]
    public class AudioSensorComponentEditor : Editor
    {
        private const string c_PlayMsg = "Enter play mode to fine-tune settings using game audio.";
        private const string c_SaveMsg = @"Copy your updated settings with 'Copy Component' while still in play mode. Back in editor mode, save them with 'Paste Component Values'.

Please disable calibration during training for better performance.";
        private const string c_NoSensorMsg = "Sensor wasn't initialized. Is the component attached to an agent?";

        private Color m_ColMargin;
        private Color m_ColClip;
        private Color m_ColBarAmp;
        private Color m_ColBarScaled;
        private Color m_ColBarNormalized;
        private Color m_ColLineScaled;
        private Color m_ColLineNormalized;
        private Color m_ColButton;

        private float[] m_FFTSmoothed;
        private float m_AmpSmoothed;
        private const float c_Response = 30; // interpolation

        private Texture2D m_Texture;
        private AudioSensorComponent m_Component;

        private int FFTBitWidth
        {
            get { return serializedObject.FindProperty("m_FFTBitWidth").intValue; }
            set { serializedObject.FindProperty("m_FFTBitWidth").intValue = value; }
        }

        private float FFTMinFreqLog
        {
            get { return serializedObject.FindProperty("m_FFTMinFreqLog").floatValue; }
            set { serializedObject.FindProperty("m_FFTMinFreqLog").floatValue = value; }
        }

        private float FFTMaxFreqLog
        {
            get { return serializedObject.FindProperty("m_FFTMaxFreqLog").floatValue; }
            set { serializedObject.FindProperty("m_FFTMaxFreqLog").floatValue = value; }
        }

        private float FFTFloorLog
        {
            get { return serializedObject.FindProperty("m_FFTFloorLog").floatValue; }
            set { serializedObject.FindProperty("m_FFTFloorLog").floatValue = value; }
        }

        private float AmpFloorLog
        {
            get { return serializedObject.FindProperty("m_AmpFloorLog").floatValue; }
            set { serializedObject.FindProperty("m_AmpFloorLog").floatValue = value; }
        }

        private bool CalibrationButtonActive
        {
            get { return serializedObject.FindProperty("m_CalibrationButtonActive").boolValue; }
            set { serializedObject.FindProperty("m_CalibrationButtonActive").boolValue = value; }
        }


        private void OnEnable()
        {
            m_Component = target as AudioSensorComponent;

            m_ColBarAmp = HexColor("553F33");
            m_ColBarScaled = HexColor("044166");
            m_ColBarNormalized = HexColor("2D5D46");
            m_ColLineScaled = HexColor("49B5F7");
            m_ColLineNormalized = HexColor("89C6A9");
            m_ColMargin = HexColor("111111");
            m_ColClip = HexColor("DA2C3800");
            m_ColButton = HexColor("FF9944");
        }

        public override bool RequiresConstantRepaint()
        {
            return m_Component.IsCalibrating;
        }

        public override void OnInspectorGUI()
        {
            EditorStyles.helpBox.padding = new RectOffset(12, 12, 8, 12);
            EditorStyles.helpBox.fontSize = 12;

            serializedObject.Update();
            DrawGeneralSettings();
            DrawCalibrationSettings();
            DrawGraphAndSensorInfo();
            DrawCalibrationButtonAndMessage();
        }

        private void DrawGeneralSettings()
        {
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SensorName"));

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BufferLength"),
                            GUILayout.Width(EditorGUIUtility.labelWidth + 32));

                        float secs = Time.fixedDeltaTime * m_Component.BufferLength;
                        EditorGUILayout.LabelField($"{string.Format("{0:0.00}", secs)} seconds");
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CompressionType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SignalType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SampleType"));
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateTexture();
            }
        }

        private void DrawCalibrationSettings()
        {
            EditorGUI.BeginDisabledGroup(!CalibrationButtonActive);
            {
                float minFreq = FFTMinFreqLog;
                float maxFreq = FFTMaxFreqLog;
                var labelWidth = GUILayout.Width(EditorGUIUtility.labelWidth);

                EditorGUI.BeginChangeCheck();
                {
                    if (m_Component.SampleType == SampleType.Spectrum)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FFTWindow"));

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"FFT Resolution: {m_Component.FFTResolution}", labelWidth);
                            FFTBitWidth = (int)GUILayout.HorizontalSlider(FFTBitWidth, 6, 13); // -> 64 to 8192
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"Band: {FormatFrequency(m_Component.FFTMinFrequency)} - {FormatFrequency(m_Component.FFTMaxFrequency)}", labelWidth);
                            EditorGUILayout.MinMaxSlider(ref minFreq, ref maxFreq, Frequeny.Log20Hz, Frequeny.Log20kHz);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        if (m_Component.SampleType == SampleType.Spectrum)
                        {
                            EditorGUILayout.LabelField($"Floor: {string.Format("{0:0}", m_Component.FFTFloor)}dB", labelWidth);
                            FFTFloorLog = GUILayout.HorizontalSlider(FFTFloorLog, -6, -2); // -> -192dB to -12dB
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Floor: {string.Format("{0:0}", m_Component.AmpFloor)}dB", labelWidth);
                            AmpFloorLog = GUILayout.HorizontalSlider(AmpFloorLog, -6, -2); // -> -192dB to -12dB
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Normalize"), GUILayout.Width(EditorGUIUtility.labelWidth + 20));
                        if (m_Component.Normalize && GUILayout.Button("Reset"))
                        {
                            m_Component.ResetNormalization();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    FFTMinFreqLog = minFreq;
                    FFTMaxFreqLog = maxFreq;

                    serializedObject.ApplyModifiedProperties();
                    UpdateTexture();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGraphAndSensorInfo()
        {
            if (m_Component.Sensor != null && m_Component.IsCalibrating)
            {
                EditorGUILayout.Space(4);
                DrawGLGraph();

                EditorGUILayout.BeginHorizontal();
                {
                    var info = $"{m_Component.Shape} ({m_Component.SamplesPerChannel} Samples per channel)";
                    GUILayout.Label(info);
                    GUILayout.FlexibleSpace();

                    UpdateTexture(true);
                    TextureUtil.UpdateTexture(m_Component.Sensor, m_Texture, 0, m_Component.Shape.SignalChannels);
                    m_Texture.Apply();
                    GUILayout.Box(m_Texture);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCalibrationButtonAndMessage()
        {
            bool playMode = Application.isPlaying;
            bool hasSensor = m_Component.Sensor != null;

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            {
                var color = GUI.backgroundColor;
                GUI.backgroundColor = CalibrationButtonActive ? m_ColButton : color;
                if (GUILayout.Button(hasSensor && m_Component.IsCalibrating ? "Listening..." : "Calibrate", GUILayout.Height(32)))
                {
                    CalibrationButtonActive = !CalibrationButtonActive;
                }
                GUI.backgroundColor = color;
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (CalibrationButtonActive)
            {
                if (playMode)
                {
                    if (hasSensor)
                    {
                        EditorGUILayout.HelpBox(c_SaveMsg, MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(c_NoSensorMsg, MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(c_PlayMsg, MessageType.None);
                }
            }
        }

        private void DrawGLGraph()
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1000, 200, 200);

            if (Event.current.type == EventType.Repaint)
            {
                GUI.BeginClip(rect);
                GL.PushMatrix();

                GL.Clear(true, false, Color.black);
                EditorUtil.GLMaterial.SetPass(0);

                GL.Begin(GL.QUADS);
                GL.Color(Color.black);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(rect.width, 0, 0);
                GL.Vertex3(rect.width, rect.height, 0);
                GL.Vertex3(0, rect.height, 0);
                GL.End();

                switch (m_Component.SampleType)
                {
                    case SampleType.Spectrum:
                        DrawSpectrum(rect);
                        break;

                    case SampleType.Amplitude:
                        DrawAmplitude(rect);
                        break;
                }

                GL.PopMatrix();
                GUI.EndClip();
            }
        }

        private void DrawSpectrum(Rect rect)
        {
            float w = rect.width;
            float h = rect.height;
            float x, y;

            float response = Time.deltaTime * c_Response;
            float floor = m_Component.FFTFloor;
            bool normalize = m_Component.Normalize;

            float xMin = AudioUtil.NormalizeFrequency(m_Component.FFTMinFrequency) * w;
            float xMax = AudioUtil.NormalizeFrequency(m_Component.FFTMaxFrequency) * w;

            int bandWidth = Mathf.RoundToInt(xMax - xMin + 1);
            if (m_FFTSmoothed == null || m_FFTSmoothed.Length != bandWidth)
            {
                m_FFTSmoothed = new float[bandWidth];
            }

            // Frequency band margins.
            GL.Begin(GL.QUADS);
            GL.Color(m_ColMargin);
            GL.Vertex3(0, h, 0);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(xMin, 0, 0);
            GL.Vertex3(xMin, h, 0);
            GL.Vertex3(w, h, 0);
            GL.Vertex3(w, 0, 0);
            GL.Vertex3(xMax, 0, 0);
            GL.Vertex3(xMax, h, 0);
            GL.End();

            // FFT bands.
            GL.Begin(GL.LINES);
            for (x = 0; x <= w; x++)
            {
                int i = AudioUtil.FrequencyToSpectrumIndex(AudioUtil.NormalizedToFrequency(x / w));
                float mean = m_Component.GetLRMeanSampleValue(i);

                if (x >= xMin && x <= xMax)
                {
                    float scaled = AudioUtil.RescaleSampleValue(mean, floor);
                    float tmp = scaled;

                    if (normalize)
                    {
                        // Normalized values.
                        GL.Color(m_ColBarNormalized);
                        float norm = scaled * m_Component.GetFFTBandExpansion(i);
                        norm = norm >= 1 ? 0.99f : norm; // cut clipping
                        tmp = norm;
                        y = h - norm * h;
                        GL.Vertex3(x, h, 0);
                        GL.Vertex3(x, y, 0);
                    }

                    // Scaled values.
                    GL.Color(m_ColBarScaled);
                    y = h - scaled * h;
                    GL.Vertex3(x, h, 0);
                    GL.Vertex3(x, y, 0);

                    i = Mathf.RoundToInt(x - xMin);
                    m_FFTSmoothed[i] = Mathf.Max(tmp, Mathf.Lerp(m_FFTSmoothed[i], tmp, response));
                }

                // Raw FFT values.
                GL.Color(m_ColBarAmp);
                y = h - mean * h;
                GL.Vertex3(x, h, 0);
                GL.Vertex3(x, y, 0);
            }
            GL.End();

            // Smoothed line.
            GL.Begin(GL.LINE_STRIP);
            GL.Color(normalize ? m_ColLineNormalized : m_ColLineScaled);
            for (int i = 1; i < bandWidth - 1; i++)
            {
                x = i + xMin;
                y = h - m_FFTSmoothed[i] * h;
                GL.Vertex3(x, y, 0);
            }
            GL.End();

            // Clip bar.
            m_ColClip.a = m_Component.IsClipping ? 1 : Mathf.Lerp(m_ColClip.a, 0, response);
            GL.Begin(GL.QUADS);
            GL.Color(m_ColClip);
            GL.Vertex3(xMin, 0, 0);
            GL.Vertex3(xMin, 5, 0);
            GL.Vertex3(xMax, 5, 0);
            GL.Vertex3(xMax, 0, 0);
            GL.End();
        }

        private void DrawAmplitude(Rect rect)
        {
            float response = Time.deltaTime * c_Response;
            float floor = m_Component.AmpFloor;
            bool normalize = m_Component.Normalize;

            float[] rms = m_Component.GetRMSLevel();
            float scaledL = AudioUtil.RescaleSampleValue(rms[0], floor);
            float scaledR = AudioUtil.RescaleSampleValue(rms[1], floor);
            float tmp = (scaledL + scaledR) * 0.5f;

            if (normalize)
            {
                float expand = m_Component.GetAmplitudeExpansion();
                float normL = scaledL * expand;
                float normR = scaledR * expand;
                normL = normL >= 1 ? 0.99f : normL; // cut clipping
                normR = normR >= 1 ? 0.99f : normR;
                // Normalized values.
                DrawLRBars(normL, normR, rect, m_ColBarNormalized);
                tmp = (normL + normR) * 0.5f;
            }

            // Scaled values.
            DrawLRBars(scaledL, scaledR, rect, m_ColBarScaled);
            // RMS values.
            DrawLRBars(rms[0], rms[1], rect, m_ColBarAmp);

            // Smoothed line.
            m_AmpSmoothed = Mathf.Max(tmp, Mathf.Lerp(m_AmpSmoothed, tmp, response));
            float y = rect.height - m_AmpSmoothed * rect.height;
            GL.Begin(GL.LINES);
            GL.Color(normalize ? m_ColLineNormalized : m_ColLineScaled);
            GL.Vertex3(0, y, 0);
            GL.Vertex3(rect.width, y, 0);
            GL.End();

            // Clip bar.
            m_ColClip.a = m_Component.IsClipping ? 1 : Mathf.Lerp(m_ColClip.a, 0, response);
            GL.Begin(GL.QUADS);
            GL.Color(m_ColClip);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 5, 0);
            GL.Vertex3(rect.width, 5, 0);
            GL.Vertex3(rect.width, 0, 0);
            GL.End();
        }

        private void DrawLRBars(float ampL, float ampR, Rect rect, Color color)
        {
            float w = rect.width;
            float h = rect.height;
            float x = w * 0.5f, y;

            GL.Begin(GL.QUADS);
            GL.Color(color);
            y = h - ampL * h;
            GL.Vertex3(0, h, 0);
            GL.Vertex3(0, y, 0);
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x, h, 0);
            y = h - ampR * h;
            GL.Vertex3(x + 1, h, 0);
            GL.Vertex3(x + 1, y, 0);
            GL.Vertex3(w, y, 0);
            GL.Vertex3(w, h, 0);
            GL.End();
        }

        private void UpdateTexture(bool keepExisting = false)
        {
            m_Texture = keepExisting
                ? (m_Texture != null ? m_Texture : TextureUtil.CreateTexture(m_Component.Shape))
                : (m_Component.IsCalibrating ? TextureUtil.CreateTexture(m_Component.Shape) : null);
        }

        private static string FormatFrequency(float freq)
        {
            return freq < 1000
                ? string.Format("{0:0}", freq) + "Hz"
                : string.Format("{0:0.0}", freq / 1000f) + "kHz";
        }

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.white;
        }
    }
}
#endif
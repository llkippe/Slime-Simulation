#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

[CustomEditor(typeof(Simulation))]
public class SimulationRecorderEditor : Editor
{
	private RecorderController recorderController;
	private RecorderControllerSettings controllerSettings;
	private MovieRecorderSettings movieRecorderSettings;
	private RenderTextureInputSettings renderTextureInputSettings;

	private Simulation sim => (Simulation)target;

	private void OnEnable()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private void OnDisable()
	{
		EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		StopRecording();
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Recorder Control", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("When Record Simulation is enabled, this editor tool will capture the runtime displayTexture using Unity Recorder.", MessageType.Info);

		if (!sim.recordSimulation)
		{
			EditorGUILayout.HelpBox("Enable Record Simulation on the Simulation component to use the Unity Recorder integration.", MessageType.None);
			return;
		}

		if (Application.isPlaying)
		{
			if (recorderController != null && recorderController.IsRecording())
			{
				if (GUILayout.Button("Stop Recorder"))
				{
					StopRecording();
				}
			}
			else
			{
				if (GUILayout.Button("Start Recorder"))
				{
					StartRecording();
				}
			}
		}
		else
		{
			if (GUILayout.Button("Prepare Recorder for Play"))
			{
				PrepareRecorder();
			}
		}
	}

	private void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.EnteredPlayMode)
		{
			if (sim.recordSimulation && sim.autoStartRecorder)
			{
				EditorApplication.delayCall += StartRecording;
			}
		}

		if (state == PlayModeStateChange.ExitingPlayMode)
		{
			EditorApplication.delayCall += StopRecording;
		}
	}

	private void PrepareRecorder()
	{
		if (controllerSettings != null)
		{
			return;
		}

		if (!Directory.Exists(sim.recorderFolder))
		{
			Directory.CreateDirectory(sim.recorderFolder);
		}

		controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
		controllerSettings.SetRecordModeToManual();
		controllerSettings.FrameRate = sim.recorderFrameRate;
		controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
		controllerSettings.CapFrameRate = true;
		controllerSettings.ExitPlayMode = false;

		movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
		movieRecorderSettings.Enabled = true;
		if (Application.platform == RuntimePlatform.LinuxEditor)
		{
			movieRecorderSettings.EncoderSettings = new CoreEncoderSettings
			{
				Codec = CoreEncoderSettings.OutputCodec.WEBM,
				EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium,
			};
			movieRecorderSettings.OutputFile = Path.Combine(sim.recorderFolder, sim.recordingFileName + ".webm");
		}
		else
		{
			movieRecorderSettings.EncoderSettings = new CoreEncoderSettings
			{
				Codec = CoreEncoderSettings.OutputCodec.MP4,
				EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium,
			};
			movieRecorderSettings.OutputFile = Path.Combine(sim.recorderFolder, sim.recordingFileName + ".mp4");
		}
		movieRecorderSettings.ImageInputSettings = new RenderTextureInputSettings();

		renderTextureInputSettings = movieRecorderSettings.ImageInputSettings as RenderTextureInputSettings;

		controllerSettings.AddRecorderSettings(movieRecorderSettings);
		recorderController = new RecorderController(controllerSettings);
		recorderController.PrepareRecording();
	}

	private void StartRecording()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("Unity Recorder can only start while the editor is in Play mode.");
			return;
		}

		if (sim.displayTexture == null)
		{
			Debug.LogWarning("displayTexture is not available yet. Wait until the Simulation component has initialized in Play mode.");
			return;
		}

		if (controllerSettings == null || recorderController == null)
		{
			PrepareRecorder();
		}

		if (renderTextureInputSettings == null)
		{
			renderTextureInputSettings = movieRecorderSettings.ImageInputSettings as RenderTextureInputSettings;
		}

		renderTextureInputSettings.RenderTexture = sim.displayTexture;
		// Width and height are taken from the RenderTexture itself.
		// Setting them here on a runtime-created texture is not supported.

		if (!recorderController.IsRecording())
		{
			recorderController.StartRecording();
			Debug.Log($"Unity Recorder started: {Path.Combine(sim.recorderFolder, sim.recordingFileName)}");
		}
	}

	private void StopRecording()
	{
		if (recorderController != null && recorderController.IsRecording())
		{
			recorderController.StopRecording();
			Debug.Log("Unity Recorder stopped.");
		}

		if (controllerSettings != null)
		{
			ScriptableObject.DestroyImmediate(controllerSettings);
			controllerSettings = null;
		}

		movieRecorderSettings = null;
		recorderController = null;
		renderTextureInputSettings = null;
	}
}
#endif
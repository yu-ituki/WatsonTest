using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 実験：SpeechToText.
/// </summary>
public class Test_SpeechToText : MonoBehaviour {

	const string API_USER_NAME = "「サービス資格情報」に書いてあるusername";
	const string API_PASS = "「サービス資格情報」に書いてあるpassword";


	[SerializeField] Button m_Button_Record;
	[SerializeField] Button m_Button_SelectFile;
	[SerializeField] Button m_Button_PlayFileNow;
	[SerializeField] Button m_Button_Run;

	[SerializeField] Text m_Text_Record;
	[SerializeField] Text m_Text_Play;
	[SerializeField] Text m_Text_LastSelectFilePath;
	[SerializeField] Text m_Text_Result;

	[SerializeField] AudioSource m_AudioSource;

	bool m_IsRecordingNow;
	string m_LastRecordingDeviceName;
	AudioClip m_LastSelectClip;
	IEnumerator m_LastAPICoroutine;

	[System.Serializable]
	class SpeechToTextResult {
		public Alternative[] alternatives = null;
	}

	[System.Serializable]
	class Alternative {
		public string transcript = null;
	}

	[System.Serializable]
	class ResponseWrapper<T> {
		public T[] results = null;
	}


	private void Awake() {
		m_Button_Record.onClick.AddListener( OnClickRecord );
		m_Button_SelectFile.onClick.AddListener( OnClickSelectFile );
		m_Button_PlayFileNow.onClick.AddListener( OnClickPlayFile );
		m_Button_Run.onClick.AddListener( OnClickRun );
		UpdateRecordStatus();
	}

	private void Update() {
		UpdateRecording();
		UpdateSelectFile();
		UpdatePlayFile();
		UpdateRun();
	}

	#region 録音系.
	void UpdateRecording() {
		// 録音関係.
		bool isEnableNow = true;
		isEnableNow &= ( Microphone.devices.Length > 0 );
		isEnableNow &= !m_AudioSource.isPlaying;
		isEnableNow &= ( m_LastAPICoroutine == null );

		if ( m_Button_Record.interactable != isEnableNow ) {
			m_Button_Record.interactable = isEnableNow;
		}
		if ( m_IsRecordingNow ) {
			if ( !Microphone.IsRecording( m_LastRecordingDeviceName ) ) {
				m_IsRecordingNow = false;
				UpdateRecordStatus();
			}
		}
	}

	void OnClickRecord() {
		m_IsRecordingNow = !m_IsRecordingNow;

		if ( m_IsRecordingNow ) {
			m_Text_LastSelectFilePath.text = "録音データ";
			m_LastRecordingDeviceName = Microphone.devices[ 0 ];
			m_LastSelectClip = Microphone.Start( m_LastRecordingDeviceName, false, 30, 44100 );
		} else {
			Microphone.End( m_LastRecordingDeviceName );
		}
		UpdateRecordStatus();
	}


	void UpdateRecordStatus() {
		if ( m_IsRecordingNow ) m_Text_Record.text = "録音中...";
		else					m_Text_Record.text = "録音開始";
	}
	#endregion


	#region ファイル選択系.
	void UpdateSelectFile() {
		bool isEnableNow = true;
		isEnableNow &= !m_IsRecordingNow;
		isEnableNow &= !m_AudioSource.isPlaying;
		isEnableNow &= ( m_LastAPICoroutine == null );

		if ( isEnableNow != m_Button_SelectFile.interactable ) {
			m_Button_SelectFile.interactable = isEnableNow;
		}
	}

	void OnClickSelectFile() {
		try {
			var dialog = new System.Windows.Forms.OpenFileDialog();
			if ( System.IO.File.Exists( m_Text_LastSelectFilePath.text ) ) {
				dialog.FileName = m_Text_LastSelectFilePath.text;
			} else {
				dialog.FileName = Application.dataPath;
			}
			dialog.Filter = "wave file(*.wav)|*.wav";
			dialog.CheckFileExists = true;
			var result = dialog.ShowDialog();
			if ( result == System.Windows.Forms.DialogResult.OK ) {
				m_Text_LastSelectFilePath.text = dialog.FileName;
				var bytes = System.IO.File.ReadAllBytes( m_Text_LastSelectFilePath.text );
				m_LastSelectClip = WAVUtil.ToAudioClip( "Now", bytes );

			}
		} catch ( System.Exception e ) {
			Debug.Log( e );
		}
	}


	#endregion


	#region 再生系.
	void UpdatePlayFile() {
		bool isEnableNow = true;
		isEnableNow &= ( m_LastSelectClip != null );
		isEnableNow &= !m_IsRecordingNow;
		isEnableNow &= ( m_LastAPICoroutine == null );
		if ( isEnableNow != m_Button_PlayFileNow.interactable ) {
			m_Button_PlayFileNow.interactable = isEnableNow;
		}
		UpdatePlayStatus();
	}

	void OnClickPlayFile() {
		if ( m_AudioSource.isPlaying ) {
			m_AudioSource.Stop();
		} else {
			m_AudioSource.clip = m_LastSelectClip;
			m_AudioSource.loop = false;
			m_AudioSource.Play();
		}
		UpdatePlayStatus();
	}

	void UpdatePlayStatus() {
		if ( m_AudioSource.isPlaying ) m_Text_Play.text = "再生中...";
		else m_Text_Play.text = "再生する";
	}
	#endregion


	#region Watson系.
	void UpdateRun() {
		bool isEnableNow = true;
		isEnableNow &= ( m_LastSelectClip != null );
		isEnableNow &= !m_IsRecordingNow;
		isEnableNow &= !m_AudioSource.isPlaying;
		isEnableNow &= ( m_LastAPICoroutine == null );
		if ( m_Button_Run.interactable != isEnableNow ) {
			m_Button_Run.interactable = isEnableNow;
		}
		if ( m_LastAPICoroutine != null && !m_LastAPICoroutine.MoveNext() ) {
			m_LastAPICoroutine = null;
		}
	}

	void OnClickRun() {
		if ( m_LastSelectClip == null ) return;
		string tmpPathFlac = Application.dataPath + "/tmp.flac";
		string tmpPathWav = Application.dataPath + "/tmp.wav";

		try {
			var bytes = WAVUtil.ToByte( m_LastSelectClip );
			System.IO.File.WriteAllBytes( tmpPathWav, bytes );
			FFMpeg.ToFlac( tmpPathWav, tmpPathFlac );
			var bytesFlac = System.IO.File.ReadAllBytes( tmpPathFlac );

			Dictionary<string,string> headers = new Dictionary<string, string>( 64 );
			headers.Add( "Content-Type", "audio/flac" );
			Dictionary<string,object> fields = new Dictionary<string, object>( 64 );
			fields.Add( "model", "ja-JP_BroadbandModel" );
			fields.Add( "max_alternatives", "3" );
			fields.Add( "smart_formatting", "false" );
			fields.Add( "timestamps", "true" );
			fields.Add( "word_confidence", "false" );
			m_LastAPICoroutine = WatsonUtil.Send(
				"https://stream.watsonplatform.net/speech-to-text/api/v1/recognize",
				API_USER_NAME,
				API_PASS,
				OnEndedWatsonAPI,
				headers,
				fields,
				bytesFlac
			);

		} catch ( System.Exception e ) {
			Debug.Log( e );
		}
		if ( System.IO.File.Exists( tmpPathWav ) ) System.IO.File.Delete( tmpPathWav );
		if ( System.IO.File.Exists( tmpPathFlac ) ) System.IO.File.Delete( tmpPathFlac );
	}

	void OnEndedWatsonAPI( WWW www ) {
		var resp = www.text;
		var result = JsonUtility.FromJson<ResponseWrapper<SpeechToTextResult>>( resp );
		System.Text.StringBuilder sb = new System.Text.StringBuilder( 1000 );
		sb.AppendLine( "[Watsonの出力データ]" );
		for ( int i = 0; i < result.results.Length; ++i ) {
			var alts = result.results[ i ].alternatives;
			for ( int j = 0; j < alts.Length; ++j ) {
				sb.Append( "◆" );
				sb.AppendLine( alts[ j ].transcript );
			}
		}
		m_Text_Result.text = sb.ToString();
	}
#endregion
}
